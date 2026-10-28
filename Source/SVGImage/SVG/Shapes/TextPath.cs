using System.Xml;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG.Shapes
{
    internal sealed class TextPath : TextShapeBase, ITextChild
    {
        internal string Href { get; }
        internal string StartOffset { get; }
        internal TextPath(SVG svg, XmlNode node, Shape parent) : base(svg, node, parent)
        {
            Href = XmlUtil.AttrValue(node, "href", XmlUtil.AttrValue(node, "xlink:href")).TrimStart('#');
            StartOffset = XmlUtil.AttrValue(node, "startOffset", "0");
        }
    }
}
