using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using SVGImage.SVG.Animation;
using SVGImage.SVG.Shapes;

namespace SVGImage.SVG.Utils
{
    internal sealed class CssTransforms
    {
        private sealed class Function { internal string Name; internal double[] Values; }
        private readonly List<Function> functions = new List<Function>();
        private Matrix? constant;
        internal static CssTransforms FromMatrix(Matrix matrix) => new CssTransforms { constant = matrix };
        internal static CssTransforms Parse(string text)
        {
            var result = new CssTransforms();
            foreach (Match match in Regex.Matches(text, @"([a-zA-Z]+)\(([^)]*)\)"))
            {
                string name = match.Groups[1].Value.ToLowerInvariant();
                string args = Regex.Replace(match.Groups[2].Value, @"([-+]?(?:[\d.]+))(deg|grad|rad|turn)", m =>
                {
                    double angle = AnimationTrack.Number(m.Groups[1].Value);
                    string unit = m.Groups[2].Value;
                    angle *= unit == "turn" ? 360 : unit == "rad" ? 180 / Math.PI : unit == "grad" ? .9 : 1;
                    return angle.ToString(CultureInfo.InvariantCulture);
                });
                result.functions.Add(new Function { Name = name, Values = AnimationTrack.Numbers(args) });
            }
            return result;
        }
        internal Matrix Matrix => Interpolate(this, 0);
        internal Matrix Interpolate(CssTransforms other, double progress)
        {
            bool compatible = !constant.HasValue && !other.constant.HasValue &&
                (functions.Count == 0 || other.functions.Count == 0 || functions.Count == other.functions.Count && functions.Zip(other.functions, (a,b) => a.Name == b.Name).All(v => v));
            if (!compatible)
            {
                var a = Raw(); var b = other.Raw();
                return new Matrix(Mix(a.M11,b.M11,progress),Mix(a.M12,b.M12,progress),Mix(a.M21,b.M21,progress),Mix(a.M22,b.M22,progress),Mix(a.OffsetX,b.OffsetX,progress),Mix(a.OffsetY,b.OffsetY,progress));
            }
            var result = System.Windows.Media.Matrix.Identity;
            int count = Math.Max(functions.Count, other.functions.Count);
            for (int i = 0; i < count; i++)
            {
                var a = functions.Count == 0 ? null : functions[i];
                var b = other.functions.Count == 0 ? null : other.functions[i];
                string name = (a ?? b).Name;
                var av = Expand(name, a?.Values); var bv = Expand(name, b?.Values);
                var values = av.Zip(bv, (x,y) => Mix(x,y,progress)).ToArray();
                result.Prepend(FunctionMatrix(name, values));
            }
            return result;
        }
        private Matrix Raw()
        {
            if (constant.HasValue) return constant.Value;
            var result = System.Windows.Media.Matrix.Identity;
            foreach (var function in functions) result.Prepend(FunctionMatrix(function.Name, Expand(function.Name, function.Values)));
            return result;
        }
        private static double Mix(double a, double b, double t) => a + (b - a) * t;
        private static double[] Expand(string name, double[] values)
        {
            var result = name == "matrix" ? new[] { 1.0,0,0,1,0,0 } : name.StartsWith("scale") ? new[] { 1.0,1,0 } : new double[3];
            if (values != null) Array.Copy(values, result, Math.Min(values.Length, result.Length));
            if (name == "scale" && values?.Length == 1) result[1] = result[0];
            return result;
        }
        private static Matrix FunctionMatrix(string name, double[] values)
        {
            var m = System.Windows.Media.Matrix.Identity;
            switch (name)
            {
                case "translate": m.Translate(values[0],values[1]); break;
                case "translatex": m.Translate(values[0],0); break;
                case "translatey": m.Translate(0,values[0]); break;
                case "scale": m.Scale(values[0],values[1]); break;
                case "scalex": m.Scale(values[0],1); break;
                case "scaley": m.Scale(1,values[0]); break;
                case "rotate": m.RotateAt(values[0],values[1],values[2]); break;
                case "skew": m.Skew(values[0],values[1]); break;
                case "skewx": m.Skew(values[0],0); break;
                case "skewy": m.Skew(0,values[0]); break;
                case "matrix": return new Matrix(values[0],values[1],values[2],values[3],values[4],values[5]);
            }
            return m;
        }

        internal static Point Origin(Shape shape, Rect objectBounds)
        {
            string value;
            if (!shape.CssProperties.TryGetValue("transform-origin", out value)) return new Point();
            string box;
            var reference = shape.CssProperties.TryGetValue("transform-box", out box) && box == "fill-box" && !objectBounds.IsEmpty
                ? objectBounds : shape.Svg.ViewBox ?? new Rect(shape.Svg.Size);
            var parts = Regex.Split(value.Trim(), @"\s+");
            string x = parts[0], y = parts.Length > 1 ? parts[1] : "center";
            if (x == "top" || x == "bottom") { string temp = x; x = y; y = temp; }
            return new Point(reference.X + Length(x,reference.Width), reference.Y + Length(y,reference.Height));
        }
        private static double Length(string value, double size)
        {
            if (value == "center") return size / 2;
            if (value == "left" || value == "top") return 0;
            if (value == "right" || value == "bottom") return size;
            var numbers = AnimationTrack.Numbers(value);
            return numbers.Length == 0 ? 0 : numbers[0] * (value.EndsWith("%") ? size / 100 : 1);
        }
        internal static Matrix Around(Matrix matrix, Point origin)
        {
            matrix.TranslatePrepend(-origin.X,-origin.Y);
            matrix.Translate(origin.X,origin.Y);
            return matrix;
        }
    }
}
