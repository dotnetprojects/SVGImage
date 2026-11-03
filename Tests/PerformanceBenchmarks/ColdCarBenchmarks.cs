using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SVGImage.SVG;
using SVGImage.SVG.FileLoaders;

internal static class ColdCarBenchmarks
{
    internal static void Run(string repo,int iterations,string baseline=null)
    {
        if(iterations<1) throw new ArgumentOutOfRangeException(nameof(iterations));
        string current=Assembly.GetExecutingAssembly().Location,path=Path.Combine(repo,"Samples/Example/Images/171.svg");
        for(int i=0;i<iterations;i++)
        {
            // Alternate pair order to reduce clock/load/OS-cache bias.
            if(baseline!=null && i%2==0) Measure(baseline,path,i+1,"baseline");
            Measure(current,path,i+1,"candidate");
            if(baseline!=null && i%2!=0) Measure(baseline,path,i+1,"baseline");
        }
    }
    private static void Measure(string assembly,string path,int iteration,string variant)
    {
        var info=new ProcessStartInfo("dotnet") {UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        info.ArgumentList.Add(assembly);
        info.ArgumentList.Add("cold-car-child");
        info.ArgumentList.Add(path);
        var watch=Stopwatch.StartNew();
        using(var process=Process.Start(info))
        {
            var errors=process.StandardError.ReadToEndAsync();
            double processPreview=0,processFull=0;JsonElement? result=null;
            string line;
            while((line=process.StandardOutput.ReadLine())!=null)
            {
                using(var document=JsonDocument.Parse(line))
                {
                    var item=document.RootElement;
                    switch(item.GetProperty("stage").GetString())
                    {
                        case "preview":processPreview=watch.Elapsed.TotalMilliseconds;break;
                        case "full":processFull=watch.Elapsed.TotalMilliseconds;break;
                        case "result":result=item.GetProperty("sample").Clone();break;
                    }
                }
            }
            process.WaitForExit();
            string errorText=errors.GetAwaiter().GetResult();
            if(process.ExitCode!=0 || result==null) throw new Exception("Cold child failed: "+errorText);
            Console.WriteLine(JsonSerializer.Serialize(new {iteration=iteration,variant=variant,processToPreviewMilliseconds=processPreview,processToFullMilliseconds=processFull,sample=result.Value}));
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Child(string path)
    {
        var watch=Stopwatch.StartNew();
        DrawingGroup preview=null,full=null;Exception error=null;
        double rendererMs=0,parseMs=0,previewMs=0,previewFreezeMs=0,fullMs=0,fullFreezeMs=0,previewReady=0,fullReady=0;
        var worker=new Thread(()=>
        {
            try
            {
                double start=watch.Elapsed.TotalMilliseconds;
                var renderer=new SVGRender {UseAnimations=false,UseFilters=false};
                rendererMs=watch.Elapsed.TotalMilliseconds-start;
                start=watch.Elapsed.TotalMilliseconds;
                var svg=new SVGImage.SVG.SVG(path,FileSystemLoader.Instance);
                parseMs=watch.Elapsed.TotalMilliseconds-start;
                start=watch.Elapsed.TotalMilliseconds;preview=renderer.CreateDrawing(svg);
                previewMs=watch.Elapsed.TotalMilliseconds-start;
                start=watch.Elapsed.TotalMilliseconds;preview.Freeze();
                previewFreezeMs=watch.Elapsed.TotalMilliseconds-start;previewReady=watch.Elapsed.TotalMilliseconds;
                Console.WriteLine("{\"stage\":\"preview\"}");
                renderer.UseFilters=true;
                start=watch.Elapsed.TotalMilliseconds;full=renderer.CreateDrawing(svg);
                fullMs=watch.Elapsed.TotalMilliseconds-start;
                start=watch.Elapsed.TotalMilliseconds;full.Freeze();
                fullFreezeMs=watch.Elapsed.TotalMilliseconds-start;fullReady=watch.Elapsed.TotalMilliseconds;
                Console.WriteLine("{\"stage\":\"full\"}");
            }
            catch(Exception ex) {error=ex;}
        }) {IsBackground=true};
        worker.SetApartmentState(ApartmentState.STA);worker.Start();worker.Join();
        if(error!=null) throw error;
        var previewHash=Pixels(preview);
        var fullHash=Pixels(full);
        if(previewHash==fullHash) throw new Exception("Filtered car must differ from preview");
        if(!preview.IsFrozen || !full.IsFrozen) throw new Exception("Drawings must be frozen before crossing threads");
        var hashes=new List<string>();Program.BitmapHashes(full,hashes);
        string bitmapHash=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("|",hashes))));
        Console.WriteLine(JsonSerializer.Serialize(new {stage="result",sample=new {rendererMilliseconds=rendererMs,parseMilliseconds=parseMs,previewDrawMilliseconds=previewMs,previewFreezeMilliseconds=previewFreezeMs,fullDrawMilliseconds=fullMs,fullFreezeMilliseconds=fullFreezeMs,previewReadyMilliseconds=previewReady,fullReadyMilliseconds=fullReady,previewPixelHash=previewHash,fullPixelHash=fullHash,filterBitmapHash=bitmapHash,bitmapCount=hashes.Count}}));
    }
    private static string Pixels(Drawing drawing)
    {
        var visual=new DrawingVisual();using(var dc=visual.RenderOpen())dc.DrawDrawing(drawing);
        var bitmap=new RenderTargetBitmap(900,600,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);
        var pixels=new byte[900*600*4];bitmap.CopyPixels(pixels,3600,0);
        bool visible=false;for(int i=3;i<pixels.Length;i+=4) if(pixels[i]>0) {visible=true;break;}
        if(!visible)throw new Exception("Blank WPF output; restore the Windows graphics session");
        return Convert.ToHexString(SHA256.HashData(pixels));
    }
}
