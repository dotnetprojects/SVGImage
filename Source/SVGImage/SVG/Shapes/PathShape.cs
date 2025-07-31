using System.Xml;
using SVGImage.SVG.Shapes;

namespace SVGImage.SVG
{
    using Utils;

    public sealed class PathShape : Shape
    {

        // http://apike.ca/prog_svg_paths.html
        public PathShape(SVG svg, XmlNode node, Shape parent) : base(svg, node, parent)
        {
            this.ClosePath = false;
            string path = XmlUtil.AttrValue(node, "d", string.Empty);
            this.Data = path;
        }
        protected override Fill DefaultFill()
        {
            return Fill.CreateDefault(Svg, "black");
        }

        public bool ClosePath { get; private set; }

        public string Data { get; private set; }
    }
}
