using System;
using System.Collections.Generic;
using System.Xml;
using System.Windows;
using System.Windows.Media;
using SVGImage.SVG.Shapes;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG.PaintServers
{
    public sealed class PatternPaintServer : PaintServer
    {
        private readonly XmlNode node;
        private readonly Size viewport;
        private bool rendering;
        public PatternPaintServer(PaintServerManager owner, SVG svg, XmlNode node) : base(owner)
        {
            this.node = node;
            viewport = svg.ViewBox?.Size ?? svg.Size;
        }
        public PatternPaintServer(PaintServerManager owner, Brush newBrush) : base(owner, newBrush) { }
        public double X => Number("x", 0, viewport.Width);
        public double Y => Number("y", 0, viewport.Height);
        public double Width => Number("width", 0, viewport.Width);
        public double Height => Number("height", 0, viewport.Height);
        public Transform PatternTransform => ShapeUtil.ParseTransform(Attribute("patternTransform", ""));
        public string PatternUnits => Attribute("patternUnits", "objectBoundingBox");

        private IEnumerable<XmlNode> Templates()
        {
            var visited = new HashSet<PatternPaintServer>();
            var current = this;
            while (current != null && current.node != null && visited.Add(current))
            {
                yield return current.node;
                string href = XmlUtil.AttrValue(current.node, "href", XmlUtil.AttrValue(current.node, "xlink:href"));
                current = href.StartsWith("#") ? Owner.GetServer(href.Substring(1)) as PatternPaintServer : null;
            }
        }
        private string Attribute(string name, string fallback)
        {
            foreach (var template in Templates()) if (template.Attributes[name] != null) return template.Attributes[name].Value;
            return fallback;
        }
        private double Number(string name, double fallback, double reference) => XmlUtil.GetDoubleValue(Attribute(name, fallback.ToString(System.Globalization.CultureInfo.InvariantCulture)), reference);

        public override Brush GetBrush(double opacity, SVG svg, SVGRender svgRender, Rect bounds)
        {
            if (Brush != null) return Brush;
            if (rendering || bounds.IsEmpty) return Brushes.Transparent;
            bool relative = PatternUnits != "userSpaceOnUse";
            double x = Number("x", 0, relative ? 1 : viewport.Width), y = Number("y", 0, relative ? 1 : viewport.Height);
            double width = Number("width", 0, relative ? 1 : viewport.Width), height = Number("height", 0, relative ? 1 : viewport.Height);
            if (relative) { x = bounds.X + x * bounds.Width; y = bounds.Y + y * bounds.Height; width *= bounds.Width; height *= bounds.Height; }
            if (width <= 0 || height <= 0) return Brushes.Transparent;
            var tile = new Rect(x, y, width, height);
            XmlNode content = null;
            foreach (var template in Templates())
            {
                foreach (XmlNode child in template.ChildNodes) if (child.NodeType == XmlNodeType.Element) { content = template; break; }
                if (content != null) break;
            }
            if (content == null) return Brushes.Transparent;
            rendering = true;
            var oldSize = svg.Size;
            var oldBox = svg.ViewBox;
            try
            {
                bool normalized = Attribute("patternContentUnits", "userSpaceOnUse") == "objectBoundingBox";
                var viewBoxNode = content.OwnerDocument.CreateElement("svg", SVGTags.sNsSvg);
                viewBoxNode.SetAttribute("viewBox", Attribute("viewBox", ""));
                var box = SvgViewport.ReadViewBox(viewBoxNode);
                svg.Size = box?.Size ?? (normalized ? new Size(1, 1) : viewport);
                svg.ViewBox = box;
                var group = new Group(svg, content, null);
                var drawing = svgRender.LoadGroup(group.Elements, null, false);
                var matrix = Matrix.Identity;
                if (box.HasValue)
                {
                    viewBoxNode.SetAttribute("x", x.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    viewBoxNode.SetAttribute("y", y.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    viewBoxNode.SetAttribute("width", width.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    viewBoxNode.SetAttribute("height", height.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    viewBoxNode.SetAttribute("preserveAspectRatio", Attribute("preserveAspectRatio", "xMidYMid meet"));
                    matrix = new SvgViewport(svg, viewBoxNode, null).ViewportTransform();
                }
                else if (normalized) matrix = new Matrix(bounds.Width, 0, 0, bounds.Height, bounds.X, bounds.Y);
                drawing.Transform = new MatrixTransform(matrix);
                return new DrawingBrush(drawing)
                {
                    ViewboxUnits = BrushMappingMode.Absolute,
                    Viewbox = tile,
                    ViewportUnits = BrushMappingMode.Absolute,
                    Viewport = tile,
                    Stretch = Stretch.Fill,
                    TileMode = TileMode.Tile,
                    Transform = PatternTransform,
                    Opacity = opacity
                };
            }
            finally { svg.Size = oldSize; svg.ViewBox = oldBox; rendering = false; }
        }
    }
}
