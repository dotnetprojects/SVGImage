using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
using DotNetProjects.SVGImage.SVG.FileLoaders;
using SVGImage.SVG.PaintServer;
using SVGImage.SVG.Shapes;
using Group = SVGImage.SVG.Shapes.Group;

namespace SVGImage.SVG
{
    public class SVG
    {
        internal Dictionary<string, Shape> m_shapes = new Dictionary<string, Shape>();

        internal Dictionary<string, List<XmlUtil.StyleItem>> m_styles = new Dictionary<string, List<XmlUtil.StyleItem>>();

        private List<Shape> m_elements;

        public string Filename { get; private set; }

        public Rect? ViewBox { get; set; }

        public Size Size { get; set; }

        public IExternalFileLoader ExternalFileLoader { get; set; }

        public void AddShape(string id, Shape shape)
        {
            //System.Diagnostics.Debug.Assert(id.Length > 0 && this.m_shapes.ContainsKey(id) == false);
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

        public SVG(string filename) : this(filename, FileSystemLoader.Instance)
        {
        }

        public SVG(string filename, IExternalFileLoader externalFileLoader)
        {
            this.ExternalFileLoader = externalFileLoader;
            Size = new Size(300, 150);
            this.Filename = filename;
            XmlDocument doc = new XmlDocument();
            doc.XmlResolver = null;
            doc.Load(filename);
            LoadExternalStyles(doc);
            XmlNode n = doc.GetElementsByTagName("svg")[0];
            this.m_elements = Parse(this, n);
        }

        public SVG(Stream stream) : this(stream, FileSystemLoader.Instance)
        {
        }

        public SVG(Stream stream, IExternalFileLoader externalFileLoader)
        {
            this.ExternalFileLoader = externalFileLoader;
            Size = new Size(300, 150);
            this.Filename = "none";
            XmlDocument doc = new XmlDocument();
            doc.XmlResolver = null;
            doc.Load(stream);
            LoadExternalStyles(doc);
            XmlNode n = doc.GetElementsByTagName("svg")[0];
            this.m_elements = Parse(this, n);
        }

        public IList<Shape> Elements => this.m_elements.AsReadOnly();

        private void LoadExternalStyles(XmlDocument doc)
        {
            if (ExternalFileLoader != null)
            {
                var cssUrlNodes = (from XmlNode childNode in doc.ChildNodes
                    where childNode.NodeType == XmlNodeType.ProcessingInstruction && childNode.Name == "xml-stylesheet"
                    select (XmlProcessingInstruction) childNode).ToList();

                if (cssUrlNodes.Count > 0)
                {
                    foreach (var node in cssUrlNodes)
                    {
                        var url = Regex.Match(node.Data, "href=\"(?<url>.*?)\"").Groups["url"].Value;
                        var stream = ExternalFileLoader.LoadFile(url, this.Filename);
                        if (stream != null)
                        {
                            using (stream)
                            {
                                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                                {
                                    var style = reader.ReadToEnd();
                                    StyleParser.ParseStyle(this, style);
                                }
                            }
                        }
                    }
                }
            }
        }

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
