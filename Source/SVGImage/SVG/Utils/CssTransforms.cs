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
        internal static CssTransforms Parse(string text, Rect reference = default(Rect))
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
                var tokens = Regex.Matches(args, @"[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?%?").Cast<Match>().Select(m => m.Value).ToArray();
                var values = tokens.Select(token => AnimationTrack.Number(token.TrimEnd('%'))).ToArray();
                if (name.StartsWith("translate"))
                    for (int i = 0; i < values.Length; i++) if (tokens[i].EndsWith("%"))
                        values[i] *= (name == "translatey" || i == 1 ? reference.Height : reference.Width) / 100;
                result.functions.Add(new Function { Name = name, Values = values });
            }
            return result;
        }
        internal Matrix Matrix => Interpolate(this, 0);
        internal Matrix Interpolate(CssTransforms other, double progress)
        {
            if (constant.HasValue || other.constant.HasValue) return InterpolateMatrices(Raw(), other.Raw(), progress);
            var result = System.Windows.Media.Matrix.Identity;
            int count = Math.Max(functions.Count, other.functions.Count);
            for (int i = 0; i < count; i++)
            {
                var a = i < functions.Count ? functions[i] : null;
                var b = i < other.functions.Count ? other.functions[i] : null;
                string name = (a ?? b).Name;
                if (name == "matrix" || a != null && b != null && a.Name != b.Name)
                {
                    result.Prepend(InterpolateMatrices(Raw(i), other.Raw(i), progress));
                    break;
                }
                var av = Expand(name, a?.Values); var bv = Expand(name, b?.Values);
                var values = av.Zip(bv, (x,y) => Mix(x,y,progress)).ToArray();
                result.Prepend(FunctionMatrix(name, values));
            }
            return result;
        }
        private Matrix Raw(int start = 0)
        {
            if (constant.HasValue) return constant.Value;
            var result = System.Windows.Media.Matrix.Identity;
            foreach (var function in functions.Skip(start)) result.Prepend(FunctionMatrix(function.Name, Expand(function.Name, function.Values)));
            return result;
        }
        // CSS 2D decomposition: interpolate translation, signed scales, rotation
        // and the residual linear matrix. Keep matching function prefixes above.
        private static double[] Decompose(Matrix m)
        {
            double sx = Math.Sqrt(m.M11*m.M11 + m.M12*m.M12), sy = Math.Sqrt(m.M21*m.M21 + m.M22*m.M22);
            double determinant = m.Determinant;
            if (determinant == 0 || sx == 0 || sy == 0) return null;
            if (determinant < 0) { if (m.M11 < m.M22) sx = -sx; else sy = -sy; }
            double a=m.M11/sx, b=m.M12/sx, c=m.M21/sy, d=m.M22/sy;
            double angle = Math.Atan2(b,a)*180/Math.PI;
            return new[] { m.OffsetX,m.OffsetY,sx,sy,angle,a*a-b*c,a*b-b*d,b*a+a*c,b*b+a*d };
        }
        private static Matrix InterpolateMatrices(Matrix a, Matrix b, double progress)
        {
            if (progress <= 0) return a;
            if (progress >= 1) return b;
            var av=Decompose(a); var bv=Decompose(b);
            if (av==null || bv==null) return progress < .5 ? a : b;
            if (av[2]<0 && bv[3]<0 || av[3]<0 && bv[2]<0)
            { av[2]=-av[2]; av[3]=-av[3]; av[4]+=av[4]<0?180:-180; }
            if (Math.Abs(av[4]-bv[4])>180) { if(av[4]>bv[4]) av[4]-=360; else bv[4]-=360; }
            var v=av.Zip(bv,(x,y)=>Mix(x,y,progress)).ToArray();
            var matrix=new Matrix(v[5],v[6],v[7],v[8],0,0);
            matrix.RotatePrepend(v[4]); matrix.ScalePrepend(v[2],v[3]); matrix.Translate(v[0],v[1]);
            return matrix;
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

        internal static Rect ReferenceBox(Shape shape, Rect objectBounds)
        {
            string box;
            return shape.CssProperties.TryGetValue("transform-box", out box) && box == "fill-box" && !objectBounds.IsEmpty
                ? objectBounds : shape.TransformViewport;
        }
        internal static Point Origin(Shape shape, Rect objectBounds)
        {
            string value;
            if (!shape.CssProperties.TryGetValue("transform-origin", out value)) return new Point();
            var reference = ReferenceBox(shape, objectBounds);
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
