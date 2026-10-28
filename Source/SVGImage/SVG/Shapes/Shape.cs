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
        private string clipId;
        private FillRule? clipRule;
        internal FillRule ClipRule => clipRule ?? Parent?.ClipRule ?? FillRule.Nonzero;
        private string m_maskId;
        private string m_colorInterpolation;
        internal List<XmlNode> AnimationNodes { get; } = new List<XmlNode>();
        internal string CssAnimation { get; private set; }
        internal Dictionary<string, string> CssProperties { get; } = new Dictionary<string, string>();
        private XmlNode sourceNode;

        // Resolve at render time, after definitions later in the document have been read.
        internal Mask Mask => Svg.GetShape(m_maskId) as Mask;
        internal string ColorInterpolation => m_colorInterpolation ?? Parent?.ColorInterpolation ?? "sRGB";

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
            sourceNode = node;
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
            if (node != null)
                foreach (XmlNode child in node.ChildNodes)
                    if (child.LocalName == "animate" || child.LocalName == "animateTransform" || child.LocalName == "animateMotion" || child.LocalName == "set")
                    {
                        string href = Utils.XmlUtil.AttrValue(child, "href", Utils.XmlUtil.AttrValue(child, "xlink:href"));
                        if (href.StartsWith("#"))
                        {
                            string id = href.Substring(1);
                            if (!svg.ReferencedAnimations.ContainsKey(id)) svg.ReferencedAnimations[id] = new List<XmlNode>();
                            svg.ReferencedAnimations[id].Add(child);
                        }
                        else AnimationNodes.Add(child);
                    }
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
                return this.m_clip ?? Svg.GetShape(clipId) as Clip;
            }
        }

        private string paintServerKey;
        public virtual string PaintServerKey
        {
            get => paintServerKey ?? (RealParent ?? Parent)?.PaintServerKey;
            set => paintServerKey = value;
        }

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

        internal Fill InheritedFill => GetParentFill();

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
        internal string TransformText { get; private set; }

        public Shape Parent { get; set; }

        private string filterId;
        internal virtual Filter Filter => Svg.GetShape(filterId) as Filter;

        protected virtual void ParseAtStart(SVG svg, XmlNode node) { }

        protected virtual void ParseLocalStyle(SVG svg)
        {
            if (sourceNode != null)
            {
                foreach (var item in svg.StyleSheet.Resolve(sourceNode))
                    if (item.Key.StartsWith("--")) CssProperties[item.Key] = item.Value;
                    else this.Parse(svg, item.Key, item.Value);
            }
            else if (!string.IsNullOrEmpty(this.m_localStyle))
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

            if (name == "mask")
            {
                int start = value.IndexOf('(');
                int end = value.LastIndexOf(')');
                string reference = start >= 0 && end > start
                    ? value.Substring(start + 1, end - start - 1).Trim().Trim('\'', '"') : string.Empty;
                m_maskId = reference.StartsWith("#") ? reference.Substring(1) : null;
                return;
            }
            if (name == "animation")
            {
                CssProperties[name] = value;
                CssAnimation = value;
                return;
            }
            if (name.StartsWith("animation-") || name == "transform-origin" || name == "transform-box")
            {
                CssProperties[name] = value;
                return;
            }
            if (name == "color-interpolation")
            {
                m_colorInterpolation = value == "inherit" ? null : value;
                return;
            }

            if (name == SVGTags.sDisplay)
            {
                this.Display = value != "none";
            }

            if (name == SVGTags.sClass) return;
            if (name == SVGTags.sTransform)
            {
                TransformText = value;
                this.Transform = new MatrixTransform(CssTransforms.Parse(value).Matrix);
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
                filterId = null;
                if (value.StartsWith("url"))
                {
                    string id = ShapeUtil.ExtractBetween(value, '(', ')').Trim().Trim('\'', '"');
                    if (id.Length > 0 && id[0] == '#')
                    {
                        id = id.Substring(1);
                    }

                    filterId = id;
                    return;
                }
                return;
            }
            if (name == SVGTags.sClipPathProperty)
            {
                m_clip = null;
                clipId = null;
                if (value.StartsWith("url"))
                {
                    string id = ShapeUtil.ExtractBetween(value, '(', ')');
                    id = id.Trim().Trim('\'', '"');
                    if (id.Length > 0 && id[0] == '#')
                    {
                        id = id.Substring(1);
                    }

                    clipId = id;
                    return;
                }
                return;
            }
            if (name == "clip-rule") { clipRule = value == "evenodd" ? FillRule.EvenOdd : FillRule.Nonzero; return; }
            if (name == SVGTags.sFill)
            {
                this.GetFill(svg).PaintServerKey = svg.PaintServers.Parse(value);
                return;
            }
            if (name == SVGTags.sColor)
            {
                this.PaintServerKey = value == "currentColor" ? null : svg.PaintServers.Parse(value);
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
                this.m_stroke.PaintServerKey = svg.PaintServers.Parse("black");
            }

            return this.m_stroke;
        }

        protected Fill GetFill(SVG svg)
        {
            if (this.m_fill == null)
            {
                this.m_fill = new Fill(svg, this);
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
