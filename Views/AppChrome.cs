using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace VrcShelf.Desktop;
internal static class AppChrome
{
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] static extern bool DestroyIcon(IntPtr icon);
    static int ColorRef(int red,int green,int blue)=>red|(green<<8)|(blue<<16);
    public static void Apply(IntPtr handle)
    {
        if(!OperatingSystem.IsWindowsVersionAtLeast(10))return;
        var dark=1;DwmSetWindowAttribute(handle,20,ref dark,sizeof(int));
        var caption=ColorRef(29,24,35);DwmSetWindowAttribute(handle,35,ref caption,sizeof(int));
        var text=ColorRef(243,238,245);DwmSetWindowAttribute(handle,36,ref text,sizeof(int));
        var border=ColorRef(58,50,66);DwmSetWindowAttribute(handle,34,ref border,sizeof(int));
    }
    public static Icon CreateIcon()
    {
        using var bitmap=new Bitmap(64,64);
        using(var graphics=Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode=SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            using var shape=new GraphicsPath();
            shape.AddArc(2,2,24,24,180,90);shape.AddArc(38,2,24,24,270,90);
            shape.AddArc(38,38,24,24,0,90);shape.AddArc(2,38,24,24,90,90);shape.CloseFigure();
            using var pink=new SolidBrush(Color.FromArgb(244,169,198));graphics.FillPath(pink,shape);
            using var ink=new SolidBrush(Color.FromArgb(42,21,32));
            graphics.FillPolygon(ink,new PointF[]{new(32,11),new(38,25),new(53,32),new(38,39),new(32,53),new(26,39),new(11,32),new(26,25)});
        }
        var handle=bitmap.GetHicon();try{using var temporary=Icon.FromHandle(handle);return (Icon)temporary.Clone();}finally{DestroyIcon(handle);}
    }
}
