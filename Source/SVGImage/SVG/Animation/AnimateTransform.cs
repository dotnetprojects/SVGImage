using System;
using System.Xml;

namespace SVGImage.SVG.Animation
{
    using Utils;
    using Shapes;

    public sealed class AnimateTransform : AnimationBase
    {
        public string AttributeName { get; set; }

        public AnimateTransformType Type { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public string Values { get; set; }

        public string RepeatType { get; set; }

        public AnimateTransform(SVG svg, XmlNode node, Shape parent)
            : base(svg, node, parent)
        {
            var valTranslate = XmlUtil.AttrValue(node, "type", "translate");
            if (Enum.TryParse<AnimateTransformType>(valTranslate, true, out var parsed))
                this.Type = parsed;
            this.From = XmlUtil.AttrValue(node, "from", null);
            this.To = XmlUtil.AttrValue(node, "to", null);
            this.AttributeName = XmlUtil.AttrValue(node, "attributeName", null);
            this.RepeatType = XmlUtil.AttrValue(node, "repeatCount", "indefinite");
            this.Values = XmlUtil.AttrValue(node, "values", null);
        }
    }
}
