using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
namespace VrcShelf.Desktop;
internal static class Program
{
    [STAThread] static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.ThreadException+=(_,e)=>MessageBox.Show(e.Exception.Message,"MochiMochi VRC");
        Application.Run(new ShelfWindow());
    }
}
