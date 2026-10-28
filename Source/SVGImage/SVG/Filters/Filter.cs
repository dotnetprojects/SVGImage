using System.Xml;

using System.Windows.Media.Effects;
using System.Windows.Media;

namespace SVGImage.SVG.Filters
{
    using Shapes;

    public sealed class Filter : Group
    {
        private readonly XmlNode node;
        private readonly System.Windows.Rect viewport;
        public Filter(SVG svg, XmlNode node, Shape parent)
            : base(svg, node, parent)
        {
            this.node = node;
            viewport = svg.ViewBox ?? new System.Windows.Rect(svg.Size);
        }

        internal Drawing Render(Drawing drawing)
        {
            foreach (XmlNode child in node.ChildNodes)
                if (child.LocalName == "feTurbulence" || child.LocalName == "feDisplacementMap")
                    return new RasterFilter(Svg,node,Utils.MaskDrawing.ObjectBounds(drawing),viewport).Render(drawing);
            ApplyColors(drawing);
            return drawing;
        }

        public BitmapEffect GetBitmapEffect()
        {
            var beg = new BitmapEffectGroup();
            foreach (FilterBaseFe element in this.Elements)
            {
                var effect = element.GetBitmapEffect();
                if (effect != null) beg.Children.Add(effect);
            }

            return beg;
        }

        internal void ApplyColors(Drawing drawing)
        {
            foreach (var element in Elements)
                if (element is FilterColorMatrix matrix) matrix.Apply(drawing);
        }
    }
}
