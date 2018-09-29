using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Xml;
using System.Windows;
using DotNetProjects.SVGImage.SVG.Shapes.Filter;

namespace SVGImage.SVG.Shapes
{
    public class Shape : ClipArtElement
    {
        private Fill m_fill;

        private Stroke m_stroke;

        private TextStyle m_textstyle;

        internal Clip m_clip = null;

        internal Clip Clip
        {
            get
            {
                return this.m_clip;
            }
        }

        public virtual Stroke Stroke
        {
            get
            {
                if (this.m_stroke != null) return this.m_stroke;
                var parent = this.Parent;
                while (parent != null)
                {
                    if (this.Parent.Stroke != null) return parent.Stroke;
                    parent = parent.Parent;
                }
                return null;
            }
        }

        public virtual Fill Fill
        {
            get
            {
                if (this.m_fill != null) return this.m_fill;
                var parent = this.Parent;
                while (parent != null)
                {
                    if (parent.Fill != null) return parent.Fill;
                    parent = parent.Parent;
                }
                return null;
            }
        }

        public virtual TextStyle TextStyle
        {
            get
            {
                if (this.m_textstyle != null) return this.m_textstyle;
                var parent = this.Parent;
                while (parent != null)
                {
                    if (parent.m_textstyle != null) return parent.m_textstyle;
                    parent = parent.Parent;
                }
                return null;
            }
        }

        public double Opacity { get; set; }

        public virtual Transform Transform { get; private set; }

        internal virtual Filter Filter { get; private set; }

        public Shape Parent { get; set; }

        public Shape(SVG svg, XmlNode node)
            : this(svg, node, null)
        {
        }

        public Shape(SVG svg, XmlNode node, Shape parent)
            : base(node)
        {
            this.Opacity = 1;
            this.Parent = parent;
            this.ParseAtStart(svg, node);
            if (node != null)
            {
                foreach (XmlAttribute attr in node.Attributes)
                    this.Parse(svg, attr);
            }
        }

        public Shape(SVG svg, List<ShapeUtil.Attribute> attrs, Shape parent)
            : base(null)
        {
            this.Opacity = 1;
            this.Parent = parent;
            if (attrs != null)
            {
                foreach (ShapeUtil.Attribute attr in attrs)
                    this.Parse(svg, attr);
            }
        }

        protected virtual void Parse(SVG svg, XmlAttribute attr)
        {
            string name = attr.Name;
            string value = attr.Value;
            this.Parse(svg, name, value);
        }

        protected virtual void Parse(SVG svg, ShapeUtil.Attribute attr)
        {
            string name = attr.Name;
            string value = attr.Value;
            this.Parse(svg, name, value);
        }

        protected virtual void ParseAtStart(SVG svg, XmlNode node)
        {
            if (node != null)
            {
                List<XmlAttribute> attributes;
                if (svg.m_styles.TryGetValue(node.Name, out attributes))
                {
                    foreach (var xmlAttribute in attributes)
                    {
                        Parse(svg, xmlAttribute);
                    }
                }
            }
        }

        protected virtual void Parse(SVG svg, string name, string value)
        {
            if (name == SVGTags.sClass)
            {
                var classes = value.Split(' ');
                foreach (var @class in classes)
                {
                    List<XmlAttribute> attributes;
                    if (svg.m_styles.TryGetValue("." + @class, out attributes))
                    {
                        foreach (var xmlAttribute in attributes)
                        {
                            Parse(svg, xmlAttribute);
                        }
                    }
                }
                return;
            }
            if (name == SVGTags.sTransform)
            {
                this.Transform = ShapeUtil.ParseTransform(value.ToLower());
                return;
            }
            if (name == SVGTags.sStroke)
            {
                this.GetStroke(svg).Color = svg.PaintServers.Parse(value);
                return;
            }
            if (name == SVGTags.sStrokeWidth)
            {
                this.GetStroke(svg).Width = XmlUtil.ParseDouble(svg, value);
                return;
            }
            if (name == SVGTags.sStrokeOpacity)
            {
                this.GetStroke(svg).Opacity = XmlUtil.ParseDouble(svg, value) * 100;
                return;
            }
            if (name == SVGTags.sStrokeDashArray)
            {
                if (value == "none")
                {
                    this.GetStroke(svg).StrokeArray = null;
                    return;
                }
                ShapeUtil.StringSplitter sp = new ShapeUtil.StringSplitter(value);
                List<double> a = new List<double>();
                while (sp.More)
                {
                    a.Add(sp.ReadNextValue());
                }
                this.GetStroke(svg).StrokeArray = a.ToArray();
                return;
            }
            if (name == SVGTags.sStrokeLinecap)
            {
                this.GetStroke(svg).LineCap = (Stroke.eLineCap)Enum.Parse(typeof(Stroke.eLineCap), value);
                return;
            }
            if (name == SVGTags.sStrokeLinejoin)
            {
                this.GetStroke(svg).LineJoin = (Stroke.eLineJoin)Enum.Parse(typeof(Stroke.eLineJoin), value);
                return;
            }
            if (name == SVGTags.sFilterProperty)
            {
                if (value.StartsWith("url"))
                {
                    Shape result;
                    string id = ShapeUtil.ExtractBetween(value, '(', ')');
                    if (id.Length > 0 && id[0] == '#') id = id.Substring(1);
                    svg.m_shapes.TryGetValue(id, out result);
                    this.Filter = result as Filter;
                    return;
                }
                return;
            }
            if (name == SVGTags.sClipPathProperty)
            {
                if (value.StartsWith("url"))
                {
                    Shape result;
                    string id = ShapeUtil.ExtractBetween(value, '(', ')');
                    if (id.Length > 0 && id[0] == '#') id = id.Substring(1);
                    svg.m_shapes.TryGetValue(id, out result);
                    this.m_clip = result as Clip;
                    return;
                }
                return;
            }
            if (name == SVGTags.sFill)
            {
                this.GetFill(svg).Color = svg.PaintServers.Parse(value);
                return;
            }
            if (name == SVGTags.sFillOpacity)
            {
                this.GetFill(svg).Opacity = XmlUtil.ParseDouble(svg, value) * 100;
                return;
            }
            if (name == SVGTags.sFillRule)
            {
                this.GetFill(svg).FillRule = (Fill.eFillRule)Enum.Parse(typeof(Fill.eFillRule), value);
                return;
            }
            if (name == SVGTags.sOpacity)
            {
                this.Opacity = XmlUtil.ParseDouble(svg, value);
                return;
            }
            if (name == SVGTags.sStyle)
            {
                foreach (ShapeUtil.Attribute item in XmlUtil.SplitStyle(svg, value)) this.Parse(svg, item);
            }
            //********************** text *******************
            if (name == SVGTags.sFontFamily)
            {
                this.GetTextStyle(svg).FontFamily = value;
                return;
            }
            if (name == SVGTags.sFontSize)
            {
                this.GetTextStyle(svg).FontSize = XmlUtil.AttrValue(new ShapeUtil.Attribute(name, value));
                return;
            }
            if (name == SVGTags.sFontWeight)
            {
                this.GetTextStyle(svg).Fontweight = (FontWeight)new FontWeightConverter().ConvertFromString(value);
                return;
            }
            if (name == SVGTags.sFontStyle)
            {
                this.GetTextStyle(svg).Fontstyle = (FontStyle)new FontStyleConverter().ConvertFromString(value);
                return;
            }
            if (name == SVGTags.sTextDecoration)
            {
                TextDecoration t = new TextDecoration();
                if (value == "none") return;
                if (value == "underline") t.Location = TextDecorationLocation.Underline;
                if (value == "overline") t.Location = TextDecorationLocation.OverLine;
                if (value == "line-through") t.Location = TextDecorationLocation.Strikethrough;
                TextDecorationCollection tt = new TextDecorationCollection();
                tt.Add(t);
                this.GetTextStyle(svg).TextDecoration = tt;
                return;
            }
            if (name == SVGTags.sTextAnchor)
            {
                if (value == "start") this.GetTextStyle(svg).TextAlignment = TextAlignment.Left;
                if (value == "middle") this.GetTextStyle(svg).TextAlignment = TextAlignment.Center;
                if (value == "end") this.GetTextStyle(svg).TextAlignment = TextAlignment.Right;
                return;
            }
            if (name == "word-spacing")
            {
                this.GetTextStyle(svg).WordSpacing = XmlUtil.AttrValue(new ShapeUtil.Attribute(name, value));
                return;
            }
            if (name == "letter-spacing")
            {
                this.GetTextStyle(svg).LetterSpacing = XmlUtil.AttrValue(new ShapeUtil.Attribute(name, value));
                return;
            }
            if (name == "baseline-shift")
            {
                //GetTextStyle(svg).BaseLineShift = XmlUtil.AttrValue(new ShapeUtil.Attribute(name, value));
                this.GetTextStyle(svg).BaseLineShift = value;
                return;
            }

            //Debug.WriteLine("Unsupported: "+ name);
        }

        private Stroke GetStroke(SVG svg)
        {
            if (this.m_stroke == null) this.m_stroke = new Stroke(svg);
            return this.m_stroke;
        }

        protected Fill GetFill(SVG svg)
        {
            if (this.m_fill == null) this.m_fill = new Fill(svg);
            return this.m_fill;
        }

        protected TextStyle GetTextStyle(SVG svg)
        {
            if (this.m_textstyle == null) this.m_textstyle = new TextStyle(svg, this);
            return this.m_textstyle;
        }

        public override string ToString()
        {
            return this.GetType().Name + " (" + (Id ?? "") + ")";
        }
    }
}
