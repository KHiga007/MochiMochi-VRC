using System.IO.Compression;
using VrcShelf.Desktop;
using System.Text.Json.Nodes;
var folder=Path.Combine(Path.GetTempPath(),"shelf-desktop-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
void Check(bool condition,string message){if(!condition)throw new Exception(message);Console.WriteLine("PASS "+message);}
async Task Reject(Func<Task> action,string message){try{await action();}catch{Console.WriteLine("PASS "+message);return;}throw new Exception(message);}
try{
 var store=new LibraryStore(folder,Path.Combine(folder,"state","favorites.json"));
 await Reject(()=>Task.FromResult(store.SafePath("..\\escape",false)),"rechaza rutas fuera de biblioteca");
 await Reject(()=>Task.FromResult(store.SafePath(@"C:\Windows",false)),"rechaza rutas absolutas");
 var data=new JsonObject{["name"]="Producto",["category"]="02_Ropa",["avatars"]=new JsonArray("Airi"),["tags"]="Booth",["files"]=new JsonArray(new JsonObject{["name"]="__preview.jpg",["size"]=3L},new JsonObject{["name"]="Producto.zip",["size"]=4L},new JsonObject{["name"]="textura.png",["size"]=5L})};
 var token=store.StartUpload(data);await Reject(()=>store.FinishUpload(token),"impide finalizar con archivos pendientes");
 await store.UploadFile(token,"__preview.jpg",new MemoryStream("abc"u8.ToArray()));
 await Reject(()=>store.UploadFile(token,"__preview.jpg",new MemoryStream("abc"u8.ToArray())),"impide sobrescribir archivos");
 await store.UploadFile(token,"Producto.zip",new MemoryStream("1234"u8.ToArray()));await store.UploadFile(token,"textura.png",new MemoryStream("image"u8.ToArray()));var relative=await store.FinishUpload(token);
 Check(relative==@"02_Ropa\Airi\Producto","carpeta correcta según categoría y avatar");
 var archive=Directory.GetFiles(Path.Combine(folder,relative),"*.zip").Single();
 using(var zip=ZipFile.OpenRead(archive)){Check(zip.Entries.Count==2,"el ZIP contiene todos los archivos del paso 1");using var reader=new StreamReader(zip.GetEntry("Producto.zip")!.Open());Check(reader.ReadToEnd()=="1234","conserva contenido del paquete dentro del ZIP");Check(zip.GetEntry("textura.png")!=null,"las imágenes del paso 1 quedan dentro del ZIP");Check(zip.GetEntry("__preview.jpg")==null,"la portada queda fuera del ZIP");}
 Check(File.ReadAllText(Path.Combine(folder,relative,"Producto.jpg"))=="abc","portada usa el nombre del paso 2 y conserva su contenido");
 Check(Path.GetFileName(archive)=="Producto.zip","ZIP usa exactamente el nombre del paso 2");
 Check(!File.Exists(Path.Combine(folder,relative,"__preview.jpg")),"no deja el nombre interno de preview");
 Check(File.Exists(Path.Combine(folder,relative,"Producto.zip"))&&!File.Exists(Path.Combine(folder,relative,"textura.png")),"retira solo las copias verificadas del paso 1");
 await Reject(()=>Task.FromResult(store.StartUpload(data)),"impide sobrescribir producto existente");
 var raw=store.Catalog()["raw"]!.AsArray();Check(raw.Count==2,"catálogo contiene los dos archivos subidos");
 Check(raw.All(r=>r!["etiquetas"]!.AsArray().Any(t=>t!.ToString()=="tag:Booth")),"conserva tags en imágenes y paquetes");
 await store.SavePersonal(new JsonObject{[relative]=new JsonObject{["favorite"]=true}});Check(store.Personal()[relative]!["favorite"]!.GetValue<bool>(),"favoritos persisten en disco");
 await File.WriteAllTextAsync(Path.Combine(folder,relative,"nuevo.txt"),"nuevo");Check(store.Catalog()["raw"]!.AsArray().Any(r=>r!["nombre"]!.ToString()=="nuevo.txt"),"detecta archivos nuevos automáticamente");
 var wrong=(JsonObject)data.DeepClone();wrong["name"]="Incompleto";var t2=store.StartUpload(wrong);await Reject(()=>store.UploadFile(t2,"Producto.zip",new MemoryStream(new byte[1])),"rechaza carga truncada");Check(!File.Exists(Path.Combine(folder,"02_Ropa","Airi","Incompleto","Producto.zip")),"retira únicamente el archivo parcial creado");
 await Reject(()=>store.DeleteProduct(new JsonObject{["name"]="Producto",["confirmation"]="Otro",["files"]=new JsonArray(relative+"\\Producto.zip")}),"eliminar exige nombre exacto");
 await Reject(()=>store.DeleteProduct(new JsonObject{["name"]="Producto",["confirmation"]="Producto",["files"]=new JsonArray("..\\fuera.zip")}),"eliminar rechaza rutas fuera de biblioteca");
 Check(File.Exists(archive),"confirmaciones inválidas conservan los archivos");
 var generic=(JsonObject)data.DeepClone();generic["name"]="Generic Retained";generic["generic"]=true;generic["files"]=new JsonArray(new JsonObject{["name"]="a.zip",["size"]=1L});
 var genericToken=store.StartUpload(generic);await store.UploadFile(genericToken,"a.zip",new MemoryStream(new byte[]{1}));var genericPath=await store.FinishUpload(genericToken);Check(genericPath==@"02_Ropa\Mix\Generic Retained","Genérico usa Mix aunque conserve selección de avatar");
 Check(store.Catalog()["raw"]!.AsArray().Where(r=>r!["relativa"]!.ToString().Contains("Generic Retained")).All(r=>!r!["etiquetas"]!.AsArray().Any(t=>t!.ToString().StartsWith("avatar:"))),"Genérico no se guarda con compatibilidad específica retenida");
 var interrupted=(JsonObject)generic.DeepClone();interrupted["name"]="Interrupted";var first=store.StartUpload(interrupted);await store.UploadFile(first,"a.zip",new MemoryStream(new byte[]{1}));Check(!Directory.Exists(Path.Combine(folder,"02_Ropa","Mix","Interrupted")),"una carga incompleta no publica la carpeta del producto");
 Check(!store.Catalog()["raw"]!.AsArray().Any(r=>r!["nombre"]!.ToString()=="a.zip"),"catálogo excluye las cargas temporales");
 var second=store.StartUpload(interrupted);store.CancelUpload(first);store.CancelUpload(second);Check(!Directory.Exists(Path.Combine(folder,"99_Catalogo",".mochi-uploads",first)),"cancelar retira solo los temporales de esa sesión");
 var afterRestart=new LibraryStore(folder,Path.Combine(folder,"state","favorites.json"));var retry=afterRestart.StartUpload(interrupted);afterRestart.CancelUpload(retry);Check(true,"puede reutilizar el nombre de una carga interrumpida");
 Console.WriteLine("TODAS LAS COMPROBACIONES PASARON");
}finally{if(Path.GetFileName(folder).StartsWith("shelf-desktop-check-"))Directory.Delete(folder,true);}

