using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

internal static class FilterTests
{
    internal static void Run()
    {
        var d=GeometryTests.Svg("<filter id='f' x='0' y='0' width='1' height='1' color-interpolation-filters='sRGB'><feTurbulence type='fractalNoise'/></filter><rect width='100' height='100' filter='url(#f)'/>");
        GeometryTests.Pixel(d,50,50,Color.FromArgb(128,128,128,128),"zero-frequency fractal noise in sRGB",2);
        d=GeometryTests.Svg("<filter id='f' x='0' y='0' width='1' height='1'><feTurbulence type='fractalNoise'/></filter><rect width='100' height='100' filter='url(#f)'/>");
        GeometryTests.Pixel(d,50,50,Color.FromArgb(128,188,188,188),"default linearRGB noise",2);
        d=GeometryTests.Svg("<filter id='f'><feTurbulence baseFrequency='.05' numOctaves='0'/></filter><rect width='100' height='100' filter='url(#f)'/>");
        GeometryTests.Pixel(d,50,50,Colors.Transparent,"zero turbulence octaves");
        string input="<filter id='f' filterUnits='userSpaceOnUse' x='0' y='0' width='100' height='100' color-interpolation-filters='sRGB'><feFlood flood-color='red' flood-opacity='.5' result='map'/><feDisplacementMap in='SourceGraphic' in2='map' scale='20' xChannelSelector='R' yChannelSelector='G'/></filter><rect x='20' y='20' width='40' height='40' fill='blue' filter='url(#f)'/>";
        d=GeometryTests.Svg(input);
        GeometryTests.Pixel(d,15,35,Colors.Blue,"displacement channel is unpremultiplied");
        GeometryTests.Pixel(d,35,65,Colors.Blue,"negative displacement channel");
        GeometryTests.Pixel(d,55,35,Colors.Transparent,"displacement exterior");
        GeometryTests.Pixel(d,35,25,Colors.Transparent,"displacement vertical boundary");
        d=GeometryTests.Svg(input.Replace("scale='20'","scale='0'"));
        GeometryTests.Pixel(d,25,25,Colors.Blue,"zero-scale displacement is identity");
        string noise="<filter id='f' x='0' y='0' width='1' height='1'><feTurbulence baseFrequency='.08 .04' seed='17' numOctaves='3' type='fractalNoise'/></filter><rect width='100' height='100' filter='url(#f)'/>";
        var first=Image(noise); var same=Image(noise); var different=Image(noise.Replace("seed='17'","seed='18'"));
        if(!first.SequenceEqual(same) || first.SequenceEqual(different)) throw new Exception("Seeded turbulence must be deterministic and seed-dependent");
        if(first.Where((v,i)=>i%4==3).Distinct().Count()<20) throw new Exception("Turbulence must vary spatially");
        ReferenceNoise();
        Console.WriteLine("Turbulence, displacement, filter input and color-space checks passed.");
    }
    private static void ReferenceNoise()
    {
        string root=Environment.GetEnvironmentVariable("RESVG_TEST_SUITE");
        if(string.IsNullOrEmpty(root)) return;
        string folder=System.IO.Path.Combine(root,"tests","filters","feTurbulence");
        foreach(string name in new[] { "baseFrequency=0.01", "baseFrequency=0.05-0.01", "baseFrequency=0.05-0.05", "type=fractalNoise", "numOctaves=5", "seed=20", "seed=-20", "color-interpolation-filters=sRGB" })
        {
            string path=System.IO.Path.Combine(folder,name);
            var expected=new System.Windows.Media.Imaging.BitmapImage(new Uri(path+".png"));
            var reference=new System.Windows.Media.Imaging.FormatConvertedBitmap(expected,PixelFormats.Pbgra32,null,0);
            var renderer=new SVGImage.SVG.SVGRender { UseAnimations=false };
            var drawing=renderer.LoadDrawing(path+".svg");
            var actual=AnimationTests.Image(drawing,expected.PixelWidth,expected.PixelHeight,renderer.SVG.ViewBox ?? new Rect(renderer.SVG.Size));
            var pixels=new byte[actual.Length]; reference.CopyPixels(pixels,expected.PixelWidth*4,0);
            double error=actual.Select((value,index)=>Math.Abs(value-pixels[index])).Average();
            if(error>2) throw new Exception(name+": reference image error "+error);
        }
    }
    private static byte[] Image(string content) => AnimationTests.Image(GeometryTests.Svg(content),100,100,new Rect(0,0,100,100));
}
