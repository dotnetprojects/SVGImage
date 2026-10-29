using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SVGImage.SVG;

internal static class LightingImageBackdropTests
{
    private const string Filter = "<filter id='f' filterUnits='userSpaceOnUse' x='0' y='0' width='100' height='100' color-interpolation-filters='sRGB'>";
    private static Drawing Render(string primitives, string content = "<rect x='20' y='20' width='60' height='60' filter='url(#f)'/>")
        => GeometryTests.Svg(Filter + primitives + "</filter>" + content);
    internal static void Run()
    {
        foreach(bool enabled in new[]{true,false})
            if(!new SVGRender { UseFilters=enabled }.CreateDrawing(new SVGImage.SVG.SVG()).Bounds.IsEmpty)
                throw new Exception("An uninitialized SVG must render empty");
        Lighting(); Images(); Backdrops(); Samples();
        Console.WriteLine("Lighting, feImage and backdrop checks passed.");
    }
    private static void Lighting()
    {
        var d=Render("<feDiffuseLighting surfaceScale='0' lighting-color='rgba(40,120,200,.1)'><feDistantLight elevation='90'/></feDiffuseLighting>");
        GeometryTests.Pixel(d,50,50,Color.FromRgb(40,120,200),"diffuse light ignores lighting-color alpha");
        GeometryTests.Pixel(d,5,5,Color.FromRgb(40,120,200),"diffuse output has opaque alpha outside the height map");
        d=Render("<feSpecularLighting surfaceScale='0' specularConstant='.5'><feDistantLight elevation='90'/></feSpecularLighting>");
        GeometryTests.Pixel(d,50,50,Color.FromArgb(128,255,255,255),"specular premultiplication and max-channel alpha",2);
        d=Render("<feSpecularLighting surfaceScale='0' specularConstant='5' lighting-color='seagreen'><feDistantLight elevation='90'/></feSpecularLighting>");
        GeometryTests.Pixel(d,50,50,Color.FromRgb(230,255,255),"large specular constant clamps without byte overflow",2);
        foreach(string exponent in new[]{"0","129"})
            GeometryTests.Pixel(Render("<feSpecularLighting specularExponent='"+exponent+"'><feDistantLight elevation='90'/></feSpecularLighting>"),50,50,Colors.Transparent,"invalid specular exponent");
        GeometryTests.Pixel(Render("<feDiffuseLighting/>"),50,50,Colors.Transparent,"missing light source");
        GeometryTests.Pixel(Render("<feDiffuseLighting surfaceScale='0'><title>ignored</title><feDistantLight elevation='90'/><feDistantLight elevation='0'/></feDiffuseLighting>"),50,50,Colors.White,"first valid light child");
        d=Render("<feDiffuseLighting surfaceScale='0'><fePointLight x='50' y='50' z='20'/></feDiffuseLighting>");
        GeometryTests.Pixel(d,50,50,Colors.White,"point light directly overhead",2);
        d=Render("<feDiffuseLighting surfaceScale='0'><feSpotLight x='50' y='50' z='20' pointsAtX='50' pointsAtY='50' limitingConeAngle='10'/></feDiffuseLighting>");
        GeometryTests.Pixel(d,50,50,Colors.White,"spotlight axis",2);
        GeometryTests.Pixel(d,10,10,Colors.Black,"spotlight cone excludes exterior");
        d=GeometryTests.Svg("<style>feDiffuseLighting {lighting-color:currentColor;color:rgb(20 50% 80)}</style>"+Filter+"<feDiffuseLighting surfaceScale='0'><feDistantLight elevation='90'/></feDiffuseLighting></filter><rect width='100' height='100' filter='url(#f)'/>");
        GeometryTests.Pixel(d,50,50,Color.FromRgb(20,128,80),"lighting CSS currentColor",1);
        var renderer=new SVGRender();
        foreach(double invalid in new[]{0,-1,double.NaN,double.PositiveInfinity})
        {
            bool rejected=false;
            try { renderer.RasterizationScale=invalid; } catch(ArgumentOutOfRangeException) { rejected=true; }
            if(!rejected) throw new Exception("Invalid filter sampling resolution accepted");
        }
    }
    private static void Images()
    {
        var encoder=new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(2,1,96,96,PixelFormats.Bgra32,null,new byte[]{0,0,255,255,255,0,0,255},8)));
        byte[] png; using(var stream=new MemoryStream()) { encoder.Save(stream); png=stream.ToArray(); }
        string data="data:image/png;base64,"+Convert.ToBase64String(png);
        var d=Render("<feImage href='"+data+"'/>");
        GeometryTests.Pixel(d,20,50,Colors.Red,"embedded raster left"); GeometryTests.Pixel(d,80,50,Colors.Blue,"embedded raster right");
        GeometryTests.Pixel(d,50,5,Colors.Transparent,"feImage default meet margin");
        d=Render("<feImage href='"+data+"' preserveAspectRatio='none'/>");
        GeometryTests.Pixel(d,20,5,Colors.Red,"feImage none stretches to subregion");
        d=Render("<feImage href='data:image/svg+xml,"+Uri.EscapeDataString("<svg xmlns='http://www.w3.org/2000/svg' width='10' height='10'><rect width='10' height='10' fill='lime'/></svg>")+"'/>");
        GeometryTests.Pixel(d,50,50,Colors.Lime,"percent encoded SVG data image");
        d=GeometryTests.Svg("<defs><rect id='r' x='10' y='10' width='20' height='20' fill='red'/>"+Filter+"<feImage href='#r' x='20' y='20' width='50' height='50'/></filter></defs><g filter='url(#f)'/>");
        GeometryTests.Pixel(d,35,35,Colors.Red,"local feImage uses referenced coordinates plus subregion origin");
        GeometryTests.Pixel(d,25,25,Colors.Transparent,"local feImage does not stretch object bounds");
        foreach(string href in new[]{"#missing","data:image/png;base64,broken","missing-image.png"})
            GeometryTests.Pixel(Render("<feImage href='"+href+"'/>"),50,50,Colors.Transparent,"unavailable image is transparent");
        d=GeometryTests.Svg("<defs>"+Filter+"<feImage href='#loop'/></filter><g id='loop' filter='url(#f)'/></defs><use href='#loop'/>");
        GeometryTests.Pixel(d,50,50,Colors.Transparent,"recursive feImage terminates");
        string folder=Path.Combine(Path.GetTempPath(),"SVGImage-filter-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllBytes(Path.Combine(folder,"pixels.png"),png);
            File.WriteAllText(Path.Combine(folder,"child.svg"),"<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><image href='pixels.png' width='100' height='100'/></svg>");
            File.WriteAllText(Path.Combine(folder,"main.svg"),"<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'>"+Filter+"<feImage href='child.svg'/></filter><g filter='url(#f)'/></svg>");
            var renderer=new SVGRender(); d=renderer.LoadDrawing(Path.Combine(folder,"main.svg"));
            GeometryTests.Pixel(d,20,50,Colors.Red,"external SVG keeps relative raster base");
            // OnLoad decoding must release file handles before the drawing dies.
            using(File.Open(Path.Combine(folder,"pixels.png"),FileMode.Open,FileAccess.ReadWrite,FileShare.None)) { }
        }
        finally { Directory.Delete(folder,true); }
    }
    private static void Backdrops()
    {
        string defs=Filter+"<feOffset in='BackgroundImage' dx='40'/></filter>";
        string shapes="<rect x='10' y='10' width='20' height='20' fill='red'/><g filter='url(#f)'/><rect x='10' y='50' width='20' height='20' fill='blue'/>";
        var d=GeometryTests.Svg(defs+"<g enable-background='new'>"+shapes+"</g>");
        GeometryTests.Pixel(d,55,15,Colors.Red,"backdrop contains preceding sibling");
        GeometryTests.Pixel(d,55,55,Colors.Transparent,"backdrop excludes later sibling");
        GeometryTests.Pixel(GeometryTests.Svg(defs+shapes),55,15,Colors.Transparent,"backdrop disabled without enable-background new");
        d=GeometryTests.Svg(defs+"<g enable-background='new'><rect x='10' y='10' width='20' height='20' fill='red'/><g enable-background='new'><g filter='url(#f)'/></g></g>");
        GeometryTests.Pixel(d,55,15,Colors.Transparent,"nested new resets parent backdrop");
        d=GeometryTests.Svg(defs+"<g enable-background='new'><rect x='10' y='10' width='20' height='20' fill='red'/><g enable-background='inherit'><g filter='url(#f)'/></g></g>");
        GeometryTests.Pixel(d,55,15,Colors.Transparent,"explicit inherit inherits new and resets backdrop");
        d=GeometryTests.Svg(defs+"<g enable-background='new 0 0 20 100'>"+shapes+"</g>");
        GeometryTests.Pixel(d,55,15,Colors.Red,"background region inside"); GeometryTests.Pixel(d,65,15,Colors.Transparent,"background region clips");
        d=GeometryTests.Svg(defs+"<g enable-background='new' opacity='.5'><rect x='10' y='10' width='20' height='20' fill='red'/><g transform='translate(10)' filter='url(#f)'/></g>");
        GeometryTests.Pixel(d,55,15,Color.FromArgb(128,255,0,0),"transformed backdrop and group opacity applied once",2);
        d=GeometryTests.Svg(defs.Replace("BackgroundImage","BackgroundAlpha")+"<g enable-background='new'>"+shapes+"</g>");
        GeometryTests.Pixel(d,55,15,Colors.Black,"BackgroundAlpha strips source color");
        var animatedRenderer=new SVGRender { UseAnimations=true };
        d=animatedRenderer.LoadXmlDrawing("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'>"+defs.Replace("<feOffset in='BackgroundImage' dx='40'/>","<feColorMatrix in='BackgroundImage' type='saturate' values='0'/>")+"<g enable-background='new'><rect x='10' y='10' width='20' height='20' fill='red'/><g filter='url(#f)'/></g></svg>");
        GeometryTests.Pixel(d,15,15,Color.FromRgb(54,54,54),"background matrix bypasses SourceGraphic vector optimization",2);
    }
    private static void Samples()
    {
        foreach(string file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory,"FilterFeatures"),"*.svg"))
        {
            var renderer=new SVGRender { UseAnimations=false }; var drawing=renderer.LoadDrawing(file);
            var box=renderer.SVG.ViewBox ?? new Rect(renderer.SVG.Size);
            var samplePixels=AnimationTests.Image(drawing,600,360,box);
            if(!samplePixels.Where((p,i)=>i%4==3).Any(p=>p>0)) throw new Exception("Blank filter sample: "+file);
        }
        var control=new SVGImage.SVG.SVGImage { UseAnimations=false };
        control.BeginInit(); control.Source="/RegressionTests;component/FilterFeatures/Images.svg"; control.EndInit();
        control.Width=720; control.Height=400;
        control.Measure(new Size(720,400)); control.Arrange(new Rect(0,0,720,400)); control.UpdateLayout();
        var bitmap=new RenderTargetBitmap(720,400,96,96,PixelFormats.Pbgra32); bitmap.Render(control);
        var pixels=new byte[720*400*4]; bitmap.CopyPixels(pixels,720*4,0);
        int at=(185*720+360)*4;
        if(pixels[at]<150 || pixels[at+1]<200 || pixels[at+2]<200) throw new Exception("Embedded feImage relative SVG resource failed");
    }
}
