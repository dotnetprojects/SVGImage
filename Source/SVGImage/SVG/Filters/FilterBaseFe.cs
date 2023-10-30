using System.Xml;

using System.Windows.Media.Effects;

namespace SVGImage.SVG.Filters
{
    using Shapes;

    public abstract class FilterBaseFe : Shape
    {
        public FilterBaseFe(SVG svg, XmlNode node, Shape parent)
            : base(svg, node, parent)
        {

        }

        public abstract BitmapEffect GetBitmapEffect();
    }
}

