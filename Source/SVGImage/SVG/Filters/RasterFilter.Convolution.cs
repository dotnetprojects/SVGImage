using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Windows.Media;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG.Filters
{
    internal sealed partial class RasterFilter
    {
        private sealed class KernelTap
        {
            internal double X, Y, Weight;
        }

        // https://www.w3.org/TR/filter-effects-1/#feConvolveMatrixElement
        private void Convolve(XmlNode node, Frame input, Frame output)
        {
            var order = SvgNumberReader.List(Attr(node, "order", "3"));
            var kernel = SvgNumberReader.List(Attr(node, "kernelMatrix"));
            if (order.Length < 1 || order.Length > 2 || order.Any(v => v < 1 || v > int.MaxValue))
            { Array.Copy(input.Data, output.Data, input.Data.Length); return; }
            int columns = (int)order[0], rows = (int)(order.Length == 2 ? order[1] : order[0]);
            if ((long)columns * rows != kernel.Length)
            { Array.Copy(input.Data, output.Data, input.Data.Length); return; }
            int targetX = (int)Number(node, "targetX", columns / 2), targetY = (int)Number(node, "targetY", rows / 2);
            if (targetX < 0 || targetX >= columns) targetX = columns / 2;
            if (targetY < 0 || targetY >= rows) targetY = rows / 2;
            double sum = kernel.Sum(), divisor = Number(node, "divisor", sum == 0 ? 1 : sum);
            if (divisor == 0) divisor = sum == 0 ? 1 : sum;
            double bias = Number(node, "bias");
            var units = SvgNumberReader.List(Attr(node, "kernelUnitLength"));
            double stepX = units.Length > 0 && units[0] > 0 ? units[0] * unitX * sx : 1;
            double stepY = units.Length > 1 && units[1] > 0 ? units[1] * unitY * sy : units.Length == 1 ? units[0] * unitY * sy : 1;
            if (stepY <= 0) stepY = 1;
            bool preserveAlpha = Attr(node, "preserveAlpha") == "true";
            var working = input;
            if (preserveAlpha)
            {
                working = new Frame(input.Data.Length, input.Linear);
                for (int i = 0; i < input.Data.Length; i += 4)
                {
                    double a = input.Data[i + 3]; working.Data[i + 3] = (float)a;
                    for (int c = 0; c < 3; c++) working.Data[i + c] = a > 0 ? (float)(input.Data[i + c] / a) : 0;
                }
            }
            var taps = new List<KernelTap>();
            for (int row = 0; row < rows; row++) for (int column = 0; column < columns; column++)
            {
                double weight = kernel[(rows - row - 1) * columns + columns - column - 1] / divisor;
                if (weight != 0) taps.Add(new KernelTap { X = (column - targetX) * stepX, Y = (row - targetY) * stepY, Weight = weight });
            }
            string edge = Attr(node, "edgeMode", "duplicate");
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                double r = 0, g = 0, b = 0, alpha = 0;
                foreach (var tap in taps)
                {
                    double px = x + tap.X, py = y + tap.Y;
                    r += SampleEdge(working, px, py, 0, edge) * tap.Weight;
                    g += SampleEdge(working, px, py, 1, edge) * tap.Weight;
                    b += SampleEdge(working, px, py, 2, edge) * tap.Weight;
                    if (!preserveAlpha) alpha += SampleEdge(working, px, py, 3, edge) * tap.Weight;
                }
                int at = (y * width + x) * 4;
                alpha = preserveAlpha ? input.Data[at + 3] : Clamp(alpha + bias);
                output.Data[at + 3] = (float)alpha;
                output.Data[at] = (float)(preserveAlpha ? Clamp(r + bias) * alpha : Math.Min(alpha, Clamp(r + bias * alpha)));
                output.Data[at + 1] = (float)(preserveAlpha ? Clamp(g + bias) * alpha : Math.Min(alpha, Clamp(g + bias * alpha)));
                output.Data[at + 2] = (float)(preserveAlpha ? Clamp(b + bias) * alpha : Math.Min(alpha, Clamp(b + bias * alpha)));
            }
        }

        private float SampleEdge(Frame input, double x, double y, int channel, string edge)
        {
            int ix = (int)Math.Floor(x), iy = (int)Math.Floor(y);
            double dx = x - ix, dy = y - iy;
            if (dx == 0 && dy == 0) return EdgePixel(input, ix, iy, channel, edge);
            return (float)((1 - dy) * ((1 - dx) * EdgePixel(input, ix, iy, channel, edge) + dx * EdgePixel(input, ix + 1, iy, channel, edge)) +
                dy * ((1 - dx) * EdgePixel(input, ix, iy + 1, channel, edge) + dx * EdgePixel(input, ix + 1, iy + 1, channel, edge)));
        }

        private float EdgePixel(Frame input, int x, int y, int channel, string edge)
        {
            if (edge == "wrap") { x = (x % width + width) % width; y = (y % height + height) % height; }
            else if (edge != "none") { x = Math.Max(0, Math.Min(width - 1, x)); y = Math.Max(0, Math.Min(height - 1, y)); }
            return Pixel(input, x, y, channel);
        }

        private void DropShadow(XmlNode node, Frame input, Frame output)
        {
            var alpha = new Frame(input.Data.Length, input.Linear);
            for (int i = 3; i < input.Data.Length; i += 4) alpha.Data[i] = input.Data[i];
            var deviation = SvgNumberReader.List(Attr(node, "stdDeviation", "2"));
            double dx = deviation.Length > 0 ? deviation[0] : 2, dy = deviation.Length > 1 ? deviation[1] : dx;
            if (dx < 0 || dy < 0 || deviation.Length > 2) dx = dy = 2;
            alpha = BlurAxes(alpha, dx * unitX * sx, dy * unitY * sy, "none");
            string color = Style(node, "flood-color", "black", false);
            if (color == "currentColor") color = Style(node, "color", "black", true);
            Color flood; if (!CssColor.TryParse(color, out flood)) flood = Colors.Black;
            double opacity = Clamp(XmlUtil.GetDoubleValue(Style(node, "flood-opacity", "1", false), 1)) * flood.A / 255.0;
            double red = flood.R / 255.0, green = flood.G / 255.0, blue = flood.B / 255.0;
            if (output.Linear) { red = ConvertColor(red, true); green = ConvertColor(green, true); blue = ConvertColor(blue, true); }
            double ox = Number(node, "dx", 2) * unitX * sx, oy = Number(node, "dy", 2) * unitY * sy;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                int at = (y * width + x) * 4;
                double a = opacity * Sample(alpha, x - ox, y - oy, 3);
                output.Data[at] = (float)(red * a); output.Data[at + 1] = (float)(green * a);
                output.Data[at + 2] = (float)(blue * a); output.Data[at + 3] = (float)a;
            }
            Over(output, input);
        }
    }
}
