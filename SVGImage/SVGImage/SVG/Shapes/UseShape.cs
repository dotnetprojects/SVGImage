using System.Xml;

namespace SVGImage.SVG.Shapes
{
    public class UseShape : Shape
    {
        public double X { get; set; }

        public double Y { get; set; }

        public string hRef { get; set; }

        public UseShape(SVG svg, XmlNode node)
            : base(svg, node)
        {
            this.X = XmlUtil.AttrValue(node, "x", 0);
            this.Y = XmlUtil.AttrValue(node, "y", 0);
            this.hRef = XmlUtil.AttrValue(node, "xlink:href", string.Empty);
            if (this.hRef.StartsWith("#")) this.hRef = this.hRef.Substring(1);
        }
    }
}
