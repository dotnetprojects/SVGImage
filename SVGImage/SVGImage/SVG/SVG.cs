using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml;

using SVGImage.SVG.PaintServer;
using SVGImage.SVG.Shapes;

namespace SVGImage.SVG
{
    public class SVG
    {
        internal Dictionary<string, Shape> m_shapes = new Dictionary<string, Shape>();

        internal Dictionary<string, List<XmlAttribute>> m_styles = new Dictionary<string, List<XmlAttribute>>();

        private List<Shape> m_elements;

        public string Filename { get; private set; }

        public Rect? ViewBox { get; set; }

        public Size Size { get; set; }

        public void AddShape(string id, Shape shape)
        {
            System.Diagnostics.Debug.Assert(id.Length > 0 && this.m_shapes.ContainsKey(id) == false);
            this.m_shapes[id] = shape;
        }

        public Shape GetShape(string id)
        {
            Shape shape = null;
            this.m_shapes.TryGetValue(id, out shape);
            return shape;
        }

        public PaintServerManager PaintServers { get; } = new PaintServerManager();

        public SVG()
        {
            Size = new Size(300, 150);
        }

        public SVG(string filename)
        {
            Size = new Size(300, 150);
            this.Filename = filename;
            XmlDocument doc = new XmlDocument();
            doc.XmlResolver = null;
            doc.Load(filename);
            XmlNode n = doc.GetElementsByTagName("svg")[0];
            this.m_elements = Parse(this, n);
        }

        public SVG(Stream stream)
        {
            Size = new Size(300, 150);
            this.Filename = "none";
            XmlDocument doc = new XmlDocument();
            doc.XmlResolver = null;
            doc.Load(stream);
            XmlNode n = doc.GetElementsByTagName("svg")[0];

            this.m_elements = Parse(this, n);
        }

        public IList<Shape> Elements => this.m_elements.AsReadOnly();

        internal static List<Shape> Parse(SVG svg, XmlNode node)
        {
            var vb = node.Attributes.GetNamedItem("viewBox");
            if (vb != null)
            {
                var cord = vb.Value.Split(' ');
                var cult = CultureInfo.InvariantCulture;
                svg.ViewBox = new Rect(double.Parse(cord[0], cult), double.Parse(cord[1], cult), double.Parse(cord[2], cult), double.Parse(cord[3], cult));
            }

            svg.Size = new Size(XmlUtil.AttrValue(node, "width", 300), XmlUtil.AttrValue(node, "height", 150));
            
            var lstElements = new List<Shape>();
            if (node == null || (node.Name != SVGTags.sSvg && node.Name != SVGTags.sPattern))
                throw new FormatException("Not a valide SVG node");
            foreach (XmlNode childnode in node.ChildNodes)
                Group.AddToList(svg, lstElements, childnode, null);
            return lstElements;
        }
    }
}
