using System;
using System.Windows;
using System.Windows.Media;
using SVGImage.SVG;

internal static class GeometryTests
{
    internal static void Run()
    {
        var d = Svg("<rect width='100' height='100' fill='red' clip-path='url(#c)'/><defs><clipPath id='c'><path d='M10 10H50V50H10Z'/></clipPath></defs>");
        Pixel(d,20,20,Colors.Red,"path clipping with forward reference"); Pixel(d,60,20,Colors.Transparent,"path clipping exterior");
        d=Svg("<clipPath id='c'><rect x='20' y='30' width='20' height='20'/></clipPath><rect width='100' height='100' fill='red' clip-path='url(#c)'/>");
        Pixel(d,25,35,Colors.Red,"clip rectangle position"); Pixel(d,5,5,Colors.Transparent,"clip definitions never paint");
        d=Svg("<clipPath id='c' clipPathUnits='objectBoundingBox'><rect width='50%' height='100%'/></clipPath><rect x='20' y='20' width='40' height='40' fill='red' clip-path='url(#c)'/>");
        Pixel(d,30,30,Colors.Red,"bbox clip inside"); Pixel(d,50,30,Colors.Transparent,"bbox clip outside");
        d=Svg("<clipPath id='c'/><rect width='100' height='100' fill='red' clip-path='url(#c)'/>");
        Pixel(d,20,20,Colors.Transparent,"empty clip");
        d=Svg("<clipPath id='c' transform='translate(20 20)'><path clip-rule='evenodd' d='M0 0H40V40H0Z M10 10H30V30H10Z'/></clipPath><rect width='100' height='100' fill='red' clip-path='url(#c)'/>");
        Pixel(d,25,25,Colors.Red,"transformed clip"); Pixel(d,40,40,Colors.Transparent,"clip-rule evenodd");
        d=Svg("<defs><path id='p' d='M0 0H20V20H0Z'/><clipPath id='c'><use href='#p' x='20' y='20'/></clipPath></defs><rect width='100' height='100' fill='red' clip-path='url(#c)'/>");
        Pixel(d,25,25,Colors.Red,"use in clip path"); Pixel(d,5,5,Colors.Transparent,"use clip position");
        d=Svg("<defs><g id='a'><use href='#b'/></g><g id='b'><use href='#a'/></g></defs><use href='#a'/><rect width='10' height='10' fill='red'/>");
        Pixel(d,5,5,Colors.Red,"recursive use is ignored without losing other shapes");

        d=Svg("<svg x='20' y='30' width='40' height='20' viewBox='0 0 20 10'><rect width='50%' height='100%' fill='red'/></svg>");
        Pixel(d,25,35,Colors.Red,"nested viewBox and percentage lengths"); Pixel(d,45,35,Colors.Transparent,"nested viewport percentage boundary");
        d=Svg("<svg x='20' y='20' width='20' height='20'><rect width='100' height='100' fill='red'/></svg>");
        Pixel(d,25,25,Colors.Red,"nested viewport without viewBox"); Pixel(d,50,25,Colors.Transparent,"nested overflow clipping");
        d=Svg("<svg x='20' y='20' width='20' height='20' overflow='visible'><rect width='50' height='50' fill='red'/></svg>");
        Pixel(d,50,25,Colors.Red,"nested overflow visible");
        d=Svg("<svg x='20' y='20' width='40' height='20' viewBox='0 0 10 10'><rect width='10' height='10' fill='red'/></svg>");
        Pixel(d,25,25,Colors.Transparent,"nested meet alignment margin"); Pixel(d,35,25,Colors.Red,"nested meet alignment content");
        d=Svg("<svg x='20' y='20' width='40' height='20' viewBox='0 0 10 10' preserveAspectRatio='none'><rect width='10' height='10' fill='red'/></svg>");
        Pixel(d,25,25,Colors.Red,"nested preserveAspectRatio none");
        d=Svg("<defs><symbol id='s' viewBox='0 0 10 10'><rect width='50%' height='100%' fill='red'/></symbol></defs><use href='#s' x='20' y='20' width='40' height='40'/>");
        Pixel(d,25,25,Colors.Red,"symbol percentage viewport"); Pixel(d,50,25,Colors.Transparent,"symbol percentage width");
        d=Svg("<svg x='20' y='20' width='40' height='40' viewBox='0 0 10 10'><defs><mask id='m' maskUnits='userSpaceOnUse' x='0' y='0' width='50%' height='100%'><rect width='10' height='10' fill='white'/></mask></defs><rect width='10' height='10' fill='red' mask='url(#m)'/></svg>");
        Pixel(d,25,25,Colors.Red,"nested mask percentage viewport"); Pixel(d,50,25,Colors.Transparent,"nested mask percentage region");
        var plain=Svg("<text x='10' y='40' font-family='Arial' font-size='20'>Test</text>").Bounds;
        var along=Svg("<defs><path id='p' d='M10 40H90'/></defs><text font-family='Arial' font-size='20'><textPath href='#p'>Test</textPath></text>").Bounds;
        Near(plain,along,"horizontal textPath matches regular text");
        var shifted=Svg("<defs><path id='p' d='M10 40H90'/></defs><text font-family='Arial' font-size='20'><textPath href='#p' startOffset='25%'>Test</textPath></text>").Bounds;
        var expected=along; expected.Offset(20,0); Near(expected,shifted,"percentage startOffset");
        var vertical=Svg("<defs><path id='p' d='M40 10V90'/></defs><text font-family='Arial' font-size='20'><textPath href='#p'>Test</textPath></text>").Bounds;
        if(Math.Abs(vertical.Height-along.Width)>.1 || Math.Abs(vertical.Width-along.Height)>.1) throw new Exception("textPath follows path tangent");
        d=Svg("<text><textPath href='#missing'>invisible</textPath></text><rect width='10' height='10' fill='red'/>");
        Pixel(d,5,5,Colors.Red,"missing textPath reference");
        d=Svg("<pattern id='p' patternUnits='userSpaceOnUse' width='20' height='20'><rect width='10' height='20' fill='grey'/></pattern><rect width='100' height='100' fill='url(#p)'/>");
        Pixel(d,5,5,Colors.Gray,"user-space pattern and CSS grey color"); Pixel(d,15,5,Colors.Transparent,"pattern tile gap"); Pixel(d,25,5,Colors.Gray,"pattern repeats");
        d=Svg("<defs><pattern id='p' width='.5' height='1' patternContentUnits='objectBoundingBox'><rect width='.25' height='1' fill='red'/></pattern></defs><rect x='20' y='20' width='40' height='40' fill='url(#p)'/><rect x='70' width='20' height='20' fill='url(#p)'/>");
        Pixel(d,25,25,Colors.Red,"bbox pattern content"); Pixel(d,35,25,Colors.Transparent,"bbox pattern gap"); Pixel(d,45,25,Colors.Red,"bbox pattern repeats");
        Pixel(d,72,5,Colors.Red,"pattern reused at different bounds"); Pixel(d,77,5,Colors.Transparent,"pattern scales per target");
        d=Svg("<defs><pattern id='p' href='#base' patternUnits='userSpaceOnUse' width='20' height='20' viewBox='0 0 10 10'/><pattern id='base'><rect width='5' height='10' fill='red'/></pattern></defs><rect width='100' height='100' fill='url(#p)'/>");
        Pixel(d,5,5,Colors.Red,"pattern forward template and viewBox"); Pixel(d,15,5,Colors.Transparent,"pattern viewBox scales content");
        d=Svg("<path d='M10 10h30v30h-30z' fill='red'/><path d='M60 10L90 10L90#40' fill='none' stroke='blue' stroke-width='4'/>");
        Pixel(d,20,20,Colors.Red,"relative path commands"); Pixel(d,75,10,Colors.Blue,"invalid path retains complete segments");
        d=Svg("<polygon/><polyline/><polygon points='10 10 40 10 40 40 broken 10 40' fill='red'/><rect x='60' y='60' width='10' height='10' fill='blue'/>");
        Pixel(d,30,15,Colors.Red,"polygon retains valid point prefix"); Pixel(d,65,65,Colors.Blue,"empty geometry does not interrupt siblings");
        d=Svg("<path d='M20 20a20 20 0 0110 30' fill='none' stroke='red' stroke-width='3'/>");
        if(d.Bounds.IsEmpty || d.Bounds.Height<29) throw new Exception("adjacent arc flags are supported");
        d=Svg("<path d='L20 20'/><path d='M20 20L' fill='none'/><rect width='10' height='10' fill='red'/>");
        Pixel(d,5,5,Colors.Red,"malformed path does not abort document");
        d=Svg("<linearGradient id='g'><stop offset='0' stop-color='red'/><stop offset='1' stop-color='red'/></linearGradient><rect width='20' height='20' fill='url(#g)' fill-opacity='.5'/><rect x='30' width='20' height='20' fill='url(#g)'/>");
        Pixel(d,5,5,Color.FromArgb(128,255,0,0),"gradient fill opacity",2); Pixel(d,35,5,Colors.Red,"cached gradient preserves per-use opacity");
        d=Svg("<pattern id='p' patternUnits='userSpaceOnUse' width='10' height='10'><rect width='10' height='10' fill='red'/></pattern><rect width='20' height='20' fill='url(#p)' fill-opacity='.5'/>");
        Pixel(d,5,5,Color.FromArgb(128,255,0,0),"pattern fill opacity",2);
        d=Svg("<radialGradient id='r'><stop offset='0' stop-color='red'/><stop offset='1' stop-color='white'/></radialGradient><rect width='100' height='100' fill='url(#r)'/>");
        Pixel(d,50,50,Colors.Red,"default radial focal point is centered",5);
        Console.WriteLine("Clip paths, nested viewports, text paths and recursive-use checks passed.");
    }
    private static void Near(Rect expected,Rect actual,string name)
    {
        if(expected.IsEmpty || actual.IsEmpty || Math.Abs(expected.X-actual.X)>.1 || Math.Abs(expected.Y-actual.Y)>.1 || Math.Abs(expected.Width-actual.Width)>.1 || Math.Abs(expected.Height-actual.Height)>.1)
            throw new Exception(name+": expected "+expected+", got "+actual);
    }
    internal static DrawingGroup Svg(string content) => new SVGRender().LoadXmlDrawing("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 100 100'>"+content+"</svg>");
    internal static void Pixel(Drawing drawing,int x,int y,Color expected,string name,int tolerance=1)
    {
        var pixels=AnimationTests.Image(drawing,100,100,new Rect(0,0,100,100)); int offset=(y*100+x)*4;
        int[] channels={expected.B*expected.A/255,expected.G*expected.A/255,expected.R*expected.A/255,expected.A};
        for(int c=0;c<4;c++) if(Math.Abs(pixels[offset+c]-channels[c])>tolerance) throw new Exception(name+": wrong pixel at "+x+","+y);
    }
}
