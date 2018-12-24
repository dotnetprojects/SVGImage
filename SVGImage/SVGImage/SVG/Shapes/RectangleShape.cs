using System.Xml;

namespace SVGImage.SVG.Shapes
{
    public class RectangleShape : Shape
    {
        private static Fill DefaultFill = null;

        public override Fill Fill
        {
            get
            {
                Fill f = base.Fill;
                if (f == null) f = DefaultFill;
                return f;
            }
        }

        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public double RX { get; set; }

        public double RY { get; set; }

        public RectangleShape(SVG svg, XmlNode node)
            : base(svg, node)
        {
            this.X = XmlUtil.AttrValue(node, "x", 0);
            this.Y = XmlUtil.AttrValue(node, "y", 0);
            this.Width = XmlUtil.AttrValue(node, "width", 0);
            this.Height = XmlUtil.AttrValue(node, "height", 0);
            this.RX = XmlUtil.AttrValue(node, "rx", 0);
            this.RY = XmlUtil.AttrValue(node, "ry", 0);

            if (DefaultFill == null)
            {
                DefaultFill = new Fill(svg);
                DefaultFill.Color = svg.PaintServers.Parse("black");
            }
        }
    }
}
