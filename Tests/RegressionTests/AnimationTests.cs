using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SVGImage.SVG;

internal static class AnimationTests
{
    internal static void Run()
    {
        var renderer = new SVGRender { UseAnimations = true };
        var symbolDrawing = renderer.LoadXmlDrawing(Wrap("<defs><symbol id='face' viewBox='-10 -10 20 20'>" +
            "<circle r='5' fill='red'/></symbol></defs><use href='#face' x='40' y='40' width='40' height='40'/>"));
        Equal(255, Pixel(symbolDrawing, 60, 60)[2], "symbol viewBox maps to use viewport");
        Equal(0, Pixel(symbolDrawing, 40, 40)[3], "symbol origin does not leak into use viewport");
        var drawing = renderer.LoadXmlDrawing(Wrap("<g><animateTransform type='translate' values='10 0;30 0' dur='2s' additive='sum'/>" +
            "<animateTransform type='scale' values='1;3' dur='2s' additive='sum'/><rect width='5' height='5' fill='red'/></g>"));
        renderer.SeekAnimations(TimeSpan.FromSeconds(1));
        Near(20, drawing.Bounds.X, "additive translate/scale order");
        Near(10, drawing.Bounds.Width, "scale interpolation");

        drawing = renderer.LoadXmlDrawing(Wrap("<g><animate attributeName='display' values='none;inline;none' keyTimes='0;0.25;1' dur='1s' repeatCount='indefinite'/>" +
            "<rect width='20' height='20' fill='red'/></g>"));
        renderer.SeekAnimations(TimeSpan.FromSeconds(.1));
        Equal(0, Pixel(drawing, 5, 5)[3], "SMIL hidden frame");
        renderer.SeekAnimations(TimeSpan.FromSeconds(.5));
        Equal(255, Pixel(drawing, 5, 5)[3], "SMIL visible frame");
        renderer.SeekAnimations(TimeSpan.FromSeconds(1));
        Equal(0, Pixel(drawing, 5, 5)[3], "SMIL loop boundary");

        foreach (string fill in new[] { "remove", "freeze" })
        {
            drawing = renderer.LoadXmlDrawing(Wrap("<g><animateTransform type='translate' from='0' to='20' dur='1s' fill='" + fill + "'/>" +
                "<rect width='5' height='5'/></g>"));
            renderer.SeekAnimations(TimeSpan.FromSeconds(1.5));
            Near(fill == "freeze" ? 20 : 0, drawing.Bounds.X, "finite repeat and fill " + fill);
        }

        drawing = renderer.LoadXmlDrawing(Wrap("<circle id='c' cx='20' cy='20' r='5'/>" +
            "<animate href='#c' attributeName='r' values='5;15' dur='2s'/>"));
        renderer.SeekAnimations(TimeSpan.FromSeconds(1));
        Near(20, drawing.Bounds.Width, "referenced radius animation");

        drawing = renderer.LoadXmlDrawing("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20'>" +
            "<style>@keyframes frames {100% {transform:translateY(-80px)}} #sheet {animation:frames 1s steps(4) infinite}</style>" +
            "<g id='sheet'><rect width='20' height='20' fill='red'/><rect y='20' width='20' height='20' fill='blue'/>" +
            "<rect y='40' width='20' height='20' fill='green'/><rect y='60' width='20' height='20' fill='yellow'/></g></svg>");
        var expected = new[] { Colors.Red, Colors.Blue, Colors.Green, Colors.Yellow, Colors.Red };
        var times = new[] { .1, .3, .55, .8, 1.0 };
        for (int i = 0; i < times.Length; i++)
        {
            renderer.SeekAnimations(TimeSpan.FromSeconds(times[i]));
            var p = Pixel(drawing, 5, 5);
            Equal(expected[i].R, p[2], "CSS stepped frame red");
            Equal(expected[i].B, p[0], "CSS stepped frame blue");
            Equal(expected[i].G, p[1], "CSS stepped frame green");
        }

        drawing=renderer.LoadXmlDrawing(Wrap("<rect width='10' height='5'><animateMotion path='M10 20L50 20L50 60' dur='2s' rotate='auto' fill='freeze'/></rect>"));
        renderer.SeekAnimations(TimeSpan.FromSeconds(.5));
        Near(30,drawing.Bounds.X,"motion follows path length"); Near(20,drawing.Bounds.Y,"motion path y");
        renderer.SeekAnimations(TimeSpan.FromSeconds(1.5));
        Near(45,drawing.Bounds.X,"motion automatic rotation"); Near(40,drawing.Bounds.Y,"motion corner y");
        drawing=renderer.LoadXmlDrawing(Wrap("<defs><path id='motion' d='M0 0Q50 100 100 0'/></defs><rect width='2' height='2'><animateMotion dur='2s'><mpath href='#motion'/></animateMotion></rect>"));
        renderer.SeekAnimations(TimeSpan.FromSeconds(1));
        Near(50,drawing.Bounds.X,"referenced curved motion midpoint",.1); Near(50,drawing.Bounds.Y,"referenced curved motion tangent",.1);
        drawing=renderer.LoadXmlDrawing(Wrap("<rect width='2' height='2'><animateMotion path='M0 0H100' dur='2s' keyPoints='0;.2;1' keyTimes='0;.5;1'/></rect>"));
        renderer.SeekAnimations(TimeSpan.FromSeconds(1));
        Near(20,drawing.Bounds.X,"motion keyPoints");

        drawing = renderer.LoadXmlDrawing(Wrap("<g><animateTransform type='translate' values='0;80' keyTimes='0;1' dur='1s' " +
            "calcMode='spline' keySplines='.42 0 1 1'/><rect width='5' height='5'/></g>"));
        renderer.SeekAnimations(TimeSpan.FromSeconds(.5));
        Near(25.23, drawing.Bounds.X, "SMIL spline interpolation", .1);

        drawing = renderer.LoadXmlDrawing(Wrap("<g><animateTransform type='translate' from='0' to='20' dur='1s' begin='play.click'/>" +
            "<rect width='5' height='5'/></g>"));
        renderer.SeekAnimations(TimeSpan.FromSeconds(2));
        Near(0, drawing.Bounds.X, "event animation waits");
        renderer.TriggerAnimationEvent("play");
        renderer.SeekAnimations(TimeSpan.FromSeconds(2.5));
        Near(10, drawing.Bounds.X, "event animation starts at event time");

        drawing = renderer.LoadXmlDrawing("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'>" +
            "<animate attributeName='viewBox' values='0 0 100 100;50 0 50 50' dur='2s'/>" +
            "<rect x='50' width='10' height='10' fill='red'/></svg>");
        renderer.SeekAnimations(TimeSpan.FromSeconds(1));
        Near(100.0 / 3, drawing.Bounds.X, "animated viewBox camera pan");
        Near(40.0 / 3, drawing.Bounds.Width, "animated viewBox camera zoom");

        drawing = renderer.LoadXmlDrawing(Wrap("<rect width='20' height='20' fill='red' filter='url(#white)'/>" +
            "<defs><filter id='white'><feColorMatrix type='matrix' values='0 0 0 0 1 0 0 0 0 1 0 0 0 0 1 0 0 0 1 0'/></filter></defs>"));
        var filtered = Pixel(drawing, 5, 5);
        Equal(255, filtered[0], "forward color-matrix filter blue");
        Equal(255, filtered[1], "forward color-matrix filter green");
        Equal(255, filtered[2], "forward color-matrix filter red");

        drawing = renderer.LoadXmlDrawing(Wrap("<g display='none'><animate attributeName='display' to='inline' values='inline;inline' begin='play.click' dur='1s'/>" +
            "<rect width='10' height='10' fill='red'/></g>"));
        renderer.SeekAnimations(TimeSpan.Zero);
        Equal(0, Pixel(drawing, 5, 5)[3], "static display none before click");
        renderer.TriggerAnimationEvent("play");
        renderer.SeekAnimations(TimeSpan.FromSeconds(.2));
        Equal(255, Pixel(drawing, 5, 5)[3], "click can reveal statically hidden content");

        drawing = renderer.LoadXmlDrawing(Wrap("<rect width='20' height='20' fill='red' opacity='0.5'>" +
            "<animate attributeName='opacity' from='0' to='1' dur='1s'/></rect>"));
        renderer.SeekAnimations(TimeSpan.FromSeconds(.5));
        Near(128, Pixel(drawing, 5, 5)[3], "animated opacity replaces base opacity", 1);

        drawing = renderer.LoadXmlDrawing(Wrap("<g><animateTransform type='translate' from='0' to='80' dur='1s'/>" +
            "<rect width='5' height='5'/></g>"));
        double before = drawing.Bounds.X;
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        timer.Tick += (s, e) => { timer.Stop(); frame.Continue = false; };
        timer.Start();
        Dispatcher.PushFrame(frame);
        if (drawing.Bounds.X <= before + 1) throw new Exception("Native WPF animation clock did not advance");

        renderer.UseAnimations = false;
        drawing = renderer.LoadXmlDrawing(Wrap("<g><animateTransform type='translate' values='0;80' dur='1s'/>" +
            "<rect width='5' height='5'/></g>"));
        renderer.SeekAnimations(TimeSpan.FromSeconds(.5));
        Near(0, drawing.Bounds.X, "UseAnimations=false");
        Console.WriteLine("SMIL, CSS steps and live WPF clock checks passed.");
        DownloadedExamples();
    }

    private static void DownloadedExamples()
    {
        string root = Environment.GetEnvironmentVariable("SVG_ANIMATION_EXAMPLES");
        if (string.IsNullOrEmpty(root)) return;
        foreach (string file in Directory.GetFiles(Path.Combine(root, "Flash2Svg.wiki", "img"), "*.svg").Concat(Directory.GetFiles(root, "horse.svg")))
        {
            var renderer = new SVGRender { UseAnimations = true };
            var drawing = renderer.LoadDrawing(file);
            renderer.SeekAnimations(TimeSpan.Zero);
            var first = Image(drawing, 150, 150, renderer.SVG.ViewBox ?? new Rect(renderer.SVG.Size));
            string name = Path.GetFileNameWithoutExtension(file);
            if (name.StartsWith("Viber"))
            {
                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(file), @"begin=""([^"";]+)\.click"))
                    renderer.TriggerAnimationEvent(match.Groups[1].Value);
            }
            double sampleTime = name == "LoveDota" ? 25 : name == "EyeOpening" ? 2.5 : name == "PeteyHead" ? 60 : name == "MattHead" ? 10 : name.StartsWith("Viber") ? .5 : .35;
            renderer.SeekAnimations(TimeSpan.FromSeconds(sampleTime));
            var second = Image(drawing, 150, 150, renderer.SVG.ViewBox ?? new Rect(renderer.SVG.Size));
            int changed = first.Zip(second, (a,b) => a != b ? 1 : 0).Sum();
            Console.WriteLine(Path.GetFileName(file) + ": loaded; changed bytes=" + changed + "; animation warnings=" + renderer.AnimationWarnings.Count);
            if (changed == 0) throw new Exception(name + " produced no changed frame at " + sampleTime);
            foreach (string warning in renderer.AnimationWarnings.Distinct().Take(3)) Console.WriteLine("  " + warning);
        }
    }

    internal static byte[] Image(Drawing drawing, int width, int height, Rect viewport)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new MatrixTransform(width / viewport.Width, 0, 0, height / viewport.Height,
                -viewport.X * width / viewport.Width, -viewport.Y * height / viewport.Height));
            dc.DrawDrawing(drawing);
        }
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var pixels = new byte[width * height * 4];
        bitmap.CopyPixels(pixels, width * 4, 0);
        return pixels;
    }
    private static byte[] Pixel(Drawing drawing, int x, int y)
    {
        return Image(drawing, 100, 100, new Rect(0, 0, 100, 100)).Skip((y * 100 + x) * 4).Take(4).ToArray();
    }
    private static string Wrap(string content) => "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'>" + content + "</svg>";
    private static void Near(double expected, double actual, string name, double tolerance = .01)
    {
        if (Math.Abs(expected - actual) > tolerance) throw new Exception(name + ": expected " + expected + ", got " + actual);
    }
    private static void Equal(int expected, int actual, string name) => Near(expected, actual, name, 0);
}
