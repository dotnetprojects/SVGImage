using System;
using System.Linq;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using SvgControl = SVGImage.SVG.SVGImage;
using SVGImage.SVG.Utils;
using System.Windows.Media.Imaging;
using SVGImage.SVG;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        VerifyRenderingAvailable();
        GradientRgbColors();
        ButtonContentSize();
        GrayscaleDrawing();
        MaskTests.Run();
        AnimationTests.Run();
        CssTests.Run();
        GeometryTests.Run();
        FilterTests.Run();
        ResvgTests.Run();
        Console.WriteLine("All regression checks passed.");
    }

    private static void VerifyRenderingAvailable()
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen()) dc.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 2, 2));
        var bitmap = new RenderTargetBitmap(2, 2, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var pixel = new byte[16]; bitmap.CopyPixels(pixel, 8, 0);
        if (pixel[2] != 255 || pixel[3] != 255)
            throw new InvalidOperationException("WPF returned blank pixels for a plain rectangle. Restore the interactive Windows graphics session before running rendering checks.");
    }

    private static void GrayscaleDrawing()
    {
        var source = new DrawingGroup();
        var gradient = new LinearGradientBrush(Colors.Red, Colors.Blue, 0);
        source.Children.Add(new GeometryDrawing(gradient, new Pen(Brushes.Green, 2),
            new RectangleGeometry(new Rect(0, 0, 20, 10))));
        var pixels = new byte[] { 0, 0, 128, 128 };
        source.Children.Add(new ImageDrawing(BitmapSource.Create(1, 1, 96, 96,
            PixelFormats.Pbgra32, null, pixels, 4), new Rect(20, 0, 1, 1)));
        source.Freeze();
        var gray = (DrawingGroup)DrawingColorConverter.ToGrayscale(source);
        Equal(source.Bounds, gray.Bounds, "grayscale preserves geometry");
        var geometry = (GeometryDrawing)gray.Children[0];
        var stops = ((GradientBrush)geometry.Brush).GradientStops;
        Equal(Color.FromRgb(76, 76, 76), stops[0].Color, "red grayscale");
        Equal(Color.FromRgb(29, 29, 29), stops[1].Color, "blue grayscale");
        var pen = ((SolidColorBrush)geometry.Pen.Brush).Color;
        Equal(pen.R, pen.G, "grayscale pen");
        Equal(pen.G, pen.B, "grayscale pen channels");
        Equal(Colors.Red, gradient.GradientStops[0].Color, "source unchanged");
        ((BitmapSource)((ImageDrawing)gray.Children[1]).ImageSource).CopyPixels(pixels, 4, 0);
        Equal((byte)128, pixels[3], "bitmap alpha preserved");
        Equal(pixels[0], pixels[1], "bitmap grayscale");
        Equal(pixels[1], pixels[2], "bitmap grayscale channels");

        var image = new SvgControl { IsGrayscale = true, ImageSource = source, Width = 40, Height = 20 };
        image.Measure(new Size(40, 20));
        image.Arrange(new Rect(0, 0, 40, 20));
        image.UpdateLayout();
        var target = new RenderTargetBitmap(40, 20, 96, 96, PixelFormats.Pbgra32);
        target.Render(image);
        var output = new byte[40 * 20 * 4];
        target.CopyPixels(output, 40 * 4, 0);
        if (!Enumerable.Range(0, 40 * 20).Any(i => output[i * 4 + 3] > 0))
            throw new Exception("grayscale control rendered no pixels");
        for (int i = 0; i < output.Length; i += 4)
        {
            Equal(output[i], output[i + 1], "rendered grayscale");
            Equal(output[i + 1], output[i + 2], "rendered grayscale channels");
        }
        image.IsGrayscale = false;
        Equal(false, image.IsGrayscale, "grayscale can be disabled");
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
