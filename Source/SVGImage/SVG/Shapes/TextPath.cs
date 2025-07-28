using System;
using System.Xml;

namespace SVGImage.SVG.Shapes
{
    internal class TextPath : TextShapeBase, ITextChild
    {
        protected TextPath(SVG svg, XmlNode node, Shape parent) : base(svg, node, parent)
        {
            throw new NotImplementedException("TextPath is not yet implemented.");
        }
    }




}
