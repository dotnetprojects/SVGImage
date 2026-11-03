using System;
using System.Reflection;
using System.Windows.Media;
using SVGImage.SVG;
using SVGImage.SVG.Utils;

internal static class ColdStartTests
{
    internal static void Run()
    {
        // Run before other SVG tests: geometry-only artwork must not enumerate
        // system fonts just because shapes inherit text-style properties.
        var resolver=(Lazy<FontResolver>)typeof(TextStyle).GetField("_fontResolver",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        if(resolver.IsValueCreated) throw new Exception("Font catalog initialized before use");
        var renderer=new SVGRender();
        renderer.LoadXmlDrawing("<svg xmlns='http://www.w3.org/2000/svg'><g font-family='Arial' font-size='16'><path d='M0 0L40 0L20 40Z'/></g></svg>");
        if(resolver.IsValueCreated) throw new Exception("Geometry-only SVG initialized the font catalog");
        var text=renderer.LoadXmlDrawing("<svg xmlns='http://www.w3.org/2000/svg'><text x='10' y='30' font-family='Arial' font-size='20'>Hello</text></svg>");
        if(!resolver.IsValueCreated || text.Bounds.IsEmpty || text.Bounds.Width<=0) throw new Exception("Actual text must resolve its typeface");
        Console.WriteLine("Geometry-only font deferral and first text typeface checks passed.");
    }
}
