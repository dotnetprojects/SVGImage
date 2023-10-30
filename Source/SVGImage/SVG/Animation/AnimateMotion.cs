using System.Xml;

namespace SVGImage.SVG.Animation
{
    using Shapes;

    public sealed class AnimateMotion : AnimationBase
    {
        public AnimateMotion(SVG svg, XmlNode node, Shape parent)
            : base(svg, node, parent)
        {
        }
    }
}
