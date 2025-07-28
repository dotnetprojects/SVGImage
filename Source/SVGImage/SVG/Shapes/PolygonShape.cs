using System.Collections.Generic;
using System.Windows;
using System.Xml;

namespace SVGImage.SVG.Shapes
{
    using Utils;

    public sealed class PolygonShape : Shape
    {
        public PolygonShape(SVG svg, XmlNode node)
            : base(svg, node)
        {

            string points = XmlUtil.AttrValue(node, SVGTags.sPoints, string.Empty);
            var split = new StringSplitter(points);
            List<Point> list = new List<Point>();
            while (split.More)
            {
                list.Add(split.ReadNextPoint());
            }
            this.Points = list.ToArray();
        }

        public Point[] Points { get; private set; }

        protected override Fill DefaultFill()
        {
            return Fill.CreateDefault(Svg, "black");
        }
    }
}
