using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace SVGImage.SVG.Shapes
{
    internal class EllipseShape : Shape
    {
        public double CX { get; set; }

        public double CY { get; set; }

        public double RX { get; set; }

        public double RY { get; set; }

        public EllipseShape(SVG svg, XmlNode node)
            : base(svg, node)
        {
            this.CX = XmlUtil.AttrValue(node, "cx", 0);
            this.CY = XmlUtil.AttrValue(node, "cy", 0);
            this.RX = XmlUtil.AttrValue(node, "rx", 0);
            this.RY = XmlUtil.AttrValue(node, "ry", 0);
        }
    }
}
