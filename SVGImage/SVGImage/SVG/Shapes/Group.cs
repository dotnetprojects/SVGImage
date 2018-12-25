using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using DotNetProjects.SVGImage.SVG.Animation;
using DotNetProjects.SVGImage.SVG.Shapes.Filter;

namespace SVGImage.SVG.Shapes
{
    public class Group : Shape
    {
        private List<Shape> m_elements = new List<Shape>();

        private static Regex _regexStyle =
            new Regex("([\\.,<>a-zA-Z0-9: ]*){([^}]*)}", RegexOptions.Compiled | RegexOptions.Singleline);

        public IList<Shape> Elements
        {
            get { return this.m_elements.AsReadOnly(); }
        }

        public bool IsSwitch { get; set; }

        private Shape AddChild(Shape shape)
        {
            this.m_elements.Add(shape);
            shape.Parent = this;
            return shape;
        }

        public Group(SVG svg, XmlNode node, Shape parent)
            : base(svg, node)
        {
            // parent on group must be set before children are added
            var clp = XmlUtil.AttrValue(node, "clip-path", null);
            if (!string.IsNullOrEmpty(clp))
            {
                Shape result;
                string id = ShapeUtil.ExtractBetween(clp, '(', ')');
                if (id.Length > 0 && id[0] == '#') id = id.Substring(1);
                svg.m_shapes.TryGetValue(id, out result);
                this.m_clip = result as Clip;
            }

            this.Parent = parent;
            foreach (XmlNode childnode in node.ChildNodes)
            {
                Shape shape = AddToList(svg, this.m_elements, childnode, this);
                if (shape != null) shape.Parent = this;
            }
        }

        public static Shape AddToList(SVG svg, List<Shape> list, XmlNode childnode, Shape parent)
        {
            if (childnode.NodeType != XmlNodeType.Element) return null;

            Shape retVal = null;

            var nodeName = GetNodeName(childnode);
            if (nodeName == null)
                return null;
            if (nodeName == SVGTags.sStyle)
            {
                var match = _regexStyle.Match(childnode.InnerText);
                while (match.Success)
                {
                    var name = match.Groups[1].Value.Trim();
                    var value = match.Groups[2].Value;
                    foreach (var nm in name.Split(','))
                    {
                        svg.m_styles.Add(nm, new List<XmlAttribute>());
                        foreach (ShapeUtil.Attribute styleitem in XmlUtil.SplitStyle(svg, value))
                        {
                            svg.m_styles[nm].Add(new XmlUtil.StyleItem(childnode, styleitem.Name, styleitem.Value));
                        }
                    }

                    match = match.NextMatch();
                }
            }
            else if (nodeName == SVGTags.sShapeRect)
                retVal = new RectangleShape(svg, childnode);
            else if (nodeName == SVGTags.sFilter)
                retVal = new Filter(svg, childnode, parent);
            else if (nodeName == SVGTags.sFeGaussianBlur)
                retVal = new FilterFeGaussianBlur(svg, childnode, parent);
            else if (nodeName == SVGTags.sShapeCircle)
                retVal = new CircleShape(svg, childnode);
            else if (nodeName == SVGTags.sShapeEllipse)
                retVal = new EllipseShape(svg, childnode);
            else if (nodeName == SVGTags.sShapeLine)
                retVal = new LineShape(svg, childnode);
            else if (nodeName == SVGTags.sShapePolyline)
                retVal = new PolylineShape(svg, childnode);
            else if (nodeName == SVGTags.sShapePolygon)
                retVal = new PolygonShape(svg, childnode);
            else if (nodeName == SVGTags.sShapePath)
                retVal = new PathShape(svg, childnode, parent);
            else if (nodeName == SVGTags.sClipPath)
                retVal = new Clip(svg, childnode, parent);
            else if (nodeName == SVGTags.sShapeGroup)
                retVal = new Group(svg, childnode, parent);
            else if (nodeName == SVGTags.sSwitch)
                retVal = new Group(svg, childnode, parent) {IsSwitch = true};
            else if (nodeName == SVGTags.sShapeUse)
                retVal = new UseShape(svg, childnode);
            else if (nodeName == SVGTags.sShapeImage)
                retVal = new ImageShape(svg, childnode);
            else if (nodeName == SVGTags.sAnimate)
                retVal = new Animate(svg, childnode, parent);
            else if (nodeName == SVGTags.sAnimateColor)
                retVal = new AnimateColor(svg, childnode, parent);
            else if (nodeName == SVGTags.sAnimateMotion)
                retVal = new AnimateMotion(svg, childnode, parent);
            else if (nodeName == SVGTags.sAnimateTransform)
                retVal = new AnimateTransform(svg, childnode, parent);
            else if (nodeName == SVGTags.sText)
                retVal = new TextShape(svg, childnode, parent);
            else if (nodeName == SVGTags.sLinearGradient)
            {
                svg.PaintServers.Create(svg, childnode);
                return null;
            }
            else if (nodeName == SVGTags.sRadialGradient)
            {
                svg.PaintServers.Create(svg, childnode);
                return null;
            }
            else if (nodeName == SVGTags.sDefinitions)
            {
                ReadDefs(svg, list, childnode);
                return null;
            }
            else if (nodeName == SVGTags.sSymbol)
            {
                retVal = new Group(svg, childnode, parent);
            }

            if (retVal != null)
            {
                list.Add(retVal);
                if (retVal.Id.Length > 0)
                    svg.AddShape(retVal.Id, retVal);
            }

            if (nodeName == SVGTags.sSymbol)
            {
                return null;
            }

            return retVal;
        }

        private static void ReadDefs(SVG svg, List<Shape> list, XmlNode node)
        {
            list = new List<Shape>(); // temp list, not needed. 
            //ShapeGroups defined in the 'def' section is added the the 'Shapes' dictionary in SVG for later reference
            foreach (XmlNode childnode in node.ChildNodes)
            {
                var nodeName = GetNodeName(childnode);
                if (nodeName == SVGTags.sLinearGradient ||
                    nodeName == SVGTags.sRadialGradient ||
                    nodeName == SVGTags.sPattern)
                {
                    svg.PaintServers.Create(svg, childnode);
                    continue;
                }

                Group.AddToList(svg, list, childnode, null);
            }
        }

        private static string GetNodeName(XmlNode node)
        {
            var nodeName = node.Name;
            if (nodeName.Contains(":"))
            {
                var parts = nodeName.Split(':');
                var ns = node.GetNamespaceOfPrefix(parts[0]);
                if (ns == SVGTags.sNsSvg)
                {
                    return parts[1];
                }

                return null;
            }
            return nodeName;
        }
    }
}
