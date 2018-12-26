using System.Xml;

namespace SVGImage.SVG.Shapes
{
    public class UseShape : Shape
    {
        public double X { get; set; }

        public double Y { get; set; }

        public string hRef { get; set; }

        public UseShape(SVG svg, XmlNode node) : base(svg, node)
        {
            this.X = XmlUtil.AttrValue(node, "x", 0, svg.Size.Width);
            this.Y = XmlUtil.AttrValue(node, "y", 0, svg.Size.Height);
            //this.hRef = XmlUtil.AttrValue(node, "xlink:href", string.Empty);
            //if (this.hRef.StartsWith("#"))
            //    this.hRef = this.hRef.Substring(1);
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

            base.Parse(svg, name, value);
        }
    }
}
