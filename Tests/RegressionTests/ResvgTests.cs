using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SVGImage.SVG;

internal static class ResvgTests
{
    internal static void Run()
    {
        string root=Environment.GetEnvironmentVariable("RESVG_TEST_SUITE");
        if(string.IsNullOrEmpty(root)) return;
        string tests=Path.Combine(root,"tests");
        var groups=new[] { "shapes/path","shapes/polygon","shapes/polyline", "filters/feBlend", "filters/feComponentTransfer", "filters/feComposite", "filters/feFlood", "filters/feGaussianBlur", "filters/feMerge", "filters/feMorphology", "filters/feOffset", "filters/feTile", "filters/feColorMatrix", "filters/flood-color", "filters/flood-opacity", "filters/feDropShadow", "filters/feConvolveMatrix" };
        int count=0;
        foreach(string group in groups)
        foreach(string path in Directory.GetFiles(Path.Combine(tests,group),"*.svg"))
        {
            // Malformed convolution fixtures use SVG 1.1 error rendering and
            // four upstream UB images are not portable Filter Effects references.
            // Synthetic tests cover the current specification's pass-through rules.
            if (group == "filters/feConvolveMatrix")
            {
                string name = Path.GetFileName(path);
                if (name.StartsWith("bias=") || name == "edgeMode=wrap-with-matrix-larger-than-target.svg" ||
                    name == "divisor=0.svg" || name == "empty-kernelMatrix.svg" || name == "no-kernelMatrix.svg" ||
                    name.StartsWith("kernelMatrix-with-not-") || name.StartsWith("kernelMatrix-with-too-") ||
                    name.StartsWith("order-with-a-negative-") || name == "order=0.svg" || name == "targetX=-1.svg" || name == "targetX=3.svg") continue;
            }
            var expected=new BitmapImage(); expected.BeginInit(); expected.CacheOption=BitmapCacheOption.OnLoad;
            expected.UriSource=new Uri(Path.ChangeExtension(path,".png")); expected.EndInit();
            var reference=new FormatConvertedBitmap(expected,PixelFormats.Pbgra32,null,0);
            var renderer=new SVGRender { UseAnimations=false };
            var drawing=renderer.LoadDrawing(path);
            var actual=AnimationTests.Image(drawing,expected.PixelWidth,expected.PixelHeight,renderer.SVG.ViewBox ?? new Rect(renderer.SVG.Size));
            var pixels=new byte[actual.Length]; reference.CopyPixels(pixels,expected.PixelWidth*4,0);
            long difference=0;
            for(int i=0;i<actual.Length;i++) difference+=Math.Abs(actual[i]-pixels[i]);
            double mean=(double)difference/actual.Length;
            // This upstream fixture only promises not to crash; its very large
            // blur reference contains implementation-specific artifacts. The transformed
            // tile and out-of-range saturation fixtures are explicitly marked UB upstream.
            bool crashOnly=group=="filters/feGaussianBlur" && Path.GetFileName(path)=="huge-stdDeviation.svg" ||
                group=="filters/feTile" && Path.GetFileName(path)=="complex-transform.svg" ||
                group=="filters/feColorMatrix" && (Path.GetFileName(path)=="type=saturate-with-a-large-coefficient.svg" || Path.GetFileName(path)=="type=saturate-with-negative-coefficient.svg");
            double tolerance=group=="filters/feConvolveMatrix"?5:group=="filters/feMorphology"?3:group=="filters/feTile"?4:2;
            if(!crashOnly && mean>tolerance) throw new Exception(group+"/"+Path.GetFileName(path)+": reference image error "+mean);
            if(++count%20==0) GC.Collect();
        }
        Console.WriteLine(count+" external resvg path, point and filter reference checks passed.");
    }
}
