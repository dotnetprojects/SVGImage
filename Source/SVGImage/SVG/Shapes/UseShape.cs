using System.Xml;

namespace SVGImage.SVG.Shapes
{
    using Utils;

    public sealed class UseShape : Shape
    {
        public double X { get; set; }

        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public string hRef { get; set; }

        public UseShape(SVG svg, XmlNode node) : base(svg, node)
        {
            Width = XmlUtil.AttrValue(node, "width", svg.Size.Width, svg.Size.Width);
            Height = XmlUtil.AttrValue(node, "height", svg.Size.Height, svg.Size.Height);
        }

        protected override void Parse(SVG svg, string name, string value)
        {
            if (name.Contains(":"))
                name = name.Split(':')[1];

            if (name == SVGTags.sHref)
            {
                this.hRef = value;
                if (this.hRef.StartsWith("#"))
                    this.hRef = this.hRef.Substring(1);
                return;
            }
            if (name == "x")
            {
                this.X = XmlUtil.GetDoubleValue(value, svg.Size.Width);
                return;
            }
            if (name == "y")
            {
                this.Y = XmlUtil.GetDoubleValue(value, svg.Size.Height);
                return;
            }

            base.Parse(svg, name, value);
        }
    }
}
