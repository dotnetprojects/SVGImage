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
        var flood=GeometryTests.Svg("<filter id='f'><feFlood flood-color='rgba(255,0,0,.5)'/></filter><rect x='20' y='20' width='40' height='40' filter='url(#f)'/>");
        GeometryTests.Pixel(flood,25,25,Color.FromArgb(128,255,0,0),"standalone flood and CSS alpha",2);
        d=GeometryTests.Svg("<filter id='f'><feOffset dx='15' dy='10'/></filter><rect x='20' y='20' width='40' height='40' fill='blue' filter='url(#f)'/>");
        GeometryTests.Pixel(d,40,40,Colors.Blue,"standalone offset"); GeometryTests.Pixel(d,25,25,Colors.Transparent,"offset removes original");
        d=GeometryTests.Svg("<filter id='f' color-interpolation-filters='sRGB'><feComponentTransfer><feFuncR type='linear' slope='0' intercept='1'/><feFuncA type='linear' slope='.5'/></feComponentTransfer></filter><rect width='100' height='100' fill='blue' filter='url(#f)'/>");
        GeometryTests.Pixel(d,40,40,Color.FromArgb(128,255,0,255),"unpremultiplied component transfer",2);
        d=GeometryTests.Svg("<filter id='f' color-interpolation-filters='sRGB'><feFlood flood-color='yellow' result='yellow'/><feBlend in='SourceGraphic' in2='yellow' mode='multiply'/></filter><rect width='100' height='100' fill='cyan' filter='url(#f)'/>");
        GeometryTests.Pixel(d,40,40,Colors.Lime,"multiply blend");
        d=GeometryTests.Svg("<filter id='f'><feMorphology radius='5' operator='dilate'/></filter><rect x='20' y='20' width='40' height='40' fill='red' filter='url(#f)'/>");
        GeometryTests.Pixel(d,17,30,Colors.Red,"morphology dilation");
        d=GeometryTests.Svg("<filter id='f'><feMorphology radius='5' operator='erode'/></filter><rect x='20' y='20' width='40' height='40' fill='red' filter='url(#f)'/>");
        GeometryTests.Pixel(d,22,30,Colors.Transparent,"morphology erosion exterior"); GeometryTests.Pixel(d,30,30,Colors.Red,"morphology erosion interior");
        d=GeometryTests.Svg("<filter id='f' filterUnits='userSpaceOnUse' x='0' y='0' width='100' height='100'><feFlood x='0' y='0' width='10' height='10' flood-color='red'/><feTile/></filter><rect width='100' height='100' filter='url(#f)'/>");
        GeometryTests.Pixel(d,75,75,Colors.Red,"tile repeats primitive subregion");
        d=GeometryTests.Svg("<filter id='f'><feGaussianBlur stdDeviation='2'/></filter><rect x='20' y='20' width='40' height='40' fill='red' filter='url(#f)'/>");
        var blurred=AnimationTests.Image(d,100,100,new Rect(0,0,100,100));
        int exterior=blurred[(30*100+18)*4+3],edge=blurred[(30*100+20)*4+3],inside=blurred[(30*100+30)*4+3];
        if(exterior<=0 || exterior>=edge || edge>=inside || inside<250) throw new Exception("Gaussian blur spreads coverage and preserves the interior");
        d=GeometryTests.Svg("<filter id='f' filterUnits='userSpaceOnUse' x='0' y='0' width='100' height='100'><feFlood flood-color='red'/></filter><g filter='url(#f)'/>");
        GeometryTests.Pixel(d,50,50,Colors.Red,"user-space flood on empty group");
        d=GeometryTests.Svg("<filter id='f'><feFlood flood-color='red'/></filter><rect x='20' y='20' width='40' height='40' opacity='.5' filter='url(#f)'/>");
        GeometryTests.Pixel(d,30,30,Color.FromArgb(128,255,0,0),"element opacity follows filtering",2);
        d=GeometryTests.Svg("<filter id='f' color-interpolation-filters='sRGB'><feComponentTransfer><feFuncA type='table' tableValues='1 0 0 1'/></feComponentTransfer></filter><rect width='100' height='100' opacity='.5' fill='blue' filter='url(#f)'/>");
        GeometryTests.Pixel(d,30,30,Color.FromArgb(128,0,0,255),"transfer receives source before element opacity",2);
        d=GeometryTests.Svg("<filter id='f' filterUnits='userSpaceOnUse' x='0' y='0' width='100' height='100'><feFlood x='0' y='0' width='20' height='20' flood-color='red'/><feOffset dx='10' dy='10'/><feTile/></filter><rect width='100' height='100' filter='url(#f)'/>");
        GeometryTests.Pixel(d,15,15,Colors.Red,"tile retains offset input subregion"); GeometryTests.Pixel(d,5,5,Colors.Transparent,"tile repeats transparent subregion pixels");
        d=GeometryTests.Svg("<filter id='f' color-interpolation-filters='sRGB'><feConvolveMatrix order='1' kernelMatrix='1'/></filter><rect x='20' y='20' width='40' height='40' fill='red' fill-opacity='.5' filter='url(#f)'/>");
        GeometryTests.Pixel(d,30,30,Color.FromArgb(128,255,0,0),"identity convolution preserves alpha",2);
        d=GeometryTests.Svg("<filter id='f' color-interpolation-filters='sRGB'><feConvolveMatrix order='1' kernelMatrix='2' divisor='1' preserveAlpha='true'/></filter><rect width='100' height='100' fill='#4080c0' fill-opacity='.5' filter='url(#f)'/>");
        GeometryTests.Pixel(d,30,30,Color.FromArgb(128,128,255,255),"convolution unpremultiplies when preserving alpha",3);
        const string convolution="<filter id='f' filterUnits='userSpaceOnUse' x='0' y='0' width='100' height='100'><feConvolveMatrix order='3 1' kernelMatrix='1 0 0' kernelUnitLength='5' edgeMode='none'/></filter><rect x='40' y='40' width='10' height='10' fill='red' filter='url(#f)'/>";
        d=GeometryTests.Svg(convolution);
        GeometryTests.Pixel(d,37,45,Colors.Red,"convolution rotates its kernel"); GeometryTests.Pixel(d,48,45,Colors.Transparent,"convolution orientation boundary");
        string wrapping=convolution.Replace("kernelUnitLength='5'","kernelUnitLength='50'").Replace("edgeMode='none'","edgeMode='wrap'").Replace("x='40' y='40' width='10'","x='0' y='40' width='10'");
        d=GeometryTests.Svg(wrapping);
        GeometryTests.Pixel(d,55,45,Colors.Red,"convolution wraps across the image edge");
        d=GeometryTests.Svg(wrapping.Replace("edgeMode='wrap'","edgeMode='none'"));
        GeometryTests.Pixel(d,55,45,Colors.Transparent,"none convolution edges stay transparent");
        d=GeometryTests.Svg("<filter id='f' filterUnits='userSpaceOnUse' x='0' y='0' width='100' height='100'><feDropShadow dx='20' dy='0' stdDeviation='0' flood-color='blue' flood-opacity='.5'/></filter><rect x='20' y='20' width='20' height='20' fill='red' filter='url(#f)'/>");
        GeometryTests.Pixel(d,30,30,Colors.Red,"drop shadow preserves source on top"); GeometryTests.Pixel(d,50,30,Color.FromArgb(128,0,0,255),"drop shadow offset color and opacity",2);
        string convolve = "<filter id='f' color-interpolation-filters='sRGB'><feConvolveMatrix order='1' kernelMatrix='2' divisor='0'/></filter><rect width='100' height='100' fill='red' filter='url(#f)'/>";
        GeometryTests.Pixel(GeometryTests.Svg(convolve),50,50,Colors.Red,"zero divisor uses the kernel sum");
        foreach (string invalid in new[] { "order='0' kernelMatrix='1'", "order='3' kernelMatrix='1 2'", "order='3'" })
            GeometryTests.Pixel(GeometryTests.Svg(convolve.Replace("order='1' kernelMatrix='2' divisor='0'",invalid)),50,50,Colors.Red,"invalid convolution passes its input through");
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
