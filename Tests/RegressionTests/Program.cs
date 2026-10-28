using System;
using System.Linq;
using System.Windows.Media;
using SVGImage.SVG;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        GradientRgbColors();
        Console.WriteLine("All regression checks passed.");
    }

    private static void GradientRgbColors()
    {
        foreach (string gradient in new[] { "linearGradient", "radialGradient" })
        {
            var render = new SVGRender();
            var drawing = render.LoadXmlDrawing(
                "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'>" +
                "<defs><" + gradient + " id='grad1'>" +
                "<stop offset='0%' style='stop-color:rgb(255,255,0);stop-opacity:1'/>" +
                "<stop offset='25%' stop-color='rgb(100%, 0%, 0%)'/>" +
                "<stop offset='50%' stop-color='blue'/>" +
                "<stop offset='100%' stop-color='#00ff00' stop-opacity='0.5'/>" +
                "</" + gradient + "></defs>" +
                "<rect width='100' height='100' fill='url(#grad1)'/></svg>");
            var brush = (GradientBrush)GeometryDrawings(drawing).Single().Brush;
            Equal(Colors.Yellow, brush.GradientStops[0].Color, gradient + " rgb stop");
            Equal(Colors.Red, brush.GradientStops[1].Color, gradient + " percentage stop");
            Equal(Colors.Blue, brush.GradientStops[2].Color, gradient + " named stop");
            Equal(Color.FromArgb(127, 0, 255, 0), brush.GradientStops[3].Color, gradient + " opacity");
        }
    }

    private static System.Collections.Generic.IEnumerable<GeometryDrawing> GeometryDrawings(Drawing drawing)
    {
        if (drawing is GeometryDrawing geometry) yield return geometry;
        if (drawing is DrawingGroup group)
            foreach (var child in group.Children)
                foreach (var item in GeometryDrawings(child)) yield return item;
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!object.Equals(expected, actual))
            throw new Exception(name + ": expected " + expected + ", got " + actual);
    }
}
