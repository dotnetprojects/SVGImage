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

        internal Drawing Render(Drawing drawing, bool preserveAnimation)
        {
            var primitives=new System.Collections.Generic.List<XmlNode>();
            foreach(XmlNode child in node.ChildNodes)
                if(child.NodeType==XmlNodeType.Element && child.LocalName.StartsWith("fe")) primitives.Add(child);
            // These vector paint mappings preserve native child animation clocks.
            // Other filters require a composited raster input.
            if(preserveAnimation && primitives.Count==1 && primitives[0].LocalName=="feColorMatrix" &&
                (Utils.XmlUtil.AttrValue(primitives[0],"type","matrix")=="matrix" || Utils.XmlUtil.AttrValue(primitives[0],"type")=="saturate"))
            {
                ApplyColors(drawing);
                return drawing;
            }
            return new RasterFilter(Svg,node,Utils.MaskDrawing.ObjectBounds(drawing),viewport).Render(drawing);
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
