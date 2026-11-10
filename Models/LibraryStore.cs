using System.IO.Compression;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
namespace VrcShelf.Desktop;
public sealed class LibraryStore
{
    public string Root { get; }
    readonly string stateFile;
    readonly SemaphoreSlim writes = new(1,1);
    public LibraryStore(string root, string state) { Root=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar); stateFile=state; }
    public string SafePath(string relative, bool mustExist=true)
    {
        if(Path.IsPathRooted(relative)) throw new InvalidOperationException("Ruta absoluta no permitida");
        var full=Path.GetFullPath(Path.Combine(Root,relative));
        if(!full.Equals(Root,StringComparison.OrdinalIgnoreCase)&&!full.StartsWith(Root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Ruta fuera de biblioteca");
        var cursor=Root;
        if(Directory.Exists(cursor)&&(File.GetAttributes(cursor)&FileAttributes.ReparsePoint)!=0) throw new InvalidOperationException("Enlaces no permitidos");
        foreach(var part in Path.GetRelativePath(Root,full).Split(Path.DirectorySeparatorChar))
        {
            if(part==".")continue;cursor=Path.Combine(cursor,part);
            if(File.Exists(cursor)||Directory.Exists(cursor)) { if((File.GetAttributes(cursor)&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Enlaces no permitidos"); }
            else if(mustExist)throw new FileNotFoundException("No se encuentra el archivo o carpeta",cursor);
        }
        return full;
    }
    public static JsonNode ReadJson(string file,JsonNode fallback) => File.Exists(file)?JsonNode.Parse(File.ReadAllText(file).TrimStart('\uFEFF'))??fallback:fallback;
    public static async Task AtomicWrite(string file, JsonNode data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
        try { await File.WriteAllTextAsync(temp,data.ToJsonString()); File.Move(temp,file,true); }
        finally { if(File.Exists(temp))File.Delete(temp); }
    }
    public JsonNode Personal()=>ReadJson(stateFile,new JsonObject());
    public async Task SavePersonal(JsonNode data)
    {
        if(data is not JsonObject)throw new InvalidOperationException("Favoritos inválidos");
        foreach(var p in (JsonObject)data)if(p.Value is not JsonObject)throw new InvalidOperationException("Favoritos inválidos");
        await writes.WaitAsync();try{await AtomicWrite(stateFile,data);}finally{writes.Release();}
    }
    public JsonObject Catalog()
    {
        SafePath("");
        var meta=Path.Combine(Root,"99_Catalogo");
        var old=(ReadJson(Path.Combine(meta,"catalogo.json"),new JsonArray()) as JsonArray??new()).OfType<JsonObject>()
          .Where(f=>f["relativa"]!=null).GroupBy(f=>f["relativa"]!.ToString(),StringComparer.OrdinalIgnoreCase).ToDictionary(g=>g.Key,g=>g.First(),StringComparer.OrdinalIgnoreCase);
        var overrides=(ReadJson(Path.Combine(meta,"compatibilidades-confirmadas.json"),new JsonArray()) as JsonArray??new()).OfType<JsonObject>().ToArray();
        var result=new JsonArray();
        void Walk(string dir)
        {
            foreach(var entry in Directory.EnumerateFileSystemEntries(dir))
            {
                var attr=File.GetAttributes(entry);if((attr&FileAttributes.ReparsePoint)!=0)continue;
                if((attr&FileAttributes.Directory)!=0){if(!entry.Equals(meta,StringComparison.OrdinalIgnoreCase))Walk(entry);continue;}
                var f=new FileInfo(entry);var rel=Path.GetRelativePath(Root,entry);old.TryGetValue(rel,out var previous);
                var record=previous?.DeepClone().AsObject()??new JsonObject();
                var tags=(previous?["etiquetas"] as JsonArray)?.Select(t=>t!.ToString()).Where(t=>!t.StartsWith("estado:duplicado")).ToList()??new(){"tipo:"+rel.Split('\\')[0],"formato:"+f.Extension.TrimStart('.')};
                if(previous?["bytes"]?.GetValue<long>()==f.Length&&(previous?["etiquetas"] as JsonArray)?.Any(t=>t?.ToString()=="estado:duplicado_exacto")==true)tags.Add("estado:duplicado_exacto");
                var match=overrides.FirstOrDefault(o=>!string.IsNullOrEmpty(o["producto"]?.ToString())&&rel.Contains(o["producto"]!.ToString(),StringComparison.OrdinalIgnoreCase));
                if(match!=null)
                {
                    tags.RemoveAll(t=>t.StartsWith("avatar:")||t.StartsWith("compatibilidad:")||t=="clasificacion:generico");
                    tags.AddRange((match["avatares"] as JsonArray??new()).Select(a=>"avatar:"+a));tags.Add("compatibilidad:confirmada_por_usuario");
                    tags.AddRange((match["etiquetas"] as JsonArray??new()).Select(t=>t!.ToString()));
                    if(match["generico"]?.GetValue<bool>()==true)tags.AddRange(new[]{"compatibilidad:generico","clasificacion:generico"});
                }
                else if(rel.StartsWith("02_Ropa\\",StringComparison.OrdinalIgnoreCase))
                {var avatar=rel.Split('\\')[1];if(!new[]{"Mix","Multiavatar","Compatibilidad_Pendiente","Generales","!_FullAsset"}.Contains(avatar))tags.Add("avatar:"+avatar);}
                if(rel.StartsWith("09_R18\\")||rel.Contains("【まよ Mayo】《R-18》ボディテクスチャ BodyTexture")||rel.Contains("【りる RIRU用】(R18) Nipple＆Body texture"))tags.Add("contenido:r18");
                record["nombre"]=f.Name;record["ruta"]=entry;record["relativa"]=rel;record["bytes"]=f.Length;record["modificadoUtc"]=f.LastWriteTimeUtc.ToString("o");
                record["version"]=previous?["version"]?.ToString()??Regex.Match(f.Name,@"\d+\.\d+(?:\.\d+)*").Value;
                record["etiquetas"]=new JsonArray(tags.Distinct().Select(t=>JsonValue.Create(t)).ToArray());result.Add(record);
            }
        }
        Walk(Root);return new JsonObject{["raw"]=result,["families"]=ReadJson(Path.Combine(meta,"familias-avatares.json"),new JsonObject()),["updated"]=DateTime.UtcNow.ToString("o")};
    }
    public async Task DeleteProduct(JsonObject data)
    {
        var name=data["name"]?.ToString();
        if(string.IsNullOrWhiteSpace(name)||data["confirmation"]?.ToString()!=name)throw new InvalidOperationException("El nombre no coincide");
        var relative=(data["files"] as JsonArray??new()).Select(f=>f!.ToString()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if(relative.Length==0)throw new InvalidOperationException("No hay archivos para eliminar");
        await writes.WaitAsync();try {
            var paths=relative.Select(r=>SafePath(r)).ToArray();
            foreach(var file in paths) {
                if(!File.Exists(file)||Path.GetRelativePath(Root,file).Split(Path.DirectorySeparatorChar).Length<2||Path.GetRelativePath(Root,file).StartsWith("99_Catalogo"+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Archivo no permitido");
            }
            foreach(var file in paths)Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(file,Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin,Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);
            foreach(var dir in paths.Select(Path.GetDirectoryName).Distinct().OrderByDescending(d=>d!.Length)) {
                if(dir!=null&&Path.GetRelativePath(Root,dir).Split(Path.DirectorySeparatorChar).Length>=2&&Directory.Exists(dir)&&!Directory.EnumerateFileSystemEntries(dir).Any())Directory.Delete(dir);
            }
        } finally {writes.Release();}
    }
    public void OpenArchive(string relative)
    {
        var file=SafePath(relative);
        if(!File.Exists(file)||!new[]{".zip",".rar",".7z"}.Contains(Path.GetExtension(file).ToLowerInvariant()))throw new InvalidOperationException("Solo se pueden abrir archivos ZIP, RAR o 7z");
        Process.Start(new ProcessStartInfo(file){UseShellExecute=true});
    }
    public void OpenFolder(string relative)
    {var folder=SafePath(relative);if(!Directory.Exists(folder))throw new InvalidOperationException("No es carpeta");Process.Start(new ProcessStartInfo("explorer.exe"){UseShellExecute=true,Arguments="\""+folder+"\""});}
    public static bool ValidName(string? name)=> !string.IsNullOrWhiteSpace(name)&&name==name.Trim()&&name.Length<=180&&name.IndexOfAny(Path.GetInvalidFileNameChars())<0&&!name.EndsWith('.')&&name!=".."&&!Regex.IsMatch(name,@"^(con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\.|$)",RegexOptions.IgnoreCase);
    public static readonly HashSet<string> Categories=new(){"01_Avatares","02_Ropa","03_Accesorios","04_Cabello","05_Texturas","06_Animaciones_y_Presets","07_Herramientas_y_Sistemas","08_Mundos","09_R18","10_Face_Tracking","11_VRM_y_Shaders","12_Mochi_Fitter"};
    sealed class Upload { public required string Relative;public required string WorkRelative;public required JsonObject Data;public required Dictionary<string,long> Files;public HashSet<string> Done=new(StringComparer.OrdinalIgnoreCase);public bool Busy;public string? Archive;public HashSet<string> Cleaned=new(StringComparer.OrdinalIgnoreCase); }
    readonly Dictionary<string,Upload> uploads=new();
    public string StartUpload(JsonObject data)
    {
        var name=data["name"]?.ToString();var category=data["category"]?.ToString();
        var avatars=(data["avatars"] as JsonArray??new()).Select(a=>a!.ToString()).Distinct().ToArray();
        if(data["generic"]?.GetValue<bool>()==true)avatars=Array.Empty<string>();
        var files=data["files"] as JsonArray??throw new InvalidOperationException("Faltan archivos");
        if(!ValidName(name)||category==null||!Categories.Contains(category)||!avatars.All(ValidName)||files.Count<1||files.Count>100)throw new InvalidOperationException("Datos del producto inválidos");
        var expected=new Dictionary<string,long>(StringComparer.OrdinalIgnoreCase);
        foreach(var f in files){var n=f?["name"]?.ToString();var size=f?["size"]?.GetValue<long>()??-1;if(!ValidName(n)||size<0||!expected.TryAdd(n!,size))throw new InvalidOperationException("Archivos repetidos o inválidos");}
        var parts=new List<string>{category};if(new[]{"02_Ropa","03_Accesorios","05_Texturas","06_Animaciones_y_Presets","09_R18"}.Contains(category))parts.Add(avatars.Length==1?avatars[0]:"Mix");parts.Add(name!);
        var relative=Path.Combine(parts.ToArray());var folder=SafePath(relative,false);
        if(File.Exists(folder)||Directory.Exists(folder))throw new InvalidOperationException("Ya existe un producto con ese nombre");
        var token=Guid.NewGuid().ToString("N");
        var workRelative=Path.Combine("99_Catalogo",".mochi-uploads",token);
        Directory.CreateDirectory(SafePath(workRelative,false));SafePath(workRelative);
        var saved=(JsonObject)data.DeepClone();saved["avatars"]=new JsonArray(avatars.Select(a=>JsonValue.Create(a)).ToArray());
        uploads.Add(token,new Upload{Relative=relative,WorkRelative=workRelative,Data=saved,Files=expected});return token;
    }
    public void CancelUpload(string token)
    {
        if(!uploads.TryGetValue(token,out var session))return;
        if(session.Busy)throw new InvalidOperationException("La carga está ocupada");
        var expected=Path.Combine("99_Catalogo",".mochi-uploads",token);
        if(!Regex.IsMatch(token,"^[a-f0-9]{32}$")||session.WorkRelative!=expected)throw new InvalidOperationException("Carga no válida");
        var folder=SafePath(session.WorkRelative);
        foreach(var file in Directory.GetFiles(folder)){SafePath(Path.GetRelativePath(Root,file));File.Delete(file);}
        Directory.Delete(folder,false);uploads.Remove(token);
    }
    public async Task UploadFile(string token,string name,Stream input)
    {
        if(!uploads.TryGetValue(token,out var session)||session.Busy||session.Done.Contains(name)||!session.Files.TryGetValue(name,out var expected))throw new InvalidOperationException("Carga inválida");
        var folder=SafePath(session.WorkRelative);var target=Path.Combine(folder,name);session.Busy=true;bool created=false;
        try{await using(var output=new FileStream(target,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,true)){created=true;long count=0;var buffer=new byte[81920];int read;while((read=await input.ReadAsync(buffer))>0){count+=read;if(count>expected)throw new InvalidOperationException("Tamaño incorrecto");await output.WriteAsync(buffer.AsMemory(0,read));}if(count!=expected)throw new InvalidOperationException("Carga incompleta");}session.Done.Add(name);}
        catch{if(created)File.Delete(target);throw;}finally{session.Busy=false;}
    }
    async Task PackFiles(Upload session)
    {
        if(session.Archive!=null)return;
        var names=session.Files.Keys.Where(n=>!System.Text.RegularExpressions.Regex.IsMatch(n,@"^__preview\.(png|jpe?g|webp)$",RegexOptions.IgnoreCase)).ToArray();
        if(names.Length==0)throw new InvalidOperationException("Faltan archivos del producto");
        var folder=SafePath(session.WorkRelative);
        var temp=Path.Combine(folder,".mochi-"+Guid.NewGuid().ToString("N")+".tmp");
        var hashes=new Dictionary<string,byte[]>(StringComparer.OrdinalIgnoreCase);
        try
        {
            await using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.ReadWrite,FileShare.None,81920,true))
            {
                using var zip=new ZipArchive(stream,ZipArchiveMode.Create,true);
                foreach(var name in names)
                {
                    var source=SafePath(Path.Combine(session.WorkRelative,name));
                    await using var input=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.Read,81920,true);
                    if(input.Length!=session.Files[name])throw new InvalidOperationException("Cambió un archivo durante la carga");
                    hashes[name]=await SHA256.HashDataAsync(input);input.Position=0;
                    var entry=zip.CreateEntry(name,CompressionLevel.Optimal);await using var output=entry.Open();await input.CopyToAsync(output);
                }
            }
            using(var zip=ZipFile.OpenRead(temp))
            {
                if(zip.Entries.Count!=names.Length)throw new InvalidOperationException("No se pudo verificar el ZIP");
                foreach(var entry in zip.Entries)
                {
                    if(!session.Files.TryGetValue(entry.FullName,out var expected)||entry.Length!=expected)throw new InvalidOperationException("Contenido ZIP incompleto");
                    await using var content=entry.Open();var hash=await SHA256.HashDataAsync(content);
                    if(!hash.SequenceEqual(hashes[entry.FullName]))throw new InvalidOperationException("Falló la verificación del ZIP");
                }
            }
            session.Archive=temp;
        }
        finally {if(session.Archive!=temp&&File.Exists(temp))File.Delete(temp);}
    }
    public async Task<string> FinishUpload(string token)
    {
        await writes.WaitAsync();Upload? session=null;
        try
        {
            if(!uploads.TryGetValue(token,out session)||session.Busy||session.Done.Count!=session.Files.Count)throw new InvalidOperationException("Faltan archivos por subir");
            session.Busy=true;await PackFiles(session);
            var file=Path.Combine(Root,"99_Catalogo","compatibilidades-confirmadas.json");SafePath(Path.GetRelativePath(Root,file),false);
            var data=ReadJson(file,new JsonArray()).AsArray();var tags=(session.Data["tags"]?.ToString()??"").Split(',').Select(t=>t.Trim()).Where(t=>t.Length>0).Select(t=>"tag:"+t).ToList();
            if(session.Data["adult"]?.GetValue<bool>()==true||session.Data["category"]?.ToString()=="09_R18")tags.Add("contenido:r18");
            var key=session.Relative+"\\";for(int i=data.Count-1;i>=0;i--)if(data[i]?["producto"]?.ToString()==key)data.RemoveAt(i);
            data.Insert(0,new JsonObject{["producto"]=key,["avatares"]=session.Data["avatars"]?.DeepClone()??new JsonArray(),["generico"]=session.Data["generic"]?.GetValue<bool>()??false,["pendientes"]=0,["etiquetas"]=new JsonArray(tags.Select(t=>JsonValue.Create(t)).ToArray())});
            await AtomicWrite(file,data);
            // Only remove copies from this upload, after checking every ZIP entry with SHA-256.
            foreach(var name in session.Files.Keys.Where(n=>!Regex.IsMatch(n,@"^__preview\.(png|jpe?g|webp)$",RegexOptions.IgnoreCase)))
            {
                if(session.Cleaned.Contains(name))continue;
                var source=SafePath(Path.Combine(session.WorkRelative,name),false);if(File.Exists(source))File.Delete(source);session.Cleaned.Add(name);
            }
            var productName=Path.GetFileName(session.Relative);
            var archivePath=SafePath(Path.Combine(session.WorkRelative,productName+".zip"),false);
            if(!string.Equals(session.Archive,archivePath,StringComparison.OrdinalIgnoreCase)){File.Move(session.Archive!,archivePath,false);session.Archive=archivePath;}
            foreach(var preview in session.Files.Keys.Where(n=>Regex.IsMatch(n,@"^__preview\.(png|jpe?g|webp)$",RegexOptions.IgnoreCase)))
            {
                var source=SafePath(Path.Combine(session.WorkRelative,preview),false);
                var destination=SafePath(Path.Combine(session.WorkRelative,productName+Path.GetExtension(preview).ToLowerInvariant()),false);
                if(File.Exists(source))File.Move(source,destination,false);
            }
            var finalDestination=SafePath(session.Relative,false);
            if(Directory.Exists(finalDestination)||File.Exists(finalDestination))throw new InvalidOperationException("Ya existe un producto con ese nombre");
            var parent=Path.GetDirectoryName(finalDestination)!;Directory.CreateDirectory(parent);SafePath(Path.GetRelativePath(Root,parent));
            Directory.Move(SafePath(session.WorkRelative),finalDestination);
            uploads.Remove(token);return session.Relative;
        }
        finally{if(session!=null)session.Busy=false;writes.Release();}
    }
}

