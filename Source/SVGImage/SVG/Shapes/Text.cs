//using System;
//using System.Collections.Generic;
//using System.Xml;

//namespace SVGImage.SVG
//{
//    using Utils;
//    using Shapes;
//    using System.Linq;

//    public sealed class TextShape : Shape
//    {
//        private static Fill DefaultFill = null;
//        private static Stroke DefaultStroke = null;

//        public TextShape(SVG svg, XmlNode node, Shape parent) 
//            : base(svg, node, parent)
//        {
//            this.X = XmlUtil.AttrValue(node, "x", 0);
//            this.Y = XmlUtil.AttrValue(node, "y", 0);
//            this.Text = node.InnerText;
//            this.GetTextStyle(svg);
//            // check for tSpan tag
//            if (node.InnerXml.IndexOf("<") >= 0)
//                this.TextSpan = this.ParseTSpan(svg, node);
//            if (DefaultFill == null)
//            {
//                DefaultFill = Fill.CreateDefault(svg, "black");
//            }
//            if (DefaultStroke == null)
//            {
//                DefaultStroke = Stroke.CreateDefault(svg, 0.1);
//            }
//        }

//        public double X { get; set; }
//        public double Y { get; set; }
//        public string Text { get; set; }
//        public TextSpan2 TextSpan {get; private set;}

//        public override Fill Fill 
//        { 
//            get 
//            {
//                Fill f = base.Fill;
//                if (f == null)
//                    f = DefaultFill;
//                return f;
//            }
//        }

//        public override Stroke Stroke
//        { 
//            get 
//            {
//                Stroke f = base.Stroke;
//                if (f == null)
//                    f = DefaultStroke;
//                return f;
//            }
//        }

//        TextSpan2 ParseTSpan(SVG svg, XmlNode node)
//        {
//            try
//            {
//                return TextSpan2.Parse(svg, node, this);
//            }
//            catch
//            {
//                return null;
//            }
//        }
//    }

//    public class TextSpan2 : Shape
//    {
//        public enum eElementType
//        {
//            Tag,
//            Text,
//        }

//        public override System.Windows.Media.Transform Transform 
//        { 
//            get {return this.Parent.Transform; }
//        }
//        public eElementType ElementType {get; private set;}
//        public List<StyleItem> Attributes {get; set;}
//        public List<TextSpan2> Children {get; private set;}
//        public int StartIndex {get; set;}
//        public string Text {get; set;}
//        public TextSpan2 End {get; set;}
//        public List<double> XList { get; set; } = new List<double>();
//        public List<double> YList { get; set; } = new List<double>();
//        public List<double> DXList { get; set; } = new List<double>();
//        public List<double> DYList { get; set; } = new List<double>();

//        public TextSpan2(SVG svg, Shape parent, string text) : base(svg, (XmlNode)null, parent)
//        {
//            this.ElementType = eElementType.Text;
//            this.Text = text;
//        }
//        private List<double> ParseNumberList(string value)
//        {
//            return value.Split(new[] { ' ', ',', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(Double.Parse).ToList();
//        }
//        public TextSpan2(SVG svg, Shape parent, eElementType eType, List<StyleItem> attrs) 
//            : base(svg, attrs, parent)
//        {
//            this.ElementType = eType;
//            this.Text = string.Empty;
//            this.Children = new List<TextSpan2>();
//            if (!(attrs is null))
//            {
//                foreach (var attr in attrs)
//                {
//                    switch (attr.Name)
//                    {
//                        case "x":
//                            XList = ParseNumberList(attr.Value);
//                            break;
//                        case "y":
//                            YList = ParseNumberList(attr.Value);
//                            break;
//                        case "dx":
//                            DXList = ParseNumberList(attr.Value);
//                            break;
//                        case "dy":
//                            DYList = ParseNumberList(attr.Value);
//                            break;
//                    }
//                }
//            }
//        }
//        public override string ToString()
//        {
//            return this.Text;
//        }

//        static TextSpan2 NextTag(SVG svg, TextSpan2 parent, string text, ref int curPos)
//        {
//            int start = text.IndexOf("<", curPos);
//            if (start < 0)
//                return null;
//            int end = text.IndexOf(">", start+1);
//            if (end < 0)
//                throw new Exception("Start '<' with no end '>'");

//            end++;

//            string tagtext = text.Substring(start, end - start);
//            if (tagtext.IndexOf("<", 1) > 0)
//                throw new Exception(string.Format("Start '<' within tag 'tag'"));

//            List<StyleItem> attrs = new List<StyleItem>();
//            int attrstart = tagtext.IndexOf("tspan");
//            if (attrstart > 0)
//            {
//                attrstart += 5;
//                while (attrstart < tagtext.Length-1)
//                    attrs.Add(StyleItem.ReadNextAttr(tagtext, ref attrstart));
//            }
    
//            TextSpan2 tag = new TextSpan2(svg, parent, eElementType.Tag, attrs);
//            tag.StartIndex = start;
//            tag.Text = text.Substring(start, end - start);
//            if (tag.Text.IndexOf("<", 1) > 0)
//                throw new Exception(string.Format("Start '<' within tag 'tag'"));

//            curPos = end;
//            return tag;
//        }

//        static TextSpan2 Parse(SVG svg, string text, ref int curPos, TextSpan2 parent, TextSpan2 curTag)
//        {
//            TextSpan2 tag = curTag;
//            if (tag == null)
//                tag = NextTag(svg, parent, text, ref curPos);
//            while (curPos < text.Length)
//            {
//                int prevPos = curPos;
//                TextSpan2 next = NextTag(svg, tag, text, ref curPos);
//                if (next == null && curPos < text.Length)
//                {
//                    // remaining pure text 
//                    string s = text.Substring(curPos, text.Length - curPos);
//                    tag.Children.Add(new TextSpan2(svg, tag, s));
//                    return tag;
//                }
//                if (next != null && next.StartIndex-prevPos > 0)
//                {
//                    // pure text between tspan elements
//                    int diff = next.StartIndex-prevPos;
//                    string s = text.Substring(prevPos, diff);
//                    tag.Children.Add(new TextSpan2(svg, tag, s));
//                }
//                if (next.Text.StartsWith("<tspan"))
//                {
//                    // new nested element
//                    next = Parse(svg, text, ref curPos, tag, next);
//                    tag.Children.Add(next);
//                    continue;
//                }
//                if (next.Text.StartsWith("</tspan"))
//                {
//                    // end of cur element
//                    tag.End = next;
//                    return tag;
//                }
//                if (next.Text.StartsWith("<textPath"))
//                {
//                    continue;
//                }
//                if (next.Text.StartsWith("</textPath"))
//                {
//                    continue;
//                }
//                throw new Exception(string.Format("unexpected tag '{0}'", next.Text));
//            }
//            return tag;
//        }

//        public static TextSpan2 Parse(SVG svg, XmlNode node, TextShape owner)
//        {
//            var rootSpan = new TextSpan2(svg, owner, eElementType.Tag, null)
//            {
//                Text = "<root>",
//                StartIndex = 0,
//                Children = new List<TextSpan2>()
//            };

//            foreach (XmlNode child in node.ChildNodes)
//            {
//                ParseXmlNode(svg, rootSpan, child);
//            }

//            return rootSpan;
//        }

//        private static void ParseXmlNode(SVG svg, TextSpan2 parent, XmlNode node)
//        {
//            if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
//            {
//                parent.Children.Add(new TextSpan2(svg, parent, node.InnerText));
//            }
//            else if (node.NodeType == XmlNodeType.Element && node.Name == "tspan")
//            {
//                var attrs = new List<StyleItem>();
//                foreach (XmlAttribute attr in node.Attributes)
//                {
//                    attrs.Add(new StyleItem(attr.Name, attr.Value));
//                }

//                var tspan = new TextSpan2(svg, parent, eElementType.Tag, attrs)
//                {
//                    Text = node.OuterXml,
//                    StartIndex = 0
//                };

//                foreach (XmlNode inner in node.ChildNodes)
//                {
//                    ParseXmlNode(svg, tspan, inner);
//                }

//                parent.Children.Add(tspan);
//            }
//            // optionally handle other text-related tags like <textPath> here
//        }


//        public static void Print(TextSpan2 tag, string indent)
//        {
//            if (tag.ElementType == eElementType.Text)
//                Console.WriteLine("{0} '{1}'", indent, tag.Text);
//            indent += "   ";
//            foreach (TextSpan2 c in tag.Children)
//                Print(c, indent);
//        }
//    }

//}
