using System.Windows;
using System.Xml;

namespace SVGImage.SVG.Shapes
{
    internal class LineShape : Shape
    {
        public Point P1 { get; private set; }

        public Point P2 { get; private set; }

        public LineShape(SVG svg, XmlNode node)
            : base(svg, node)
        {
            double x1 = XmlUtil.AttrValue(node, "x1", 0);
            double y1 = XmlUtil.AttrValue(node, "y1", 0);
            double x2 = XmlUtil.AttrValue(node, "x2", 0);
            double y2 = XmlUtil.AttrValue(node, "y2", 0);
            this.P1 = new Point(x1, y1);
            this.P2 = new Point(x2, y2);
        }
    }
}
