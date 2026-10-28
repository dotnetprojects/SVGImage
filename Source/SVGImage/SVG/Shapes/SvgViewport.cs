using System;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using SVGImage.SVG.Animation;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG.Shapes
{
    internal sealed class SvgViewport : Group
    {
        internal Rect Frame { get; }
        internal Rect? ViewBox { get; }
        internal bool OverflowVisible { get; }
        private readonly string aspect;
        internal SvgViewport(SVG svg, XmlNode node, Shape parent) : base(svg, node, parent)
        {
            var viewport = svg.ViewBox ?? new Rect(svg.Size);
            double x = XmlUtil.AttrValue(node, "x", 0, viewport.Width), y = XmlUtil.AttrValue(node, "y", 0, viewport.Height);
            double w = XmlUtil.AttrValue(node, "width", viewport.Width, viewport.Width), h = XmlUtil.AttrValue(node, "height", viewport.Height, viewport.Height);
            Frame = new Rect(x, y, Math.Max(0, w), Math.Max(0, h));
            ViewBox = ReadViewBox(node);
            aspect = XmlUtil.AttrValue(node, "preserveAspectRatio", "xMidYMid meet").Trim();
            string overflow;
            OverflowVisible = svg.StyleSheet.Resolve(node).TryGetValue("overflow", out overflow) && overflow == "visible";
        }
        internal static Rect? ReadViewBox(XmlNode node)
        {
            var values = AnimationTrack.Numbers(XmlUtil.AttrValue(node, "viewBox"));
            if (values.Length != 4 || values[2] <= 0 || values[3] <= 0) return null;
            return new Rect(values[0], values[1], values[2], values[3]);
        }
        internal Matrix ViewportTransform()
        {
            if (!ViewBox.HasValue) return new Matrix(1, 0, 0, 1, Frame.X, Frame.Y);
            var box = ViewBox.Value;
            double sx = Frame.Width / box.Width, sy = Frame.Height / box.Height, dx = 0, dy = 0;
            if (aspect != "none")
            {
                sx = sy = aspect.Contains("slice") ? Math.Max(sx, sy) : Math.Min(sx, sy);
                dx = (Frame.Width - box.Width * sx) * (aspect.Contains("xMin") ? 0 : aspect.Contains("xMax") ? 1 : .5);
                dy = (Frame.Height - box.Height * sy) * (aspect.Contains("YMin") ? 0 : aspect.Contains("YMax") ? 1 : .5);
            }
            return new Matrix(sx, 0, 0, sy, Frame.X + dx - box.X * sx, Frame.Y + dy - box.Y * sy);
        }
    }
}
