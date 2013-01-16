using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;

namespace SVGImage.SVG.Shapes
{
    internal class PolylineShape : Shape
    {
        public Point[] Points { get; private set; }

        public PolylineShape(SVG svg, XmlNode node)
            : base(svg, node)
        {
            string points = XmlUtil.AttrValue(node, SVGTags.sPoints, string.Empty);
            ShapeUtil.StringSplitter split = new ShapeUtil.StringSplitter(points);
            List<Point> list = new List<Point>();
            while (split.More)
            {
                list.Add(split.ReadNextPoint());
            }
            this.Points = list.ToArray();
        }
    }
}
