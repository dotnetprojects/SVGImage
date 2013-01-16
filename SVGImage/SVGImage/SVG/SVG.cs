using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;

using SVGImage.SVG.PaintServer;
using SVGImage.SVG.Shapes;

namespace SVGImage.SVG
{
    public class SVG
    {
        private PaintServerManager m_paintServers = new PaintServerManager();

        internal Dictionary<string, Shape> m_shapes = new Dictionary<string, Shape>();

        private List<Shape> m_elements = new List<Shape>();

        public string Filename { get; private set; }

        public Rect? ViewBox { get; set; }

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

        public PaintServerManager PaintServers
        {
            get
            {
                return this.m_paintServers;
            }
        }

        public SVG()
        {

        }

        public SVG(string filename)
        {
            this.Filename = filename;
            XmlDocument doc = new XmlDocument();
            doc.XmlResolver = null;
            doc.Load(filename);
            XmlNode n = doc.GetElementsByTagName("svg")[0];
            this.Parse(n);
        }

        public SVG(Stream stream)
        {
            this.Filename = "none";
            XmlDocument doc = new XmlDocument();
            doc.XmlResolver = null;
            doc.Load(stream);
            XmlNode n = doc.GetElementsByTagName("svg")[0];

            var vb = n.Attributes.GetNamedItem("viewBox");
            if (vb != null)
            {
                var cord = vb.Value.Split(' ');
                var cult = CultureInfo.InvariantCulture;
                this.ViewBox = new Rect(double.Parse(cord[0], cult), double.Parse(cord[1], cult), double.Parse(cord[2], cult), double.Parse(cord[3], cult));
            }
            this.Parse(n);
        }

        public IList<Shape> Elements
        {
            get
            {
                return this.m_elements.AsReadOnly();
            }
        }

        private void Parse(XmlNode node)
        {
            if (node == null || node.Name != "svg") throw new FormatException("Not a valide SVG node");
            foreach (XmlNode childnode in node.ChildNodes) Group.AddToList(this, this.m_elements, childnode, null);
        }
    }
}
