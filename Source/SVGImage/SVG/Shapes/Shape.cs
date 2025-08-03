using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Xml;
using System.Windows;

namespace SVGImage.SVG.Shapes
{
    using Utils;
    using Filters;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows.Shapes;
    using System.Windows.Markup;
    using System.Diagnostics;
    using System.Text;
    using System.Collections;
    using System.Reflection;

    public class Shape : ClipArtElement
    {
        private static readonly Regex _whiteSpaceRegex = new Regex(@"\s+");
        protected Fill m_fill;

        protected Stroke m_stroke;

        protected TextStyle m_textstyle;

        protected string m_localStyle;

        internal Clip m_clip = null;

        internal Geometry geometryElement;
        private readonly SVG _svg;
        public SVG Svg => _svg;

        public bool Display { get; private set; } = true;

        //Used during render
        internal Shape RealParent;
        private double m_opacity;

        public Shape(SVG svg, XmlNode node) : this(svg, node, null)
        {
        }

        public Shape GetRoot()
        {
            Shape root = this;
            while (root.Parent != null)
            {
                root = root.Parent;
            }
            return root;
        }
        

        public Shape(SVG svg, XmlNode node, Shape parent) : base(node)
        {
            _svg = svg;
            this.Opacity = 1;
            this.Parent = parent;
            this.ParseAtStart(svg, node);
            if (node != null)
            {
                _ = GetTextStyle(svg); // Ensure TextStyle is initialized
                foreach (XmlAttribute attr in node.Attributes)
                {
                    this.Parse(svg, attr.Name, attr.Value);
                }
            }
            ParseLocalStyle(svg);
        }

        public Shape(SVG svg, List<StyleItem> attrs, Shape parent) : base(null)
        {
            _svg = svg;
            this.Opacity = 1;
            this.Parent = parent;
            if (attrs != null)
            {
                foreach (StyleItem attr in attrs)
                {
                    this.Parse(svg, attr.Name, attr.Value);
                }
            }
            ParseLocalStyle(svg);
        }

        internal Clip Clip
        {
            get
            {
                return this.m_clip;
            }
        }

        public virtual string PaintServerKey { get; set; }

        public string RequiredExtensions { get; set; }

        public string RequiredFeatures { get; set; }

        public Visibility Visibility { get; set; }

        protected virtual Stroke DefaultStroke()
        {
            var parent = this.Parent;
            while (parent != null)
            {
                if (parent.Stroke != null)
                {
                    return parent.Stroke;
                }

                parent = parent.Parent;
            }
            return null;
        }

        public Stroke Stroke
        {
            get => m_stroke ?? DefaultStroke();
            set => m_stroke = value;
        }

        protected virtual Fill DefaultFill()
        {
            return null;
        }

        protected virtual Fill GetParentFill()
        {
            var parent = this.Parent;
            while (parent != null)
            {
                if (parent.Fill != null)
                {
                    return parent.Fill;
                }

                parent = parent.Parent;
            }
            return null;
        }

        public Fill Fill
        {
            get => m_fill ?? GetParentFill() ?? DefaultFill();
            set => m_fill = value;
        }

        public virtual TextStyle TextStyle
        {
            get
            {
                if (this.m_textstyle != null)
                {
                    return this.m_textstyle;
                }

                var parent = this.Parent;
                while (parent != null)
                {
                    if (parent.m_textstyle != null)
                    {
                        return parent.m_textstyle;
                    }

                    parent = parent.Parent;
                }
                return null;
            }
        }

        public double Opacity
        {
            get => Visibility == Visibility.Visible ? m_opacity : 0;
            set => m_opacity = value;
        }

        public virtual Transform Transform { get; private set; }

        public Shape Parent { get; set; }

        internal virtual Filter Filter { get; private set; }

        protected virtual void ParseAtStart(SVG svg, XmlNode node)
        {
            if (node != null)
            {
                var name = node.Name;
                if (name.Contains(':'))
                {
                    name = name.Split(':')[1];
                }

                if (svg.m_styles.TryGetValue(name, out var attributes))
                {
                    foreach (var xmlAttribute in attributes)
                    {
                        Parse(svg, xmlAttribute.Name, xmlAttribute.Value);
                    }
                }

                if (!string.IsNullOrEmpty(this.Id) && svg.m_styles.TryGetValue("#" + this.Id, out attributes))
                {
                    foreach (var xmlAttribute in attributes)
                    {
                        Parse(svg, xmlAttribute.Name, xmlAttribute.Value);
                    }
                }
            }
        }

        protected virtual void ParseLocalStyle(SVG svg)
        {
            if (!string.IsNullOrEmpty(this.m_localStyle))
            {
                foreach (StyleItem item in StyleParser.SplitStyle(svg, this.m_localStyle))
                {
                    this.Parse(svg, item.Name, item.Value);
                }
            }
        }

        protected virtual void Parse(SVG svg, string name, string value)
        {
            if (name.Contains(':'))
            {
                name = name.Split(':')[1];
            }

            if (name == SVGTags.sDisplay && value == "none")
            {
                this.Display = false;
            }

            if (name == SVGTags.sClass)
            {
                var classes = value.Split(' ');
                foreach (var @class in classes)
                {
                    if (svg.m_styles.TryGetValue("." + @class, out var attributes))
                    {
                        foreach (var xmlAttribute in attributes)
                        {
                            Parse(svg, xmlAttribute.Name, xmlAttribute.Value);
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
            if (name == SVGTags.sVisibility)
            {
                this.Visibility = value == "hidden" ? Visibility.Hidden : Visibility.Visible;
            }
            if (name == SVGTags.sStroke)
            {
                this.GetStroke(svg).PaintServerKey = svg.PaintServers.Parse(value);
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
                StringSplitter sp = new StringSplitter(value);
                List<double> a = new List<double>();
                while (sp.More)
                {
                    a.Add(sp.ReadNextValue());
                }
                this.GetStroke(svg).StrokeArray = a.ToArray();
                return;
            }
            if (name == SVGTags.sRequiredFeatures)
            {
                this.RequiredFeatures = value.Trim();
                return;
            }
            if (name == SVGTags.sRequiredExtensions)
            {
                this.RequiredExtensions = value.Trim();
                return;
            }
            if (name == SVGTags.sStrokeLinecap)
            {
                if (Enum.TryParse<Stroke.eLineCap>(value, true, out var parsed))
                {
                    this.GetStroke(svg).LineCap = parsed;
                }

                return;
            }
            if (name == SVGTags.sStrokeLinejoin)
            {
                if (Enum.TryParse<Stroke.eLineJoin>(value, true, out var parsed))
                {
                    this.GetStroke(svg).LineJoin = parsed;
                }

                return;
            }
            if (name == SVGTags.sFilterProperty)
            {
                if (value.StartsWith("url"))
                {
                    Shape result;
                    string id = ShapeUtil.ExtractBetween(value, '(', ')');
                    if (id.Length > 0 && id[0] == '#')
                    {
                        id = id.Substring(1);
                    }

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
                    if (id.Length > 0 && id[0] == '#')
                    {
                        id = id.Substring(1);
                    }

                    svg.m_shapes.TryGetValue(id, out result);
                    this.m_clip = result as Clip;
                    return;
                }
                return;
            }
            if (name == SVGTags.sFill)
            {
                this.GetFill(svg).PaintServerKey = svg.PaintServers.Parse(value);
                return;
            }
            if (name == SVGTags.sColor)
            {
                this.PaintServerKey = svg.PaintServers.Parse(value);
                return;
            }
            if (name == SVGTags.sFillOpacity)
            {
                this.GetFill(svg).Opacity = XmlUtil.ParseDouble(svg, value) * 100;
                return;
            }
            if (name == SVGTags.sFillRule)
            {
                if (Enum.TryParse<Fill.eFillRule>(value, true, out var parsed))
                {
                    this.GetFill(svg).FillRule = parsed;
                }

                return;
            }
            if (name == SVGTags.sOpacity)
            {
                this.Opacity = XmlUtil.ParseDouble(svg, value);
                return;
            }
            if (name == SVGTags.sStyle)
            {
                m_localStyle = value;
            }
            //********************** text *******************
            if (name == SVGTags.sFontFamily)
            {
                this.GetTextStyle(svg).FontFamily = value;
                return;
            }
            if (name == SVGTags.sFontSize)
            {
                this.GetTextStyle(svg).FontSize = XmlUtil.AttrValue(new StyleItem(name, value));
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
                if (value == "none")
                {
                    return;
                }
                var textDecorations = _whiteSpaceRegex.Split(value);
                TextDecorationCollection tt = new TextDecorationCollection();
                if (textDecorations.Length == 0 || textDecorations.Any(td => td.Equals("none", StringComparison.OrdinalIgnoreCase)))
                {
                    // If "none" is explicitly set, set TextDecoration to empty collection.
                    // This distinguishes it from not setting TextDecoration at all, in which case it defaults to inherit.
                    this.GetTextStyle(svg).TextDecoration = tt;
                    return;
                }
                foreach (var textDecoration in textDecorations)
                {
                    TextDecoration t = new TextDecoration();
                    if (value == "underline")
                    {
                        t.Location = TextDecorationLocation.Underline;
                    }
                    else if (value == "overline")
                    {
                        t.Location = TextDecorationLocation.OverLine;
                    }
                    else if (value == "line-through")
                    {
                        t.Location = TextDecorationLocation.Strikethrough;
                    }
                    tt.Add(t);
                }

                this.GetTextStyle(svg).TextDecoration = tt;

                return;
            }
            if (name == SVGTags.sTextAnchor)
            {
                if (value == "start")
                {
                    this.GetTextStyle(svg).TextAlignment = TextAlignment.Left;
                }

                if (value == "middle")
                {
                    this.GetTextStyle(svg).TextAlignment = TextAlignment.Center;
                }

                if (value == "end")
                {
                    this.GetTextStyle(svg).TextAlignment = TextAlignment.Right;
                }

                return;
            }
            if (name == "word-spacing")
            {
                this.GetTextStyle(svg).WordSpacing = XmlUtil.AttrValue(new StyleItem(name, value));
                return;
            }
            if (name == "letter-spacing")
            {
                this.GetTextStyle(svg).LetterSpacing = XmlUtil.AttrValue(new StyleItem(name, value));
                return;
            }
            if (name == "baseline-shift")
            {
                //GetTextStyle(svg).BaseLineShift = XmlUtil.AttrValue(new Attribute(name, value));
                this.GetTextStyle(svg).BaseLineShift = value;
                return;
            }

            //Debug.WriteLine("Unsupported: "+ name);
        }

        private Stroke GetStroke(SVG svg)
        {
            if (this.m_stroke == null)
            {
                this.m_stroke = new Stroke(svg);
            }

            return this.m_stroke;
        }

        protected Fill GetFill(SVG svg)
        {
            if (this.m_fill == null)
            {
                this.m_fill = new Fill(svg);
            }

            return this.m_fill;
        }

        protected TextStyle GetTextStyle(SVG svg)
        {
            if (this.m_textstyle == null)
            {
                this.m_textstyle = new TextStyle( this);
            }

            return this.m_textstyle;
        }

        public override string ToString()
        {
            return this.GetType().Name + " (" + (Id ?? "") + ")";
        }
    }




}
