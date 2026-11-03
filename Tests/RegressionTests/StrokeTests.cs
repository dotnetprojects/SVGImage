using System;
using System.Windows.Media;

internal static class StrokeTests
{
    internal static void Run()
    {
        // Exact markup from issue #123: stroke-width must not create black paint.
        var drawing=GeometryTests.Svg("<rect fill='yellow' width='60' height='20' x='30' y='0' style='stroke-width:2'/>");
        GeometryTests.Pixel(drawing,29,10,Colors.Transparent,"issue #123 has no outer stroke");
        GeometryTests.Pixel(drawing,30,10,Colors.Yellow,"issue #123 fill reaches the edge");
        foreach(string property in new[]{"stroke-width='8'","stroke-opacity='.5'","stroke-dasharray='3 2'","stroke-linecap='round'","stroke-linejoin='bevel'"})
        {
            drawing=GeometryTests.Svg("<rect x='20' y='20' width='40' height='40' fill='yellow' "+property+"/>");
            GeometryTests.Pixel(drawing,20,40,Colors.Yellow,"stroke properties retain initial none: "+property);
        }
        drawing=GeometryTests.Svg("<style>rect {stroke-width:8}</style><rect x='20' y='20' width='40' height='40' fill='yellow'/>");
        GeometryTests.Pixel(drawing,18,40,Colors.Transparent,"stylesheet stroke width retains initial none");
        drawing=GeometryTests.Svg("<g stroke='red' stroke-width='8'><rect x='20' y='20' width='40' height='40' fill='yellow' stroke-width='4'/></g>");
        GeometryTests.Pixel(drawing,19,40,Colors.Red,"local width preserves inherited stroke paint");
        GeometryTests.Pixel(drawing,17,40,Colors.Transparent,"local width overrides inherited width");
        drawing=GeometryTests.Svg("<g stroke='red' stroke-width='8'><rect x='20' y='20' width='40' height='40' fill='yellow' stroke='blue'/></g>");
        GeometryTests.Pixel(drawing,17,40,Colors.Blue,"local paint preserves inherited stroke width");
        drawing=GeometryTests.Svg("<g stroke='red' stroke-width='8' stroke-opacity='.5'><rect x='20' y='20' width='40' height='40' fill='yellow' stroke='blue'/></g>");
        GeometryTests.Pixel(drawing,17,40,Color.FromArgb(128,0,0,255),"local paint preserves inherited opacity",2);
        drawing=GeometryTests.Svg("<g stroke='red' stroke-width='8'><rect x='20' y='20' width='40' height='40' fill='yellow' stroke='none' stroke-linecap='round'/></g>");
        GeometryTests.Pixel(drawing,18,40,Colors.Transparent,"explicit none blocks inherited paint");
        drawing=GeometryTests.Svg("<g stroke='none'><rect x='20' y='20' width='40' height='40' fill='yellow' stroke='inherit' stroke-width='8'/></g>");
        GeometryTests.Pixel(drawing,18,40,Colors.Transparent,"explicit inherit of none remains empty");
        drawing=GeometryTests.Svg("<defs><rect id='r' x='20' y='20' width='40' height='40' fill='yellow' stroke-width='4'/></defs><use href='#r' stroke='red'/>");
        GeometryTests.Pixel(drawing,19,40,Colors.Red,"local width inherits paint from use instance");
        drawing=GeometryTests.Svg("<circle cx='40' cy='40' r='20' fill='yellow' stroke-width='8'/>");
        GeometryTests.Pixel(drawing,17,40,Colors.Transparent,"circle width does not introduce paint");
        drawing=GeometryTests.Svg("<defs><rect id='r' x='20' y='20' width='20' height='20' fill='yellow' stroke-width='4'/></defs><use href='#r' stroke='red'/><use href='#r' x='40' stroke='blue'/>");
        GeometryTests.Pixel(drawing,19,30,Colors.Red,"first use keeps its own stroke paint");
        GeometryTests.Pixel(drawing,59,30,Colors.Blue,"second use resolves its own stroke paint");
        var renderer=new SVGImage.SVG.SVGRender();
        renderer.LoadXmlDrawing("<svg xmlns='http://www.w3.org/2000/svg'><g id='parent' stroke='red' stroke-width='6' stroke-opacity='.5' stroke-dasharray='3 2' stroke-linecap='round' stroke-linejoin='bevel'><rect id='child' width='40' height='40' stroke-width='4'/></g></svg>");
        var parent=renderer.SVG.GetShape("parent").Stroke;
        var child=renderer.SVG.GetShape("child").Stroke;
        if(child.PaintServerKey!=parent.PaintServerKey || child.Opacity!=50 || child.StrokeArray.Length!=2 || child.LineCap!=SVGImage.SVG.Stroke.eLineCap.round || child.LineJoin!=SVGImage.SVG.Stroke.eLineJoin.bevel)
            throw new Exception("Stroke properties must inherit independently");
        child.StrokeArray=null;
        if(child.StrokeArray!=null || parent.StrokeArray==null) throw new Exception("Explicit dash reset must not mutate its parent");
        Console.WriteLine("Issue #123 default stroke and independent stroke inheritance checks passed.");
    }
}
