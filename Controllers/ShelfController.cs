using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
namespace VrcShelf.Desktop;
internal sealed class ShelfController(
    CoreWebView2Environment environment, string web, Func<LibraryStore> getStore,
    Func<object> settingsInfo, Func<Task> chooseLibrary)
{
    static bool IsInternal(string uri)=>ShelfWindow.IsInternal(uri);
    static Dictionary<string,string> Query(Uri uri)=>uri.Query.TrimStart('?').Split('&',StringSplitOptions.RemoveEmptyEntries).Select(s=>s.Split('=',2)).ToDictionary(p=>Uri.UnescapeDataString(p[0]),p=>p.Length>1?Uri.UnescapeDataString(p[1].Replace('+',' ')):"");
    static string Mime(string file)=>Path.GetExtension(file).ToLowerInvariant() switch{".html"=>"text/html; charset=utf-8",".js"=>"text/javascript; charset=utf-8",".css"=>"text/css; charset=utf-8",".json"=>"application/json",".png"=>"image/png",".jpg" or ".jpeg"=>"image/jpeg",".webp"=>"image/webp",".svg"=>"image/svg+xml",".pdf"=>"application/pdf",_=>"application/octet-stream"};
    async Task<JsonObject> Body(CoreWebView2WebResourceRequest request)
    {if(request.Content==null)return new();using var reader=new StreamReader(request.Content,Encoding.UTF8,leaveOpen:true);var text=await reader.ReadToEndAsync();if(text.Length>2_000_000)throw new InvalidOperationException("Demasiados datos");return JsonNode.Parse(text)?.AsObject()??new();}
    void Json(CoreWebView2WebResourceRequestedEventArgs e,object data,int status=200)
    {var stream=new MemoryStream(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data));e.Response=environment.CreateWebResourceResponse(stream,status,status==200?"OK":"Error","Content-Type: application/json; charset=utf-8\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff");}
    void FileResponse(CoreWebView2WebResourceRequestedEventArgs e,string file)
    {e.Response=environment.CreateWebResourceResponse(File.OpenRead(file),200,"OK","Content-Type: "+Mime(file)+"\r\nCache-Control: no-cache\r\nX-Content-Type-Options: nosniff");}
    public async void ResourceRequested(object? sender,CoreWebView2WebResourceRequestedEventArgs e)
    {
        using var deferral=e.GetDeferral();
        try
        {
            var store=getStore();var request=e.Request;if(!IsInternal(request.Uri)){Json(e,new{error="Recurso externo bloqueado"},403);return;}
            var uri=new Uri(request.Uri);var route=uri.AbsolutePath;var query=Query(uri);var method=request.Method;
            if(method=="POST"&&(!request.Headers.Contains("Origin")||request.Headers.GetHeader("Origin")!="https://shelf.internal")){Json(e,new{error="Origen no permitido"},403);return;}
            if(route=="/api/desktop/settings"&&method=="GET"){Json(e,settingsInfo());return;}
            if(route=="/api/desktop/choose-library"&&method=="POST"){await chooseLibrary();Json(e,settingsInfo());return;}
            if(route=="/api/catalog"&&method=="GET"){var result=await Task.Run(store.Catalog);Json(e,result);return;}
            if(route=="/api/personal")
            {if(method=="GET"){Json(e,store.Personal());return;}if(method=="POST"){await store.SavePersonal(await Body(request));Json(e,new{ok=true});return;}}
            if(route=="/api/file"&&method=="GET"){var file=store.SafePath(query.GetValueOrDefault("path", ""));if(!File.Exists(file))throw new FileNotFoundException();FileResponse(e,file);return;}
            if(route=="/api/delete-product"&&method=="POST"){await store.DeleteProduct(await Body(request));Json(e,new{ok=true});return;}
            if(route=="/api/open-file"&&method=="POST"){store.OpenArchive((await Body(request))["path"]?.ToString()??"");Json(e,new{ok=true});return;}
            if(route=="/api/open-folder"&&method=="POST"){store.OpenFolder((await Body(request))["path"]?.ToString()??"");Json(e,new{ok=true});return;}
            if(route=="/api/upload/cancel"&&method=="POST"){store.CancelUpload((await Body(request))["token"]?.ToString()??"");Json(e,new{ok=true});return;}
            if(route=="/api/upload/start"&&method=="POST"){var token=store.StartUpload(await Body(request));Json(e,new{token});return;}
            if(route=="/api/upload/file"&&method=="POST"){await store.UploadFile(query.GetValueOrDefault("token",""),query.GetValueOrDefault("name",""),request.Content??Stream.Null);Json(e,new{ok=true});return;}
            if(route=="/api/upload/finish"&&method=="POST"){var result=await store.FinishUpload((await Body(request))["token"]?.ToString()??"");Json(e,new{ok=true,path=result});return;}
            if(route.StartsWith("/api/")){Json(e,new{error="No encontrado"},404);return;}
            if(method!="GET"){Json(e,new{error="Método no permitido"},405);return;}
            var relative=Uri.UnescapeDataString(route).TrimStart('/').Replace('/',Path.DirectorySeparatorChar);if(relative.Length==0)relative="index.html";
            var target=Path.GetFullPath(Path.Combine(web,relative));if(!target.StartsWith(web+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Ruta inválida");
            FileResponse(e,target);
        }
        catch(Exception ex){Json(e,new{error=ex.Message},ex is FileNotFoundException or DirectoryNotFoundException?404:400);}
    }
}

