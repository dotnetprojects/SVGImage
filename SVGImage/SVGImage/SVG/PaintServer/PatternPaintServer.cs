using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using SVGImage.SVG.Shapes;

namespace SVGImage.SVG.PaintServer
{
    public class PatternPaintServer : PaintServer
    {
        //x="0" y="0" width="2" height="12"
        //patternUnits
        //patternTransform

        private List<Shape> m_elements;

        public double X { get; private set; }

        public double Y { get; private set; }

        public double Width { get; private set; }

        public double Height { get; private set; }

        public Transform PatternTransform { get; protected set; }

        public string PatternUnits { get; private set; }

        public PatternPaintServer(PaintServerManager owner, SVG svg, XmlNode node) : base(owner)
        {
            this.PatternUnits = XmlUtil.AttrValue(node, "patternUnits", string.Empty);
            string transform = XmlUtil.AttrValue(node, "patternTransform", string.Empty);
            if (transform.Length > 0)
            {
                this.PatternTransform = ShapeUtil.ParseTransform(transform.ToLower());
            }

            m_elements = SVG.Parse(svg, node);
            this.X = XmlUtil.AttrValue(node, "x", double.NaN);
            this.Y = XmlUtil.AttrValue(node, "y", double.NaN);
            this.Width = XmlUtil.AttrValue(node, "width", double.NaN);
            this.Height = XmlUtil.AttrValue(node, "height", double.NaN);
        }

        public override Brush GetBrush(double opacity, SVG svg, SVGRender svgRender)
        {
            var db = new DrawingBrush() {Drawing = svgRender.LoadGroup(m_elements, null)}; //new Rect(X, Y, Width, Height))};
            //db.Viewport = new Rect(X, Y, Width, Height);
            //db.TileMode = TileMode.Tile;
            db.Transform = PatternTransform;
            //db.ViewboxUnits = BrushMappingMode.Absolute;
            //db.ViewportUnits = BrushMappingMode.Absolute;
            //if (this.PatternUnits == SVGTags.sUserSpaceOnUse)
            //{
            //}

            return db;
        }
    }
}
