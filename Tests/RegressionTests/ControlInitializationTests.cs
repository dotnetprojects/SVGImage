using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using SvgControl = SVGImage.SVG.SVGImage;

internal static class ControlInitializationTests
{
    internal static void Run()
    {
        if (Application.Current == null) new Application();
        var image = new SvgControl { UseAnimations = false };
        image.BeginInit();
        image.Source = "/RegressionTests;component/Embedded/acid1.svg";
        image.EndInit();
        var eye = image.SVG.GetShape("right-eye");
        var brush = eye.Fill.FillBrush(image.SVG, new SVGImage.SVG.SVGRender(), eye, 1, new Rect(0, 0, 100, 100)) as SolidColorBrush;
        if (brush == null || brush.Color != Colors.White)
            throw new Exception("The embedded acid1.svg must load its companion acid1.css resource.");

        var drawing = new SVGImage.SVG.SVGRender().CreateDrawing(image.SVG);
        var acidPixels = AnimationTests.Image(drawing, 600, 400, new Rect(-600, -400, 1200, 800));
        int pupil = (174 * 600 + 351) * 4;
        if (acidPixels[pupil] != 0 || acidPixels[pupil + 1] != 128 || acidPixels[pupil + 2] != 0 || acidPixels[pupil + 3] != 255)
            throw new Exception("The actual embedded acid1.svg must render its right pupil green using modern mixed-unit RGB.");

        // Initial paint overrides and later edits must still work when publishing
        // the initial palette no longer causes a second complete rendering.
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><rect width='100' height='100' fill='red'/></svg>"));
        var control = new SvgControl { OverrideFillColor = Colors.Blue, Width = 100, Height = 100 };
        control.BeginInit(); control.SetImage(stream); control.EndInit();
        AssertPixel(control, Colors.Blue);
        control.OverrideFillColor = Colors.Green;
        AssertPixel(control, Colors.Green);
        stream.Dispose();
        Console.WriteLine("Embedded companion resources and initial/later brush override checks passed.");
    }
    private static void AssertPixel(SvgControl control, Color expected)
    {
        control.Measure(new Size(100, 100)); control.Arrange(new Rect(0, 0, 100, 100)); control.UpdateLayout();
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(100, 100, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(control);
        var pixels = new byte[40000]; bitmap.CopyPixels(pixels, 400, 0);
        int at = (50 * 100 + 50) * 4;
        if (pixels[at] != expected.B || pixels[at + 1] != expected.G || pixels[at + 2] != expected.R || pixels[at + 3] != 255)
            throw new Exception("Control brush override did not render " + expected);
    }
}
