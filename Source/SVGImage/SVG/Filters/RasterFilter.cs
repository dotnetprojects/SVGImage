using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SVGImage.SVG.Animation;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG.Filters
{
    internal sealed partial class RasterFilter
    {
        private readonly SVG svg;
        private readonly XmlNode filter;
        private readonly Rect bounds, region, filterRegion, viewport;
        private readonly int width, height;
        private readonly double sx, sy, unitX, unitY;
        private sealed class Frame
        {
            internal readonly float[] Data;
            internal bool Linear;
            internal Rect Area = Rect.Empty;
            internal Frame(int size, bool linear) { Data = new float[size]; Linear = linear; }
        }
        internal RasterFilter(SVG svg, XmlNode filter, Rect bounds, Rect viewport)
        {
            this.svg = svg; this.filter = filter; this.bounds = bounds; this.viewport = viewport;
            filterRegion = Region(filter, bounds, true);
            if (filterRegion.IsEmpty) return;
            region = filterRegion;
            double scale = Math.Min(4, 2048 / Math.Max(region.Width, region.Height));
            width = Math.Max(1, (int)Math.Ceiling(region.Width * scale)); height = Math.Max(1, (int)Math.Ceiling(region.Height * scale));
            sx = width / region.Width; sy = height / region.Height;
            bool bbox = Attr(filter, "primitiveUnits", "") == "objectBoundingBox";
            unitX = bbox ? bounds.Width : 1; unitY = bbox ? bounds.Height : 1;
        }
        private static string Attr(XmlNode node, string name, string fallback = "") => XmlUtil.AttrValue(node, name, fallback);
        private static double Number(XmlNode node, string name, double fallback = 0)
        {
            double value;
            return double.TryParse(Attr(node, name), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value) && !double.IsNaN(value) && !double.IsInfinity(value) ? value : fallback;
        }
        private Rect Region(XmlNode node, Rect fallback, bool outer)
        {
            bool bbox = Attr(filter, outer ? "filterUnits" : "primitiveUnits", outer ? "objectBoundingBox" : "userSpaceOnUse") == "objectBoundingBox";
            double refX = bbox ? 1 : viewport.Width, refY = bbox ? 1 : viewport.Height;
            double x = node.Attributes["x"] == null ? fallback.X : XmlUtil.GetDoubleValue(Attr(node, "x"), refX) * (bbox ? bounds.Width : 1) + (bbox ? bounds.X : 0);
            double y = node.Attributes["y"] == null ? fallback.Y : XmlUtil.GetDoubleValue(Attr(node, "y"), refY) * (bbox ? bounds.Height : 1) + (bbox ? bounds.Y : 0);
            double w = node.Attributes["width"] == null ? fallback.Width : XmlUtil.GetDoubleValue(Attr(node, "width"), refX) * (bbox ? bounds.Width : 1);
            double h = node.Attributes["height"] == null ? fallback.Height : XmlUtil.GetDoubleValue(Attr(node, "height"), refY) * (bbox ? bounds.Height : 1);
            if (outer)
            {
                if (node.Attributes["x"] == null) x = bbox ? bounds.X - .1 * bounds.Width : -.1 * viewport.Width;
                if (node.Attributes["y"] == null) y = bbox ? bounds.Y - .1 * bounds.Height : -.1 * viewport.Height;
                if (node.Attributes["width"] == null) w = 1.2 * (bbox ? bounds.Width : viewport.Width);
                if (node.Attributes["height"] == null) h = 1.2 * (bbox ? bounds.Height : viewport.Height);
            }
            return w <= 0 || h <= 0 || double.IsInfinity(x + y + w + h) || double.IsNaN(x + y + w + h) ? Rect.Empty : new Rect(x, y, w, h);
        }
        internal Drawing Render(Drawing source)
        {
            if (filterRegion.IsEmpty || region.IsEmpty || unitX <= 0 || unitY <= 0) return new DrawingGroup();
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.PushTransform(new MatrixTransform(sx, 0, 0, sy, -region.X * sx, -region.Y * sy)); dc.DrawDrawing(source);
            }
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
            var bytes = new byte[width * height * 4]; bitmap.CopyPixels(bytes, width * 4, 0);
            var graphic = new Frame(bytes.Length, false) { Area = filterRegion }; var alpha = new Frame(bytes.Length, false) { Area = filterRegion };
            for (int i = 0; i < bytes.Length; i += 4)
            {
                graphic.Data[i] = bytes[i + 2] / 255f; graphic.Data[i + 1] = bytes[i + 1] / 255f; graphic.Data[i + 2] = bytes[i] / 255f;
                graphic.Data[i + 3] = alpha.Data[i + 3] = bytes[i + 3] / 255f;
            }
            var images = new Dictionary<string, Frame> { { "SourceGraphic", graphic }, { "SourceAlpha", alpha } };
            Frame previous = graphic;
            foreach (XmlNode node in filter.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element || !node.LocalName.StartsWith("fe")) continue;
                var styles = svg.StyleSheet.Resolve(node);
                string space = Style(node, "color-interpolation-filters", "linearRGB", true);
                bool linear = space != "sRGB";
                Frame input = Input(images, Attr(node, "in"), previous, linear), second = Input(images, Attr(node, "in2"), previous, linear);
                var output = new Frame(bytes.Length, linear);
                switch (node.LocalName)
                {
                    case "feTurbulence": Noise(node, output); break;
                    case "feDisplacementMap":
                        double scale = Number(node, "scale"); int cx = Channel(Attr(node, "xChannelSelector", "A")), cy = Channel(Attr(node, "yChannelSelector", "A"));
                        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                        {
                            int at = (y * width + x) * 4; double a = second.Data[at + 3];
                            double dx = scale * unitX * sx * ((a > 0 ? second.Data[at + cx] / a : 0) - .5), dy = scale * unitY * sy * ((a > 0 ? second.Data[at + cy] / a : 0) - .5);
                            for (int c = 0; c < 4; c++) output.Data[at + c] = Sample(input, x + dx, y + dy, c);
                        }
                        break;
                    case "feFlood":
                        string color, opacity; styles.TryGetValue("flood-color", out color); styles.TryGetValue("flood-opacity", out opacity);
                        Color flood; if (color == "currentColor") color = Style(node, "color", "black", true);
                        if (!CssColor.TryParse(color ?? "black", out flood)) flood = Colors.Black;
                        double fa = Clamp(flood.A / 255.0 * (opacity == null ? 1 : XmlUtil.GetDoubleValue(opacity, 1)));
                        for (int i = 0; i < output.Data.Length; i += 4)
                        {
                            output.Data[i] = (float)(fa * (linear ? ConvertColor(flood.R / 255.0, true) : flood.R / 255.0)); output.Data[i + 1] = (float)(fa * (linear ? ConvertColor(flood.G / 255.0, true) : flood.G / 255.0));
                            output.Data[i + 2] = (float)(fa * (linear ? ConvertColor(flood.B / 255.0, true) : flood.B / 255.0)); output.Data[i + 3] = (float)fa;
                        }
                        break;
                    case "feOffset":
                        double ox = Number(node, "dx") * unitX * sx, oy = Number(node, "dy") * unitY * sy;
                        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) for (int c = 0; c < 4; c++) output.Data[(y * width + x) * 4 + c] = Sample(input, x - ox, y - oy, c);
                        break;
                    case "feMerge":
                        foreach (XmlNode child in node.ChildNodes) if (child.LocalName == "feMergeNode") Over(output, Input(images, Attr(child, "in"), previous, linear));
                        break;
                    case "feComposite": Composite(node, input, second, output); break;
                    case "feColorMatrix": ColorMatrix(node, input, output); break;
                    case "feComponentTransfer": ComponentTransfer(node, input, output); break;
                    case "feBlend": Blend(node, input, second, output); break;
                    case "feGaussianBlur": output = Blur(node, input); break;
                    case "feMorphology": output = Morphology(node, input); break;
                    case "feTile": Tile(input, output); break;
                    case "feConvolveMatrix": Convolve(node, input, output); break;
                    case "feDropShadow": DropShadow(node, input, output); break;
                    default: Array.Copy(input.Data, output.Data, input.Data.Length); break;
                }
                Rect defaultArea = input.Area;
                if (node.LocalName == "feFlood" || node.LocalName == "feTurbulence" || node.LocalName == "feTile") defaultArea = filterRegion;
                else if (node.LocalName == "feBlend" || node.LocalName == "feComposite" || node.LocalName == "feDisplacementMap") defaultArea.Union(second.Area);
                else if (node.LocalName == "feMerge")
                    foreach (XmlNode child in node.ChildNodes) if (child.LocalName == "feMergeNode") defaultArea.Union(Input(images, Attr(child, "in"), previous, linear).Area);
                Rect clip = Region(node, defaultArea, false);
                clip.Intersect(region);
                output.Area = clip;
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                    if (!clip.Contains(region.X + (x + .5) / sx, region.Y + (y + .5) / sy)) Array.Clear(output.Data, (y * width + x) * 4, 4);
                previous = output;
                string result = Attr(node, "result"); if (result.Length > 0) images[result] = output;
            }
            previous = ConvertFrame(previous, false);
            for (int i = 0; i < bytes.Length; i += 4)
            {
                bytes[i] = Byte(previous.Data[i + 2]); bytes[i + 1] = Byte(previous.Data[i + 1]); bytes[i + 2] = Byte(previous.Data[i]); bytes[i + 3] = Byte(previous.Data[i + 3]);
            }
            var image = BitmapSource.Create(width, height, 96, 96, PixelFormats.Pbgra32, null, bytes, width * 4); image.Freeze();
            var resultDrawing = new DrawingGroup { ClipGeometry = new RectangleGeometry(filterRegion) };
            resultDrawing.Children.Add(new ImageDrawing(image, region));
            return resultDrawing;
        }
        private string Style(XmlNode node, string property, string fallback, bool inherited)
        {
            for (; node != null && node.NodeType == XmlNodeType.Element; node = inherited ? node.ParentNode : null)
            {
                string value; if (svg.StyleSheet.Resolve(node).TryGetValue(property, out value)) return value;
            }
            return fallback;
        }
        private Frame Input(Dictionary<string, Frame> images, string name, Frame previous, bool linear)
        {
            Frame frame;
            return ConvertFrame(name.Length == 0 ? previous : images.TryGetValue(name, out frame) ? frame : new Frame(previous.Data.Length, linear), linear);
        }
        private static double Clamp(double v) => Math.Max(0, Math.Min(1, v));
        private static byte Byte(double v) => (byte)Math.Round(255 * Clamp(v));
        private static double ConvertColor(double v, bool linear) => linear ? (v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4)) : (v <= .0031308 ? v * 12.92 : 1.055 * Math.Pow(v, 1 / 2.4) - .055);
        private static Frame ConvertFrame(Frame input, bool linear)
        {
            if (input.Linear == linear) return input;
            var result = new Frame(input.Data.Length, linear) { Area = input.Area };
            for (int i = 0; i < input.Data.Length; i += 4)
            {
                double a = input.Data[i + 3]; result.Data[i + 3] = (float)a;
                for (int c = 0; c < 3; c++) result.Data[i + c] = (float)(a > 0 ? a * ConvertColor(input.Data[i + c] / a, linear) : 0);
            }
            return result;
        }
        private static int Channel(string value) => value == "R" ? 0 : value == "G" ? 1 : value == "B" ? 2 : 3;
        private float Pixel(Frame frame, int x, int y, int c) => x < 0 || y < 0 || x >= width || y >= height ? 0 : frame.Data[(y * width + x) * 4 + c];
        private float Sample(Frame frame, double x, double y, int c)
        {
            int ix = (int)Math.Floor(x), iy = (int)Math.Floor(y); double dx = x - ix, dy = y - iy;
            return (float)((1 - dy) * ((1 - dx) * Pixel(frame, ix, iy, c) + dx * Pixel(frame, ix + 1, iy, c)) + dy * ((1 - dx) * Pixel(frame, ix, iy + 1, c) + dx * Pixel(frame, ix + 1, iy + 1, c)));
        }
        private void Noise(XmlNode node, Frame output)
        {
            var frequencies = global::SVGImage.SVG.Utils.SvgNumberReader.List(Attr(node, "baseFrequency", "0"));
            double fx = frequencies.Length > 0 ? frequencies[0] : 0, fy = frequencies.Length > 1 ? frequencies[1] : fx;
            if (fx < 0 || fy < 0) return;
            int octaves = (int)Math.Max(0, Math.Min(9, Number(node, "numOctaves", 1)));
            var noise = new TurbulenceNoise(Number(node, "seed"));
            bool fractal = Attr(node, "type") == "fractalNoise", stitch = Attr(node, "stitchTiles") == "stitch";
            Rect tile = Region(node, filterRegion, false);
            if (tile.IsEmpty) return;
            tile = new Rect(tile.X / unitX, tile.Y / unitY, tile.Width / unitX, tile.Height / unitY);
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                int at = (y * width + x) * 4; double px = (region.X + (x + .5) / sx) / unitX, py = (region.Y + (y + .5) / sy) / unitY;
                double a = noise.Sample(3, px, py, fx, fy, octaves, fractal, stitch, tile); output.Data[at + 3] = (float)a;
                for (int c = 0; c < 3; c++) output.Data[at + c] = (float)(a * noise.Sample(c, px, py, fx, fy, octaves, fractal, stitch, tile));
            }
        }
        private static void Over(Frame destination, Frame source)
        {
            for (int i = 0; i < source.Data.Length; i += 4) for (int c = 0; c < 4; c++) destination.Data[i + c] = source.Data[i + c] + destination.Data[i + c] * (1 - source.Data[i + 3]);
        }
        private static void Composite(XmlNode node, Frame first, Frame second, Frame output)
        {
            string op = Attr(node, "operator", "over");
            double k1 = Number(node, "k1"), k2 = Number(node, "k2"), k3 = Number(node, "k3"), k4 = Number(node, "k4");
            for (int i = 0; i < output.Data.Length; i += 4)
            {
                double a = first.Data[i + 3], b = second.Data[i + 3];
                for (int c = 0; c < 4; c++)
                {
                    double p = first.Data[i + c], q = second.Data[i + c];
                    output.Data[i + c] = (float)Clamp(op == "in" ? p * b : op == "out" ? p * (1 - b) : op == "atop" ? p * b + q * (1 - a) : op == "xor" ? p * (1 - b) + q * (1 - a) : op == "arithmetic" ? k1 * p * q + k2 * p + k3 * q + k4 : p + q * (1 - a));
                }
                for (int c = 0; c < 3; c++) output.Data[i + c] = Math.Min(output.Data[i + c], output.Data[i + 3]);
            }
        }
        private static void ColorMatrix(XmlNode node, Frame input, Frame output)
        {
            double[] m = global::SVGImage.SVG.Utils.SvgNumberReader.List(Attr(node, "values"));
            string type = Attr(node, "type", "matrix");
            if (type == "luminanceToAlpha") m = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, .2125, .7154, .0721, 0, 0 };
            else if (type == "saturate")
            {
                double s = m.Length == 1 && m[0] >= 0 && m[0] <= 1 ? m[0] : 1;
                m = new[] { .213 + .787 * s, .715 - .715 * s, .072 - .072 * s, 0, 0, .213 - .213 * s, .715 + .285 * s, .072 - .072 * s, 0, 0, .213 - .213 * s, .715 - .715 * s, .072 + .928 * s, 0, 0, 0, 0, 0, 1, 0 };
            }
            else if (type == "hueRotate")
            {
                double angle = m.Length == 1 ? m[0] * Math.PI / 180 : 0, cos = Math.Cos(angle), sin = Math.Sin(angle);
                m = new[] { .213+.787*cos-.213*sin,.715-.715*cos-.715*sin,.072-.072*cos+.928*sin,0,0,
                    .213-.213*cos+.143*sin,.715+.285*cos+.140*sin,.072-.072*cos-.283*sin,0,0,
                    .213-.213*cos-.787*sin,.715-.715*cos+.715*sin,.072+.928*cos+.072*sin,0,0,0,0,0,1,0 };
            }
            if (m.Length != 20) { Array.Copy(input.Data, output.Data, input.Data.Length); return; }
            for (int i = 0; i < input.Data.Length; i += 4)
            {
                double a = input.Data[i + 3]; var values = new[] { a > 0 ? input.Data[i] / a : 0, a > 0 ? input.Data[i + 1] / a : 0, a > 0 ? input.Data[i + 2] / a : 0, a, 1 };
                var result = new double[4];
                for (int r = 0; r < 4; r++) { for (int c = 0; c < 5; c++) result[r] += m[r * 5 + c] * values[c]; result[r] = Clamp(result[r]); }
                for (int c = 0; c < 3; c++) output.Data[i + c] = (float)(result[c] * result[3]); output.Data[i + 3] = (float)result[3];
            }
        }
    }
}
