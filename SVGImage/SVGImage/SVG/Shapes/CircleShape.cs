using System.Xml;

namespace SVGImage.SVG.Shapes
{
    public class CircleShape : Shape
    {
        public double CX { get; set; }

        public double CY { get; set; }

        public double R { get; set; }

        public CircleShape(SVG svg, XmlNode node)
            : base(svg, node)
        {
            //this.
            this.CX = XmlUtil.AttrValue(node, "cx", 0, svg.Size.Width);
            this.CY = XmlUtil.AttrValue(node, "cy", 0, svg.Size.Height);
            this.R = XmlUtil.AttrValue(node, "r", 0);
        }
    }
}
