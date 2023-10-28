using System;
using System.Xml;

namespace SVGImage.SVG.Animation
{
    using Utils;
    using Shapes;

    public class AnimationBase : Shape
    {
        //https://www.mediaevent.de/tutorial/svg-animate-attribute.html
        public TimeSpan Duration { get; set; }

        public AnimationBase(SVG svg, XmlNode node, Shape parent)
            : base(svg, node, parent)
        {
            var d = XmlUtil.AttrValue(node, "dur", "");
            if (d.EndsWith("ms"))
                Duration = TimeSpan.FromMilliseconds(double.Parse(d.Substring(0, d.Length - 2)));
            else if (d.EndsWith("s"))
                Duration = TimeSpan.FromSeconds(double.Parse(d.Substring(0, d.Length - 1)));
            else
                Duration = TimeSpan.FromSeconds(double.Parse(d));
        }
    }
}
