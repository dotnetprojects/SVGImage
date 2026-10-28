using System;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Xml;
using SVGImage.SVG.Animation;
using SVGImage.SVG.Shapes;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG.Filters
{
    internal sealed class FilterColorMatrix : FilterBaseFe
    {
        private readonly double[] matrix;
        internal FilterColorMatrix(SVG svg, XmlNode node, Shape parent) : base(svg, node, parent)
        {
            string type = XmlUtil.AttrValue(node, "type", "matrix");
            var values = AnimationTrack.Numbers(XmlUtil.AttrValue(node, "values"));
            if (type == "matrix" && values.Length == 20) matrix = values;
            else if (type == "saturate" && values.Length == 1)
            {
                double s = values[0];
                matrix = new[] { .213+.787*s, .715-.715*s, .072-.072*s, 0, 0,
                    .213-.213*s, .715+.285*s, .072-.072*s, 0, 0,
                    .213-.213*s, .715-.715*s, .072+.928*s, 0, 0,
                    0, 0, 0, 1, 0 };
            }
        }
        public override BitmapEffect GetBitmapEffect() => null;
        internal void Apply(Drawing drawing)
        {
            if (matrix != null) DrawingColorConverter.MapColors(drawing, Map);
        }
        private Color Map(Color color)
        {
            var input = new[] { Linear(color.R / 255.0), Linear(color.G / 255.0), Linear(color.B / 255.0), color.A / 255.0, 1 };
            var output = new double[4];
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 5; col++) output[row] += matrix[row * 5 + col] * input[col];
                output[row] = Math.Max(0, Math.Min(1, output[row]));
            }
            return Color.FromArgb((byte)Math.Round(output[3] * 255), Srgb(output[0]), Srgb(output[1]), Srgb(output[2]));
        }
        private static double Linear(double v) => v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4);
        private static byte Srgb(double v) => (byte)Math.Round(255 * (v <= .0031308 ? v * 12.92 : 1.055 * Math.Pow(v, 1 / 2.4) - .055));
    }
}
