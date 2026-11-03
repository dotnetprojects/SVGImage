using System;
using System.Linq;
using System.Xml;
using SVGImage.SVG.Animation;

namespace SVGImage.SVG.Filters
{
    internal sealed partial class RasterFilter
    {
        private static void ComponentTransfer(XmlNode node, Frame input, Frame output)
        {
            var functions = new Func<double, double>[] { v => v, v => v, v => v, v => v };
            foreach (XmlNode child in node.ChildNodes)
            {
                int channel = child.LocalName == "feFuncR" ? 0 : child.LocalName == "feFuncG" ? 1 : child.LocalName == "feFuncB" ? 2 : child.LocalName == "feFuncA" ? 3 : -1;
                if (channel < 0) continue;
                string type = Attr(child, "type", "identity");
                double[] table = global::SVGImage.SVG.Utils.SvgNumberReader.List(Attr(child, "tableValues"));
                double slope = Number(child, "slope", 1), intercept = Number(child, "intercept"), amplitude = Number(child, "amplitude", 1), exponent = Number(child, "exponent", 1), offset = Number(child, "offset");
                functions[channel] = v =>
                {
                    if (type == "linear") return Clamp(slope * v + intercept);
                    if (type == "gamma") return Clamp(amplitude * Math.Pow(v, exponent) + offset);
                    if (type == "discrete" && table.Length > 0) return Clamp(table[Math.Min(table.Length - 1, (int)(v * table.Length))]);
                    if (type == "table" && table.Length > 0)
                    {
                        double position = v * (table.Length - 1); int index = Math.Min(table.Length - 1, (int)position);
                        return Clamp(table[index] + (position - index) * (table[Math.Min(index + 1, table.Length - 1)] - table[index]));
                    }
                    return v;
                };
            }
            for (int i = 0; i < input.Data.Length; i += 4)
            {
                double alpha = input.Data[i + 3], resultAlpha = functions[3](alpha);
                for (int c = 0; c < 3; c++) output.Data[i + c] = (float)(resultAlpha * functions[c](alpha > 0 ? input.Data[i + c] / alpha : 0));
                output.Data[i + 3] = (float)resultAlpha;
            }
        }
        private static void Blend(XmlNode node, Frame source, Frame backdrop, Frame output)
        {
            string mode = Attr(node, "mode", "normal");
            for (int i = 0; i < output.Data.Length; i += 4)
            {
                double sa = source.Data[i + 3], ba = backdrop.Data[i + 3];
                var s = new Rgb(sa > 0 ? source.Data[i] / sa : 0, sa > 0 ? source.Data[i+1] / sa : 0, sa > 0 ? source.Data[i+2] / sa : 0);
                var b = new Rgb(ba > 0 ? backdrop.Data[i] / ba : 0, ba > 0 ? backdrop.Data[i+1] / ba : 0, ba > 0 ? backdrop.Data[i+2] / ba : 0);
                var nonseparable = s;
                bool useNonseparable = mode == "hue" || mode == "saturation" || mode == "color" || mode == "luminosity";
                if (mode == "hue") nonseparable = SetLum(SetSat(s, Sat(b)), Lum(b));
                else if (mode == "saturation") nonseparable = SetLum(SetSat(b, Sat(s)), Lum(b));
                else if (mode == "color") nonseparable = SetLum(s, Lum(b));
                else if (mode == "luminosity") nonseparable = SetLum(b, Lum(s));
                for (int c = 0; c < 3; c++)
                {
                    double sc = s[c], bc = b[c], value = sc;
                    switch (mode)
                    {
                        case "multiply": value = bc * sc; break;
                        case "screen": value = bc + sc - bc * sc; break;
                        case "darken": value = Math.Min(bc, sc); break;
                        case "lighten": value = Math.Max(bc, sc); break;
                        case "overlay": value = bc <= .5 ? 2 * bc * sc : 1 - 2 * (1 - bc) * (1 - sc); break;
                        case "hard-light": value = sc <= .5 ? 2 * bc * sc : 1 - 2 * (1 - bc) * (1 - sc); break;
                        case "soft-light": value = sc <= .5 ? bc - (1 - 2 * sc) * bc * (1 - bc) : bc + (2 * sc - 1) * ((bc <= .25 ? ((16 * bc - 12) * bc + 4) * bc : Math.Sqrt(bc)) - bc); break;
                        case "color-dodge": value = bc == 0 ? 0 : sc == 1 ? 1 : Math.Min(1, bc / (1 - sc)); break;
                        case "color-burn": value = bc == 1 ? 1 : sc == 0 ? 0 : 1 - Math.Min(1, (1 - bc) / sc); break;
                        case "difference": value = Math.Abs(bc - sc); break;
                        case "exclusion": value = bc + sc - 2 * bc * sc; break;
                    }
                    if (useNonseparable) value = nonseparable[c];
                    output.Data[i + c] = (float)Clamp((1 - sa) * backdrop.Data[i + c] + (1 - ba) * source.Data[i + c] + sa * ba * value);
                }
                output.Data[i + 3] = (float)(sa + ba - sa * ba);
            }
        }
        // Small value types keep blend color arithmetic off the managed heap.
        private struct Rgb
        {
            internal double R, G, B;
            internal Rgb(double r, double g, double b) { R=r; G=g; B=b; }
            internal double this[int channel] => channel==0 ? R : channel==1 ? G : B;
            internal double Min => Math.Min(R,Math.Min(G,B));
            internal double Max => Math.Max(R,Math.Max(G,B));
        }
        private static double Lum(Rgb color) => .3 * color.R + .59 * color.G + .11 * color.B;
        private static double Sat(Rgb color) => color.Max - color.Min;
        private static Rgb SetLum(Rgb color, double lum)
        {
            double delta=lum-Lum(color);
            var result=new Rgb(color.R+delta,color.G+delta,color.B+delta);
            double min=result.Min, max=result.Max;
            if(min<0) result=new Rgb(lum+(result.R-lum)*lum/(lum-min),lum+(result.G-lum)*lum/(lum-min),lum+(result.B-lum)*lum/(lum-min));
            if(max>1) result=new Rgb(lum+(result.R-lum)*(1-lum)/(max-lum),lum+(result.G-lum)*(1-lum)/(max-lum),lum+(result.B-lum)*(1-lum)/(max-lum));
            return result;
        }
        private static Rgb SetSat(Rgb color, double sat)
        {
            double min=color.Min, max=color.Max;
            return max>min ? new Rgb((color.R-min)*sat/(max-min),(color.G-min)*sat/(max-min),(color.B-min)*sat/(max-min)) : new Rgb();
        }
        private Frame Blur(XmlNode node, Frame input,bool disposableInput=false)
        {
            var deviation = global::SVGImage.SVG.Utils.SvgNumberReader.List(Attr(node, "stdDeviation", "0"));
            double dx = deviation.Length > 0 ? deviation[0] : 0, dy = deviation.Length > 1 ? deviation[1] : dx;
            if (dx < 0 || dy < 0 || deviation.Length > 2) return BlurAxes(input, 0, 0, "none",disposableInput);
            return BlurAxes(input, dx * unitX * sx, dy * unitY * sy, Attr(node, "edgeMode", "none"),disposableInput);
        }
        private float Edge(Frame frame, int position, int line, int channel, bool horizontal, string edge)
        {
            int length = horizontal ? width : height;
            if (edge == "duplicate") position = Math.Max(0, Math.Min(length - 1, position));
            else if (edge == "wrap") position = (position % length + length) % length;
            return horizontal ? Pixel(frame, position, line, channel) : Pixel(frame, line, position, channel);
        }
        private Frame Morphology(XmlNode node, Frame input)
        {
            var values = global::SVGImage.SVG.Utils.SvgNumberReader.List(Attr(node, "radius", "0"));
            double rx = values.Length > 0 && values.Length <= 2 ? values[0] : 0, ry = values.Length == 2 ? values[1] : rx;
            if (rx < 0 || ry < 0) { var copy = new Frame(input.Data.Length, input.Linear); Array.Copy(input.Data, copy.Data, input.Data.Length); return copy; }
            bool dilate = Attr(node, "operator", "erode") == "dilate";
            return Extremum(Extremum(input, (int)Math.Min(width, Math.Ceiling(rx * unitX * sx)), true, dilate), (int)Math.Min(height, Math.Ceiling(ry * unitY * sy)), false, dilate);
        }
        private Frame Extremum(Frame input, int radius, bool horizontal, bool maximum)
        {
            var output = new Frame(input.Data.Length, input.Linear);
            int length = horizontal ? width : height, lines = horizontal ? height : width;
            var deque = new int[length + 2 * radius + 1];
            for (int line = 0; line < lines; line++) for (int c = 0; c < 4; c++)
            {
                int head = 0, tail = 0;
                for (int position = -radius; position < length + radius; position++)
                {
                    float value = Edge(input, position, line, c, horizontal, "none");
                    while (tail > head && (maximum ? Edge(input, deque[tail - 1], line, c, horizontal, "none") <= value : Edge(input, deque[tail - 1], line, c, horizontal, "none") >= value)) tail--;
                    deque[tail++] = position;
                    while (tail > head && deque[head] < position - 2 * radius) head++;
                    int at = position - radius;
                    if (at >= 0)
                    {
                        int index = horizontal ? (line * width + at) * 4 + c : (at * width + line) * 4 + c;
                        output.Data[index] = Edge(input, deque[head], line, c, horizontal, "none");
                    }
                }
            }
            return output;
        }
        private void Tile(Frame input, Frame output)
        {
            if (input.Area.IsEmpty || input.Area.Width <= 0 || input.Area.Height <= 0) return;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                double px = region.X + (x + .5) / sx, py = region.Y + (y + .5) / sy;
                px = ((px - input.Area.X) % input.Area.Width + input.Area.Width) % input.Area.Width + input.Area.X;
                py = ((py - input.Area.Y) % input.Area.Height + input.Area.Height) % input.Area.Height + input.Area.Y;
                for (int c = 0; c < 4; c++) output.Data[(y * width + x) * 4 + c] = Sample(input, (px - region.X) * sx - .5, (py - region.Y) * sy - .5, c);
            }
        }
    }
}
