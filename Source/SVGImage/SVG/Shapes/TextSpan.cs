using System.Xml;

namespace SVGImage.SVG.Shapes
{
    public class TextSpan : TextShapeBase, ITextChild
    {

        public TextSpan(SVG svg, XmlNode node, Shape parent) : base(svg, node, parent)
        {
        }

    }




}
