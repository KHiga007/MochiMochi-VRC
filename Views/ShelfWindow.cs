using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
namespace VrcShelf.Desktop;
public sealed class ShelfWindow:Form
{
    readonly WebView2 browser=new(){Dock=DockStyle.Fill};
    readonly string appData=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VrcShelfDesktop");
    readonly string web=Path.Combine(AppContext.BaseDirectory,"web");
    LibraryStore store=null!;
    public ShelfWindow()
    {
        Text="MochiMochi VRC";Width=1380;Height=920;MinimumSize=new(420,560);StartPosition=FormStartPosition.CenterScreen;
        Icon=AppChrome.CreateIcon(); BackColor=Color.FromArgb(29,24,35);
        Directory.CreateDirectory(appData);LoadStore();
        Controls.Add(browser);Shown+=Initialize;
    }
    protected override void OnHandleCreated(EventArgs e) {base.OnHandleCreated(e);AppChrome.Apply(Handle);}
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
    {
        if(keyData==Keys.F5){if(browser.CoreWebView2!=null)_=browser.CoreWebView2.ExecuteScriptAsync("window.dispatchEvent(new Event('shelf-refresh'))");return true;}
        return base.ProcessCmdKey(ref msg,keyData);
    }
    object SettingsInfo()=>new{libraryRoot=store.Root,dataPath=appData,version=typeof(Program).Assembly.GetName().Version?.ToString(3)};
    void LoadStore()
    {
        var config=LibraryStore.ReadJson(Path.Combine(appData,"settings.json"),new JsonObject());
        var root=Environment.GetEnvironmentVariable("VRC_LIBRARY_ROOT")??config["libraryRoot"]?.ToString()??@"H:\Unity Asset";
        var state=Path.Combine(appData,"personal-data.json");
        // Import the existing web favorites once. The web's original is never overwritten.
        var old=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","vrc-shelf","personal-data.json"));
        var bundled=Path.Combine(AppContext.BaseDirectory,"initial-personal-data.json");
        if(!File.Exists(state)){if(File.Exists(bundled))File.Copy(bundled,state);else if(File.Exists(old))File.Copy(old,state);}
        store=new(root,state);
    }
    async Task ChooseLibrary(IWin32Window owner)
    {
        using var picker=new FolderBrowserDialog{Description="Elige la carpeta raíz de Unity Asset",UseDescriptionForTitle=true};
        if(picker.ShowDialog(owner)!=DialogResult.OK)return;
        try{await LibraryStore.AtomicWrite(Path.Combine(appData,"settings.json"),new JsonObject{["libraryRoot"]=picker.SelectedPath});LoadStore();}catch(Exception e){MessageBox.Show(e.Message,Text);}
    }
    DateTime lastRendererRecovery;
    async void Initialize(object? sender,EventArgs args)
    {
        try
        {
            if(!File.Exists(Path.Combine(web,"index.html")))throw new FileNotFoundException("Falta la interfaz web. Ejecuta Preparar-App.ps1.");
            var environment=await CoreWebView2Environment.CreateAsync(null,Path.Combine(appData,"WebView2"));
            await browser.EnsureCoreWebView2Async(environment);
            browser.CoreWebView2.Settings.AreDevToolsEnabled=false;browser.CoreWebView2.Settings.IsStatusBarEnabled=false;
            browser.CoreWebView2.AddWebResourceRequestedFilter("*",CoreWebView2WebResourceContext.All);
            browser.CoreWebView2.WebResourceRequested+=new ShelfController(browser.CoreWebView2.Environment,web,()=>store,SettingsInfo,()=>ChooseLibrary(this)).ResourceRequested;
            browser.CoreWebView2.ProcessFailed+=(_, failure)=>
            {
                if(failure.ProcessFailedKind!=CoreWebView2ProcessFailedKind.RenderProcessExited || IsDisposed || !IsHandleCreated)return;
                BeginInvoke((Action)(()=>
                {
                    if(IsDisposed)return;
                    var now=DateTime.UtcNow;
                    if(now-lastRendererRecovery<TimeSpan.FromSeconds(30))
                    {
                        MessageBox.Show(this,"La interfaz se cerró repetidamente. Cierra y vuelve a abrir MochiMochi VRC.",Text);
                        return;
                    }
                    lastRendererRecovery=now;
                    browser.CoreWebView2.Reload();
                }));
            };
            browser.CoreWebView2.NavigationStarting+=(_,e)=>{if(!IsInternal(e.Uri)){e.Cancel=true;OpenExternal(e.Uri);}};
            browser.CoreWebView2.NewWindowRequested+=(_,e)=>{e.Handled=true;OpenExternal(e.Uri);};
            await browser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.__VRC_DESKTOP__=true;");
            browser.CoreWebView2.Navigate("https://shelf.internal/");
        }
        catch(Exception e){MessageBox.Show("No se pudo abrir la aplicación.\n"+e.Message+"\n\nRequiere Microsoft Edge WebView2 Runtime.",Text,MessageBoxButtons.OK,MessageBoxIcon.Error);Close();}
    }
    internal static bool IsInternal(string uri)=>Uri.TryCreate(uri,UriKind.Absolute,out var u)&&u.Scheme=="https"&&u.Host=="shelf.internal"&&u.IsDefaultPort;
    static void OpenExternal(string uri){if(Uri.TryCreate(uri,UriKind.Absolute,out var u)&&u.Scheme is "https" or "http")Process.Start(new ProcessStartInfo(uri){UseShellExecute=true});}
}