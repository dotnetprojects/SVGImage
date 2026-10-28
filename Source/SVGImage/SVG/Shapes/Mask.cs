using System.Windows;
using System.Xml;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG.Shapes
{
    internal sealed class Mask : Group
    {
        private readonly XmlNode node;
        private string maskType;

        internal Mask(SVG svg, XmlNode node, Shape parent) : base(svg, node, parent)
        {
            this.node = node;
        }

        internal bool UsesAlpha => maskType == "alpha";
        internal bool ContentUsesBoundingBox => XmlUtil.AttrValue(node, "maskContentUnits") == "objectBoundingBox";

        internal Rect GetRegion(Rect bounds)
        {
            bool boundingBox = XmlUtil.AttrValue(node, "maskUnits", "objectBoundingBox") != "userSpaceOnUse";
            var viewport = Svg.ViewBox ?? new Rect(Svg.Size);
            double width = boundingBox ? 1 : viewport.Width;
            double height = boundingBox ? 1 : viewport.Height;
            double x = XmlUtil.GetDoubleValue(XmlUtil.AttrValue(node, "x", "-10%"), width);
            double y = XmlUtil.GetDoubleValue(XmlUtil.AttrValue(node, "y", "-10%"), height);
            double w = XmlUtil.GetDoubleValue(XmlUtil.AttrValue(node, "width", "120%"), width);
            double h = XmlUtil.GetDoubleValue(XmlUtil.AttrValue(node, "height", "120%"), height);
            if (boundingBox)
            {
                x = bounds.X + x * bounds.Width;
                y = bounds.Y + y * bounds.Height;
                w *= bounds.Width;
                h *= bounds.Height;
            }
            if (w <= 0 || h <= 0 || double.IsNaN(x + y + w + h) || double.IsInfinity(x + y + w + h))
                return Rect.Empty;
            return new Rect(x, y, w, h);
        }

        protected override void Parse(SVG svg, string name, string value)
        {
            if (name == "mask-type") maskType = value;
            else base.Parse(svg, name, value);
        }
    }
}
