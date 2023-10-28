using System.Xml;
using System.Windows.Media.Effects;

namespace SVGImage.SVG.Filters
{
    using Utils;
    using Shapes;

    public class FilterFeGaussianBlur : FilterBaseFe
    {
        public string In { get; set; }

        public double StdDeviationX { get; set; }

        public double StdDeviationY { get; set; }

        public FilterFeGaussianBlur(SVG svg, XmlNode node, Shape parent)
            : base(svg, node, parent)
        {
            StdDeviationX = StdDeviationY = XmlUtil.AttrValue(node, "stdDeviation", 0);
        }

        public override BitmapEffect GetBitmapEffect()
        {
            return new BlurBitmapEffect() {Radius = StdDeviationX};
        }
    }
}
