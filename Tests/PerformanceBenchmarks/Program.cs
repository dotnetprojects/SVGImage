using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SVGImage.SVG;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        string repo=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
        int iterations=args.Length>0?int.Parse(args[0]):5;
        if(iterations<1) throw new ArgumentOutOfRangeException(nameof(iterations));
        bool carOnly=args.Length>1 && args[1]=="car";
        if(!carOnly)
        {
        Benchmark("nested groups 16",()=>new SVGRender().LoadXmlDrawing(Nested(16)),new Rect(0,0,100,100),iterations);
        Benchmark("nested groups 20",()=>new SVGRender().LoadXmlDrawing(Nested(20)),new Rect(0,0,100,100),iterations);
        Benchmark("color matrix",()=>Filter("<feColorMatrix type='saturate' values='.35'/>","sRGB"),new Rect(0,0,300,200),iterations);
        Benchmark("multiply blend",()=>Filter("<feFlood flood-color='#a863c4' flood-opacity='.7' result='b'/><feBlend in='SourceGraphic' in2='b' mode='multiply'/>","sRGB"),new Rect(0,0,300,200),iterations);
        Benchmark("hue blend",()=>Filter("<feFlood flood-color='#a863c4' flood-opacity='.7' result='b'/><feBlend in='SourceGraphic' in2='b' mode='hue'/>","sRGB"),new Rect(0,0,300,200),iterations);
        Benchmark("linear filter chain",()=>Filter("<feColorMatrix type='saturate' values='.35'/><feGaussianBlur stdDeviation='2'/><feOffset dx='2' dy='1'/>","linearRGB"),new Rect(0,0,300,200),iterations);
        }
        foreach(string file in new[]{"tiger.svg","acid1.svg","171.svg"}.Where(file=>!carOnly || file=="171.svg"))
        {
            string path=Path.Combine(repo,"Samples/Example/Images",file);
            var reference=new SVGRender(); reference.LoadDrawing(path);
            var box=reference.SVG.ViewBox ?? new Rect(reference.SVG.Size);
            Benchmark(file+" load",()=>new SVGRender().LoadDrawing(path),box,iterations);
            Benchmark(file+" redraw",()=>reference.CreateDrawing(reference.SVG),box,iterations);
        }
    }
    private static string Nested(int depth)=>"<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'>"+string.Concat(Enumerable.Repeat("<g>",depth))+"<rect x='10' y='10' width='80' height='80'/>"+string.Concat(Enumerable.Repeat("</g>",depth))+"</svg>";
    private static Drawing Filter(string primitives,string space)=>new SVGRender().LoadXmlDrawing("<svg xmlns='http://www.w3.org/2000/svg' width='300' height='200'><defs><linearGradient id='g'><stop stop-color='#6cd2a8' stop-opacity='.4'/><stop offset='1' stop-color='#235b9c'/></linearGradient><filter id='f' filterUnits='userSpaceOnUse' x='0' y='0' width='300' height='200' color-interpolation-filters='"+space+"'>"+primitives+"</filter></defs><rect width='300' height='200' fill='url(#g)' filter='url(#f)'/></svg>");
    private static void Benchmark(string name,Func<Drawing> render,Rect box,int iterations)
    {
        render(); // Warm up JIT and WPF caches separately for each workload.
        var times=new double[iterations]; var allocations=new long[iterations]; Drawing drawing=null;
        for(int i=0;i<iterations;i++)
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            long before=GC.GetAllocatedBytesForCurrentThread(); var watch=Stopwatch.StartNew();
            drawing=render(); watch.Stop();
            times[i]=watch.Elapsed.TotalMilliseconds;
            allocations[i]=GC.GetAllocatedBytesForCurrentThread()-before;
        }
        var visual=new DrawingVisual();
        using(var dc=visual.RenderOpen())
        {
            dc.PushTransform(new MatrixTransform(300/box.Width,0,0,200/box.Height,-box.X*300/box.Width,-box.Y*200/box.Height));
            dc.DrawDrawing(drawing);
        }
        var bitmap=new RenderTargetBitmap(300,200,96,96,PixelFormats.Pbgra32); bitmap.Render(visual);
        var pixels=new byte[300*200*4]; bitmap.CopyPixels(pixels,1200,0);
        if(!pixels.Where((v,i)=>i%4==3).Any(v=>v>0)) throw new Exception("Blank benchmark image; restore the Windows desktop session");
        // Also check full-size output and every native filter bitmap, so a
        // downsampled thumbnail cannot hide a change in filter precision.
        var fullVisual=new DrawingVisual();
        using(var dc=fullVisual.RenderOpen())
        {
            dc.PushTransform(new MatrixTransform(900/box.Width,0,0,600/box.Height,-box.X*900/box.Width,-box.Y*600/box.Height));
            dc.DrawDrawing(drawing);
        }
        var fullBitmap=new RenderTargetBitmap(900,600,96,96,PixelFormats.Pbgra32);fullBitmap.Render(fullVisual);
        var fullPixels=new byte[900*600*4];fullBitmap.CopyPixels(fullPixels,3600,0);
        var hashes=new System.Collections.Generic.List<string>();
        BitmapHashes(drawing,hashes);
        string bitmapHash=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("|",hashes))));
        Array.Sort(times); Array.Sort(allocations);
        Console.WriteLine(JsonSerializer.Serialize(new {name,iterations,medianMilliseconds=times[iterations/2],allocatedBytes=allocations[iterations/2],pixelHash=Convert.ToHexString(SHA256.HashData(pixels)),fullPixelHash=Convert.ToHexString(SHA256.HashData(fullPixels)),filterBitmapHash=bitmapHash,bitmapCount=hashes.Count}));
    }
    private static void BitmapHashes(Drawing drawing,System.Collections.Generic.List<string> hashes)
    {
        if(drawing is DrawingGroup group)
            foreach(var child in group.Children) BitmapHashes(child,hashes);
        else if(drawing is ImageDrawing image && image.ImageSource is BitmapSource bitmap)
        {
            var converted=new FormatConvertedBitmap(bitmap,PixelFormats.Pbgra32,null,0);
            var bytes=new byte[bitmap.PixelWidth*bitmap.PixelHeight*4];converted.CopyPixels(bytes,bitmap.PixelWidth*4,0);
            hashes.Add(bitmap.PixelWidth+"x"+bitmap.PixelHeight+":"+Convert.ToHexString(SHA256.HashData(bytes)));
        }
    }

}
