using System;
using System.Collections.Generic;
using System.Xml;

namespace SVGImage.SVG
{
    using Utils;
    using Shapes;
    using System.Linq;

    public sealed class TextShape : Shape
    {
        private static Fill DefaultFill = null;
        private static Stroke DefaultStroke = null;

        public TextShape(SVG svg, XmlNode node, Shape parent) 
            : base(svg, node, parent)
        {
            this.X = XmlUtil.AttrValue(node, "x", 0);
            this.Y = XmlUtil.AttrValue(node, "y", 0);
            this.Text = node.InnerText;
            this.GetTextStyle(svg);
            // check for tSpan tag
            if (node.InnerXml.IndexOf("<") >= 0)
                this.TextSpan = this.ParseTSpan(svg, node.InnerXml);
            if (DefaultFill == null)
            {
                DefaultFill = Fill.CreateDefault(svg, "black");
            }
            if (DefaultStroke == null)
            {
                DefaultStroke = Stroke.CreateDefault(svg, 0.1);
            }
        }

        public double X { get; set; }
        public double Y { get; set; }
        public string Text { get; set; }
        public TextSpan TextSpan {get; private set;}

        public override Fill Fill 
        { 
            get 
            {
                Fill f = base.Fill;
                if (f == null)
                    f = DefaultFill;
                return f;
            }
        }

        public override Stroke Stroke
        { 
            get 
            {
                Stroke f = base.Stroke;
                if (f == null)
                    f = DefaultStroke;
                return f;
            }
        }

        TextSpan ParseTSpan(SVG svg, string tspanText)
        {
            try
            {
                return TextSpan.Parse(svg, tspanText, this);
            }
            catch
            {
                return null;
            }
        }
    }

    public class TextSpan : Shape
    {
        public enum eElementType
        {
            Tag,
            Text,
        }

        public override System.Windows.Media.Transform Transform 
        { 
            get {return this.Parent.Transform; }
        }
        public eElementType ElementType {get; private set;}
        public List<StyleItem> Attributes {get; set;}
        public List<TextSpan> Children {get; private set;}
        public int StartIndex {get; set;}
        public string Text {get; set;}
        public TextSpan End {get; set;}
        public double? X { get; set; }
        public double? Y { get; set; }
        public double? DX { get; set; }
        public double? DY { get; set; }

        public TextSpan(SVG svg, Shape parent, string text) : base(svg, (XmlNode)null, parent)
        {
            this.ElementType = eElementType.Text;
            this.Text = text;
        }
        public TextSpan(SVG svg, Shape parent, eElementType eType, List<StyleItem> attrs) 
            : base(svg, attrs, parent)
        {
            this.ElementType = eType;
            this.Text = string.Empty;
            this.Children = new List<TextSpan>();

            if (!(attrs is null))
            {
                foreach (var attr in attrs)
                {
                    switch (attr.Name)
                    {
                        case "x": this.X = Double.Parse(attr.Value); break;
                        case "y": this.Y = Double.Parse(attr.Value); break;
                        case "dx": this.DX = Double.Parse(attr.Value); break;
                        case "dy": this.DY = Double.Parse(attr.Value); break;
                    }
                }
            }
        }
        public override string ToString()
        {
            return this.Text;
        }

        static TextSpan NextTag(SVG svg, TextSpan parent, string text, ref int curPos)
        {
            int start = text.IndexOf("<", curPos);
            if (start < 0)
                return null;
            int end = text.IndexOf(">", start+1);
            if (end < 0)
                throw new Exception("Start '<' with no end '>'");

            end++;

            string tagtext = text.Substring(start, end - start);
            if (tagtext.IndexOf("<", 1) > 0)
                throw new Exception(string.Format("Start '<' within tag 'tag'"));

            List<StyleItem> attrs = new List<StyleItem>();
            int attrstart = tagtext.IndexOf("tspan");
            if (attrstart > 0)
            {
                attrstart += 5;
                while (attrstart < tagtext.Length-1)
                    attrs.Add(StyleItem.ReadNextAttr(tagtext, ref attrstart));
            }
    
            TextSpan tag = new TextSpan(svg, parent, eElementType.Tag, attrs);
            tag.StartIndex = start;
            tag.Text = text.Substring(start, end - start);
            if (tag.Text.IndexOf("<", 1) > 0)
                throw new Exception(string.Format("Start '<' within tag 'tag'"));

            curPos = end;
            return tag;
        }

        static TextSpan Parse(SVG svg, string text, ref int curPos, TextSpan parent, TextSpan curTag)
        {
            TextSpan tag = curTag;
            if (tag == null)
                tag = NextTag(svg, parent, text, ref curPos);
            while (curPos < text.Length)
            {
                int prevPos = curPos;
                TextSpan next = NextTag(svg, tag, text, ref curPos);
                if (next == null && curPos < text.Length)
                {
                    // remaining pure text 
                    string s = text.Substring(curPos, text.Length - curPos);
                    tag.Children.Add(new TextSpan(svg, tag, s));
                    return tag;
                }
                if (next != null && next.StartIndex-prevPos > 0)
                {
                    // pure text between tspan elements
                    int diff = next.StartIndex-prevPos;
                    string s = text.Substring(prevPos, diff);
                    tag.Children.Add(new TextSpan(svg, tag, s));
                }
                if (next.Text.StartsWith("<tspan"))
                {
                    // new nested element
                    next = Parse(svg, text, ref curPos, tag, next);
                    tag.Children.Add(next);
                    continue;
                }
                if (next.Text.StartsWith("</tspan"))
                {
                    // end of cur element
                    tag.End = next;
                    return tag;
                }
                if (next.Text.StartsWith("<textPath"))
                {
                    continue;
                }
                if (next.Text.StartsWith("</textPath"))
                {
                    continue;
                }
                throw new Exception(string.Format("unexpected tag '{0}'", next.Text));
            }
            return tag;
        }

        public static TextSpan Parse(SVG svg, string text, TextShape owner)
        {
            int curpos = 0;
            TextSpan root = new TextSpan(svg, owner, eElementType.Tag, null);
            root.Text = "<root>";
            root.StartIndex = 0;
            return Parse(svg, text, ref curpos, null, root);
        }

        public static void Print(TextSpan tag, string indent)
        {
            if (tag.ElementType == eElementType.Text)
                Console.WriteLine("{0} '{1}'", indent, tag.Text);
            indent += "   ";
            foreach (TextSpan c in tag.Children)
                Print(c, indent);
        }
    }

}
