using System;
using System.Windows;
using System.Xml;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG.Shapes
{
    internal sealed class Symbol : Group
    {
        internal Rect? ViewBox { get; }
        internal string PreserveAspectRatio { get; }
        internal bool OverflowVisible { get; }

        internal Symbol(SVG svg, XmlNode node, Shape parent) : base(svg, node, parent)
        {
            var values = Animation.AnimationTrack.Numbers(XmlUtil.AttrValue(node, "viewBox"));
            if (values.Length == 4 && values[2] > 0 && values[3] > 0)
                ViewBox = new Rect(values[0], values[1], values[2], values[3]);
            PreserveAspectRatio = XmlUtil.AttrValue(node, "preserveAspectRatio", "xMidYMid meet");
            OverflowVisible = XmlUtil.AttrValue(node, "overflow") == "visible";
        }

        internal System.Windows.Media.Matrix Viewport(double x, double y, double width, double height)
        {
            var box = ViewBox.Value;
            double sx = width / box.Width, sy = height / box.Height;
            double dx = 0, dy = 0;
            if (PreserveAspectRatio != "none")
            {
                sx = sy = PreserveAspectRatio.Contains("slice") ? Math.Max(sx, sy) : Math.Min(sx, sy);
                dx = (width - box.Width * sx) * (PreserveAspectRatio.Contains("xMin") ? 0 : PreserveAspectRatio.Contains("xMax") ? 1 : .5);
                dy = (height - box.Height * sy) * (PreserveAspectRatio.Contains("YMin") ? 0 : PreserveAspectRatio.Contains("YMax") ? 1 : .5);
            }
            return new System.Windows.Media.Matrix(sx, 0, 0, sy, x + dx - box.X * sx, y + dy - box.Y * sy);
        }
    }
}
