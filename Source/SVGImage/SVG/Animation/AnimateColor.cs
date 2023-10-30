using System.Xml;

namespace SVGImage.SVG.Animation
{
    using Shapes;

    public sealed class AnimateColor : AnimationBase
    {
        public AnimateColor(SVG svg, XmlNode node, Shape parent)
            : base(svg, node, parent)
        {
        }
    }
}
