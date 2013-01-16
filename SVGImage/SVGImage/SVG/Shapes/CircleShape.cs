using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace SVGImage.SVG.Shapes
{
    internal class CircleShape : Shape
    {
        public double CX { get; set; }

        public double CY { get; set; }

        public double R { get; set; }

        public CircleShape(SVG svg, XmlNode node)
            : base(svg, node)
        {
            this.CX = XmlUtil.AttrValue(node, "cx", 0);
            this.CY = XmlUtil.AttrValue(node, "cy", 0);
            this.R = XmlUtil.AttrValue(node, "r", 0);
        }
    }
}
