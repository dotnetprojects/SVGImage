using System.Xml;

namespace SVGImage.SVG.Shapes
{
    public class EllipseShape : Shape
    {
        public double CX { get; set; }

        public double CY { get; set; }

        public double RX { get; set; }

        public double RY { get; set; }

        public EllipseShape(SVG svg, XmlNode node)
            : base(svg, node)
        {
            this.CX = XmlUtil.AttrValue(node, "cx", 0, svg.Size.Width);
            this.CY = XmlUtil.AttrValue(node, "cy", 0, svg.Size.Height);
            this.RX = XmlUtil.AttrValue(node, "rx", 0, svg.Size.Width);
            this.RY = XmlUtil.AttrValue(node, "ry", 0, svg.Size.Height);
        }
    }
}
