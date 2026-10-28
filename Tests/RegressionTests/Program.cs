using System;
using System.Linq;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using SvgControl = SVGImage.SVG.SVGImage;
using SVGImage.SVG;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        GradientRgbColors();
        ButtonContentSize();
        Console.WriteLine("All regression checks passed.");
    }

    private static void ButtonContentSize()
    {
        foreach (SvgControl.eSizeType mode in Enum.GetValues(typeof(SvgControl.eSizeType)))
        {
            var image = new SvgControl { SizeType = mode };
            image.SetImage(new GeometryDrawing(Brushes.Red, null, new RectangleGeometry(new Rect(0, 0, 32, 16))));
            var button = new Button { Content = image };
            button.Measure(new Size(200, 100));
            button.Arrange(new Rect(0, 0, 200, 100));
            button.UpdateLayout();
            if (image.DesiredSize.Width <= 0 || image.DesiredSize.Height <= 0 || image.ActualWidth <= 0 || image.ActualHeight <= 0)
                throw new Exception(mode + ": SVG inside Button received no space");
        }

        var changing = new SvgControl();
        changing.SetImage(new GeometryDrawing(Brushes.Red, null, new RectangleGeometry(new Rect(0, 0, 32, 16))));
        changing.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Equal(new Size(32, 16), changing.DesiredSize, "unconstrained intrinsic size");
        changing.SetImage(new GeometryDrawing(Brushes.Red, null, new RectangleGeometry(new Rect(0, 0, 64, 48))));
        changing.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Equal(new Size(64, 48), changing.DesiredSize, "replacement drawing invalidates measure");
        changing.SetImage((Drawing)null);
        changing.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Equal(new Size(0, 0), changing.DesiredSize, "clearing drawing invalidates measure");
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
