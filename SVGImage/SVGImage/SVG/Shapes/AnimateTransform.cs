using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using SVGImage.SVG.Shapes;

namespace SVGImage.SVG
{
    public class AnimateTransform : Shape
    {
        public AnimateTransform(SVG svg, XmlNode node)
            : base(svg, node)
        {
        }
    }
}
