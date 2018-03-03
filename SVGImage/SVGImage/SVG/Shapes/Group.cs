using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace SVGImage.SVG.Shapes
{
    internal class Group : Shape
    {
        private List<Shape> m_elements = new List<Shape>();

        private static Regex _regexStyle =
            new Regex("([\\.<>a-zA-Z0-9 ]*){([^}]*)}", RegexOptions.Compiled | RegexOptions.Singleline);

        public IList<Shape> Elements
        {
            get
            {
                return this.m_elements.AsReadOnly();
            }
        }

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
            if (this.Id.Length > 0) svg.AddShape(this.Id, this);
        }

        public static Shape AddToList(SVG svg, List<Shape> list, XmlNode childnode, Shape parent)
        {
            if (childnode.NodeType != XmlNodeType.Element) return null;

            if (childnode.Name == SVGTags.sShapeRect)
            {
                list.Add(new RectangleShape(svg, childnode));
                return list[list.Count - 1];
            }
            if (childnode.Name == SVGTags.sShapeCircle)
            {
                list.Add(new CircleShape(svg, childnode));
                return list[list.Count - 1];
            }
            if (childnode.Name == SVGTags.sShapeEllipse)
            {
                list.Add(new EllipseShape(svg, childnode));
                return list[list.Count - 1];
            }
            if (childnode.Name == SVGTags.sShapeLine)
            {
                list.Add(new LineShape(svg, childnode));
                return list[list.Count - 1];
            }
            if (childnode.Name == SVGTags.sShapePolyline)
            {
                list.Add(new PolylineShape(svg, childnode));
                return list[list.Count - 1];
            }
            if (childnode.Name == SVGTags.sShapePolygon)
            {
                list.Add(new PolygonShape(svg, childnode));
                return list[list.Count - 1];
            }
            if (childnode.Name == SVGTags.sShapePath)
            {
                list.Add(new PathShape(svg, childnode));
                return list[list.Count - 1];
            }
            if (childnode.Name == SVGTags.sClipPath)
            {
                list.Add(new Clip(svg, childnode, parent));
                return list[list.Count - 1];
            }
            if (childnode.Name == SVGTags.sShapeGroup || childnode.Name == SVGTags.sSwitch)
            {
                list.Add(new Group(svg, childnode, parent));
                return list[list.Count - 1];
            }
            if (childnode.Name == SVGTags.sLinearGradient)
            {
                svg.PaintServers.Create(childnode);
                return null;
            }
            if (childnode.Name == SVGTags.sRadialGradient)
            {
                svg.PaintServers.Create(childnode);
                return null;
            }
            if (childnode.Name == SVGTags.sDefinitions)
            {
                ReadDefs(svg, list, childnode);
                return null;
            }
            if (childnode.Name == SVGTags.sShapeUse)
            {
                list.Add(new UseShape(svg, childnode));
                return list[list.Count - 1];
            }
            if (childnode.Name == SVGTags.sShapeImage)
            {
                list.Add(new ImageShape(svg, childnode));
                return list[list.Count - 1];
            }
            if (childnode.Name == SVGTags.sAnimateTransform)
            {
                list.Add(new AnimateTransform(svg, childnode));
                return list[list.Count - 1];
            }
            if (childnode.Name == SVGTags.sText)
            {
                list.Add(new TextShape(svg, childnode, parent));
                return list[list.Count - 1];
            }
            if (childnode.Name == SVGTags.sStyle)
            {
                var match = _regexStyle.Match(childnode.InnerText);
                while (match.Success)
                {
                    var name = match.Groups[1].Value.Trim();
                    var value = match.Groups[2].Value;
                    svg.m_styles.Add(name, new List<XmlAttribute>());
                    foreach (ShapeUtil.Attribute styleitem in XmlUtil.SplitStyle(svg, value))
                    {
                        svg.m_styles[name].Add(new XmlUtil.StyleItem(childnode, styleitem.Name, styleitem.Value));
                    }
                    match = match.NextMatch();
                }
            }
            return null;
        }

        private static void ReadDefs(SVG svg, List<Shape> list, XmlNode node)
        {
            list = new List<Shape>(); // temp list, not needed. 
            //ShapeGroups defined in the 'def' section is added the the 'Shapes' dictionary in SVG for later reference
            foreach (XmlNode childnode in node.ChildNodes)
            {
                if (childnode.Name == SVGTags.sLinearGradient)
                {
                    svg.PaintServers.Create(childnode);
                    continue;
                }
                if (childnode.Name == SVGTags.sRadialGradient)
                {
                    svg.PaintServers.Create(childnode);
                    continue;
                }
                Group.AddToList(svg, list, childnode, null);
            }
        }
    }
}
