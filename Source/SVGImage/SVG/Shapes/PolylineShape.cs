using System.Collections.Generic;
using System.Windows;
using System.Xml;

namespace SVGImage.SVG.Shapes
{
    using Utils;

    public sealed class PolylineShape : Shape
    {
        public PolylineShape(SVG svg, XmlNode node)
            : base(svg, node)
        {
            string points = XmlUtil.AttrValue(node, SVGTags.sPoints, string.Empty);
            this.Points = SvgNumberReader.Points(points);
        }

        public Point[] Points { get; private set; }
    }
}
