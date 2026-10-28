using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SVGImage.SVG;

internal static class MaskTests
{
    internal static void Run()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Issue118.svg");
        var renderer = new SVGRender();
        var drawing = renderer.LoadDrawing(path);
        var image = Render(drawing, 320, 320, 20);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using (var file = File.Create(Path.Combine(AppContext.BaseDirectory, "Issue118.actual.png")))
            encoder.Save(file);
        Pixel(image, 140, 200, Colors.Transparent, "issue #118 check-mark hole");
        Pixel(image, 40, 100, Color.FromRgb(163, 204, 155), "issue #118 folder fill");
        Pixel(image, 10, 100, Color.FromRgb(98, 142, 87), "issue #118 inner border");
        CompareBrowserReference(image);
        MaskSemantics();
    }

    private static RenderTargetBitmap Svg(string content, SVGRender renderer = null)
    {
        return Render((renderer ?? new SVGRender()).LoadXmlDrawing(
            "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 100 100'>" + content + "</svg>"), 100, 100);
    }

    private static void MaskSemantics()
    {
        var image = Svg("<mask id='m'><rect width='50' height='100' fill='white'/></mask>" +
            "<rect width='100' height='100' fill='red' mask='url(#m)'/>");
        Pixel(image, 25, 50, Colors.Red, "white reveals");
        Pixel(image, 75, 50, Colors.Transparent, "outside mask hides");
        image = Svg("<mask id='m'><rect width='100' height='100' fill='white'/></mask>");
        Pixel(image, 50, 50, Colors.Transparent, "mask definitions never paint");

        image = Svg("<rect width='100' height='100' fill='red' style='mask:url(&quot;#m&quot;)'/>" +
            "<defs><mask id='m'><rect width='100' height='100' fill='white'/>" +
            "<rect width='50' height='100' fill='black'/></mask></defs>");
        Pixel(image, 25, 50, Colors.Transparent, "black erases white after compositing; forward style reference");
        Pixel(image, 75, 50, Colors.Red, "forward definition");
        image = Svg("<defs fill='white'><mask id='m'><path fill-rule='evenodd' d='M0 0H100V100H0Z'/></mask></defs>" +
            "<rect width='100' height='100' fill='red' mask='url(#m)'/>");
        Pixel(image, 50, 50, Colors.Red, "mask inherits fill from definition ancestors");

        image = Svg("<mask id='m'><rect width='100' height='100' fill='#808080' fill-opacity='0.5'/></mask>" +
            "<g mask='url(#m)'><rect width='100' height='100' fill='red'/></g>");
        Pixel(image, 50, 50, Color.FromArgb(64, 255, 0, 0), "luminance times alpha on a group", 2);
        image = Svg("<mask id='m' color-interpolation='linearRGB'><rect width='100' height='100' fill='#808080'/></mask>" +
            "<rect width='100' height='100' fill='red' mask='url(#m)'/>");
        Pixel(image, 50, 50, Color.FromArgb(55, 255, 0, 0), "linearRGB luminance", 2);
        image = Svg("<mask id='m' style='mask-type:alpha'><rect width='100' height='100' fill='black'/></mask>" +
            "<rect width='100' height='100' fill='red' mask='url(#m)'/>");
        Pixel(image, 50, 50, Colors.Red, "alpha mask ignores color");

        image = Svg("<mask id='m'><g opacity='0.5'><rect width='100' height='100' fill='white'/></g></mask>" +
            "<rect width='100' height='100' fill='red' mask='url(#m)'/>");
        Pixel(image, 50, 50, Color.FromArgb(128, 255, 0, 0), "group opacity in mask", 2);

        foreach (string units in new[] { "", "maskUnits='userSpaceOnUse' x='20' y='20' width='20' height='40'" })
        {
            image = Svg("<mask id='m' maskContentUnits='objectBoundingBox' " + units + ">" +
                "<rect width='50%' height='100%' fill='white'/></mask>" +
                "<rect x='20' y='20' width='40' height='40' fill='red' mask='url(#m)'/>");
            Pixel(image, 30, 40, Colors.Red, "bbox content with nonzero origin " + units);
            Pixel(image, 50, 40, Colors.Transparent, "bbox content boundary " + units);
        }
        image = Svg("<mask id='m' x='0%' y='0%' width='50%' height='100%'>" +
            "<rect width='100' height='100' fill='white'/></mask>" +
            "<rect x='20' y='20' width='40' height='40' fill='red' stroke='red' stroke-width='10' mask='url(#m)'/>");
        Pixel(image, 20, 40, Colors.Red, "bbox excludes stroke");
        Pixel(image, 41, 40, Colors.Transparent, "bbox region clips");
        Pixel(image, 17, 40, Colors.Transparent, "stroke clipped by object region");

        image = Svg("<mask id='m' maskUnits='userSpaceOnUse' x='20' y='20' width='20' height='40'>" +
            "<g transform='translate(20 20)'><rect width='20' height='40' fill='white'/></g></mask>" +
            "<rect width='40' height='40' fill='red' transform='translate(20 0)' mask='url(#m)'/>");
        Pixel(image, 50, 30, Colors.Red, "mask follows target transform");
        Pixel(image, 30, 30, Colors.Transparent, "mask content transform and region");

        foreach (string mask in new[] { "<mask id='m'/>", "<mask id='m' width='0'><rect width='100' height='100' fill='white'/></mask>",
            "<mask id='m'><rect width='100' height='100' fill='white' mask='url(#m)'/></mask>" })
        {
            image = Svg(mask + "<rect width='100' height='100' fill='red' mask='url(#m)'/>");
            Pixel(image, 50, 50, Colors.Transparent, "empty/zero/cyclic mask");
        }
        image = Svg("<rect width='100' height='100' fill='red' mask='none'/>");
        Pixel(image, 50, 50, Colors.Red, "mask none");
        image = Svg("<mask id='m'><rect width='100' height='100' fill='transparent'/></mask>" +
            "<rect width='100' height='100' fill='red' mask='url(#m)'/>");
        Pixel(image, 50, 50, Colors.Transparent, "transparent mask color retains alpha");
        image = Svg("<mask id='m'><rect width='100' height='100' fill='white' fill-opacity='0.5'/></mask>" +
            "<rect width='50' height='100' fill='white' mask='url(#m)'/>" +
            "<rect x='50' width='50' height='100' fill='white'/>");
        Pixel(image, 25, 50, Color.FromArgb(128, 255, 255, 255), "mask opacity is independent of shared color", 2);
        Pixel(image, 75, 50, Colors.White, "mask does not contaminate cached paint opacity");

        image = Svg("<mask id='m'><rect width='100' height='100' fill='white'/></mask>" +
            "<rect width='100' height='100' fill='red' mask='url(#m)'/>", new SVGRender { OverrideColor = Colors.Black });
        Pixel(image, 50, 50, Colors.Black, "paint overrides do not recolor mask");

        image = Svg("<defs><mask id='m'><rect width='20' height='20' fill='white'/></mask>" +
            "<g id='g' mask='url(#m)'><rect width='40' height='40' fill='red'/></g></defs>" +
            "<use href='#g' x='20' y='20'/>");
        Pixel(image, 30, 30, Colors.Red, "referenced group mask");
        Pixel(image, 50, 30, Colors.Transparent, "referenced group mask boundary");

        image = Svg("<mask id='m' fill='white'><path fill-rule='evenodd' d='M0 0H100V100H0Z M20 20H80V80H20Z'/>" +
            "<path fill='none' d='M20 20H80V80H20Z'/></mask><rect width='100' height='100' fill='red' mask='url(#m)'/>");
        Pixel(image, 50, 50, Colors.Transparent, "fill none and independent fill inheritance");
        Pixel(image, 10, 50, Colors.Red, "inherited white with fill-rule");

        image = Svg("<defs><linearGradient id='fade'><stop offset='0' stop-color='black'/>" +
            "<stop offset='1' stop-color='white'/></linearGradient>" +
            "<mask id='m'><rect width='100' height='100' fill='url(#fade)'/></mask></defs>" +
            "<rect width='100' height='100' fill='red' mask='url(#m)'/>");
        Pixel(image, 25, 50, Color.FromArgb(65, 255, 0, 0), "gradient luminance left", 3);
        Pixel(image, 75, 50, Color.FromArgb(193, 255, 0, 0), "gradient luminance right", 3);

        var bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Pbgra32, null, new byte[] { 0, 0, 255, 255 }, 4);
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        string data;
        using (var stream = new MemoryStream())
        {
            png.Save(stream);
            data = Convert.ToBase64String(stream.ToArray());
        }
        image = Svg("<mask id='m'><image xmlns:xlink='http://www.w3.org/1999/xlink' width='100' height='100' " +
            "xlink:href='data:image/png;base64," + data + "'/></mask>" +
            "<rect width='100' height='100' fill='red' mask='url(#m)'/>");
        Pixel(image, 50, 50, Color.FromArgb(54, 255, 0, 0), "embedded image luminance", 2);

        image = Svg("<mask id='a'><rect width='50' height='100' fill='white'/></mask>" +
            "<mask id='b'><rect width='100' height='100' fill='white' mask='url(#a)'/></mask>" +
            "<rect width='100' height='100' fill='red' mask='url(#b)'/>");
        Pixel(image, 25, 50, Colors.Red, "nested mask visible");
        Pixel(image, 75, 50, Colors.Transparent, "nested mask hidden");

        var root = new SVGRender().LoadXmlDrawing("<svg xmlns='http://www.w3.org/2000/svg' mask='url(#m)' " +
            "width='100' height='100'><mask id='m'><rect width='50' height='100' fill='white'/></mask>" +
            "<rect width='100' height='100' fill='red'/></svg>");
        image = Render(root, 100, 100);
        Pixel(image, 25, 50, Colors.Red, "root svg mask visible");
        Pixel(image, 75, 50, Colors.Transparent, "root svg mask hidden");

        image = Svg("<defs><rect id='r' width='40' height='40' fill='red'/></defs>" +
            "<mask id='m'><rect x='20' y='20' width='20' height='20' fill='white'/></mask>" +
            "<use href='#r' x='20' y='20' mask='url(#m)'/>");
        Pixel(image, 30, 30, Colors.Red, "mask on use");
        Pixel(image, 50, 30, Colors.Transparent, "mask on use boundary");
    }

    private static void CompareBrowserReference(BitmapSource actual)
    {
        var reference = new FormatConvertedBitmap(new BitmapImage(new Uri(Path.Combine(
            AppContext.BaseDirectory, "Fixtures", "Issue118.chromium.png"))), PixelFormats.Pbgra32, null, 0);
        const int width = 320, height = 320, stride = width * 4;
        if (reference.PixelWidth != width || reference.PixelHeight != height)
            throw new Exception("Unexpected browser reference dimensions");
        var expected = new byte[stride * height];
        var pixels = new byte[expected.Length];
        reference.CopyPixels(expected, stride, 0);
        actual.CopyPixels(pixels, stride, 0);
        int checkedPixels = 0;
        // Compare flat interiors across the entire image; browsers and WPF use
        // different edge antialiasing, so skip the one-pixel edge neighborhoods.
        for (int y = 1; y < height - 1; y++)
        for (int x = 1; x < width - 1; x++)
        {
            int offset = y * stride + x * 4;
            bool flat = true;
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            for (int c = 0; c < 4; c++)
                if (expected[offset + c] != expected[offset + dy * stride + dx * 4 + c]) flat = false;
            if (!flat) continue;
            checkedPixels++;
            for (int c = 0; c < 4; c++)
                if (Math.Abs(expected[offset + c] - pixels[offset + c]) > 2)
                    throw new Exception("Issue #118 browser comparison failed at " + x + "," + y);
        }
        if (checkedPixels < width * height * 0.8) throw new Exception("Insufficient browser reference coverage");
        Console.WriteLine("Issue #118: " + checkedPixels + " interior pixels match the Chromium reference.");
    }

    private static RenderTargetBitmap Render(Drawing drawing, int width, int height, double scale = 1)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(scale, scale));
            dc.DrawDrawing(drawing);
        }
        var image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        image.Render(visual);
        return image;
    }

    private static void Pixel(BitmapSource image, int x, int y, Color expected, string name, int tolerance = 1)
    {
        var pixel = new byte[4];
        image.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
        // Pbgra32 pixels contain premultiplied color channels.
        var channels = new[] { expected.B * expected.A / 255, expected.G * expected.A / 255,
            expected.R * expected.A / 255, (int)expected.A };
        for (int i = 0; i < 4; i++)
            if (Math.Abs(pixel[i] - channels[i]) > tolerance)
                throw new Exception(name + ": unexpected BGRA pixel " + string.Join(",", pixel));
    }
}
