using System;
using System.Xml;
using System.Windows;

namespace SVGImage.SVG.Shapes
{
    using Utils;

    public sealed class CircleShape : Shape
    {
        public CircleShape(SVG svg, XmlNode node) : base(svg, node)
        {
            Rect? box = svg.ViewBox;

            this.CX = XmlUtil.AttrValue(node, "cx", 0, box.HasValue ? box.Value.Width : svg.Size.Width);
            this.CY = XmlUtil.AttrValue(node, "cy", 0, box.HasValue ? box.Value.Height : svg.Size.Height);
            // see https://oreillymedia.github.io/Using_SVG/extras/ch05-percentages.html
            var diagRef = Math.Sqrt(svg.Size.Width * svg.Size.Width + svg.Size.Height * svg.Size.Height) / Math.Sqrt(2); 
            if (box.HasValue)
                diagRef = Math.Sqrt(box.Value.Width * box.Value.Width + box.Value.Height * box.Value.Height) / Math.Sqrt(2);
            this.R = XmlUtil.AttrValue(node, "r", 0, diagRef);
        }

        public double CX { get; set; }

        public double CY { get; set; }

        public double R { get; set; }
    }
}
