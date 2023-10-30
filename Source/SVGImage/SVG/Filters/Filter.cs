using System.Xml;

using System.Windows.Media.Effects;

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
                beg.Children.Add(element.GetBitmapEffect());
            }

            return beg;
        }
    }
}
