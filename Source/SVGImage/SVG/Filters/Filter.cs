using System.Xml;

using System.Windows.Media.Effects;
using System.Windows.Media;

namespace SVGImage.SVG.Filters
{
    using Shapes;

    public sealed class Filter : Group
    {
        public Filter(SVG svg, XmlNode node, Shape parent)
            : base(svg, node, parent)
        {
           
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
