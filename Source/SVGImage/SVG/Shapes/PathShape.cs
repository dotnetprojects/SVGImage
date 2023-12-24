using System.Xml;
using SVGImage.SVG.Shapes;

namespace SVGImage.SVG
{
    using Utils;

    public sealed class PathShape : Shape
    {
        static Fill DefaultFill = null;

        // http://apike.ca/prog_svg_paths.html
        public PathShape(SVG svg, XmlNode node, Shape parent) : base(svg, node, parent)
        {
            if (DefaultFill == null)
            {
                DefaultFill = Fill.CreateDefault(svg, "black");
            }

            this.ClosePath = false;
            string path = XmlUtil.AttrValue(node, "d", string.Empty);
            this.Data = path;
        }

        public override Fill Fill
        {
            get
            {
                Fill f = base.Fill;
                if (f == null)
                    f = DefaultFill;
                return f;
            }
        }

        public bool ClosePath { get; private set; }

        public string Data { get; private set; }
    }
}
