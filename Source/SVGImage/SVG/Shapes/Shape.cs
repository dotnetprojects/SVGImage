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

    public static class DpiUtil
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "Workaround")]
        static DpiUtil()
        {
            try
            {
                var sysPara = typeof(SystemParameters);
                var dpiXProperty = sysPara.GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static);
                var dpiYProperty = sysPara.GetProperty("Dpi", BindingFlags.NonPublic | BindingFlags.Static);
                DpiX = (int)dpiXProperty.GetValue(null, null);
                DpiX = (int)dpiYProperty.GetValue(null, null);
            }
            catch
            {
                DpiX = 96;
                DpiY = 96;
            }
#if !DOTNET40 && !DOTNET45 && !DOTNET46
            DpiScale = new DpiScale(DpiX / 96.0, DpiY / 96.0);
#endif
        }

        public static int DpiX { get; private set; }
        public static int DpiY { get; private set; }
#if !DOTNET40 && !DOTNET45 && !DOTNET46
        public static DpiScale DpiScale { get; private set; }
#endif
        public static double PixelsPerDip => GetPixelsPerDip();

        public static double GetPixelsPerDip()
        {
            return DpiY / 96.0;
        }




    }


    public class Shape : ClipArtElement
    {
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

        public virtual Stroke Stroke
        {
            get
            {
                if (this.m_stroke != null)
                {
                    return this.m_stroke;
                }

                var parent = this.Parent;
                while (parent != null)
                {
                    if (this.Parent.Stroke != null)
                    {
                        return parent.Stroke;
                    }

                    parent = parent.Parent;
                }
                return null;
            }
            set
            {
                m_stroke = value;
            }
        }

        public virtual Fill Fill
        {
            get
            {
                if (this.m_fill != null)
                {
                    return this.m_fill;
                }

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
            set
            {
                m_fill = value;
            }
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
                TextDecoration t = new TextDecoration();
                if (value == "none")
                {
                    return;
                }

                if (value == "underline")
                {
                    t.Location = TextDecorationLocation.Underline;
                }

                if (value == "overline")
                {
                    t.Location = TextDecorationLocation.OverLine;
                }

                if (value == "line-through")
                {
                    t.Location = TextDecorationLocation.Strikethrough;
                }

                TextDecorationCollection tt = new TextDecorationCollection();
                tt.Add(t);
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

    public class LengthPercentageOrNumberList : IList<LengthPercentageOrNumber>
    {
        private readonly Shape _owner;
        private List<LengthPercentageOrNumber> _list = new List<LengthPercentageOrNumber>();
        private readonly LengthOrientation _orientation;
        private static readonly Regex _splitRegex = new Regex(@"\b(?:,|\s*,?\s+)\b", RegexOptions.Compiled);
        private LengthPercentageOrNumberList(Shape owner, LengthOrientation orientation = LengthOrientation.None)
        {
            _owner = owner;
            _orientation = orientation;
        }
        public LengthPercentageOrNumberList(Shape owner, string value, LengthOrientation orientation = LengthOrientation.None) : this(owner, orientation)
        {
            Parse(value);
        }
        private void Parse(string value)
        {
            string[] list = _splitRegex.Split(value.Trim());

            if (list.Any(string.IsNullOrEmpty))
            {
                throw new ArgumentException("Invalid length/percentage/number list: " + value);
            }
            _list = list.Select(s=>LengthPercentageOrNumber.Parse(_owner, s, _orientation)).ToList();
        }

        public static LengthPercentageOrNumberList Empty(Shape owner, LengthOrientation orientation = LengthOrientation.None)
        {
            return new LengthPercentageOrNumberList(owner, orientation);
        }


        public LengthPercentageOrNumber this[int index] { get => ((IList<LengthPercentageOrNumber>)_list)[index]; set => ((IList<LengthPercentageOrNumber>)_list)[index] = value; }

        public int Count => ((ICollection<LengthPercentageOrNumber>)_list).Count;

        public bool IsReadOnly => ((ICollection<LengthPercentageOrNumber>)_list).IsReadOnly;

        public void Add(LengthPercentageOrNumber item)
        {
            //Remove units because Child elements do not inherit the relative values as specified for their parent; they inherit the computed values.
            var strippedContext = new LengthPercentageOrNumber(item.Value, new LengthContext(_owner, LengthUnit.None));
            ((ICollection<LengthPercentageOrNumber>)_list).Add(strippedContext);
        }

        public void Clear()
        {
            ((ICollection<LengthPercentageOrNumber>)_list).Clear();
        }

        public bool Contains(LengthPercentageOrNumber item)
        {
            return ((ICollection<LengthPercentageOrNumber>)_list).Contains(item);
        }

        public void CopyTo(LengthPercentageOrNumber[] array, int arrayIndex)
        {
            ((ICollection<LengthPercentageOrNumber>)_list).CopyTo(array, arrayIndex);
        }

        public IEnumerator<LengthPercentageOrNumber> GetEnumerator()
        {
            return ((IEnumerable<LengthPercentageOrNumber>)_list).GetEnumerator();
        }

        public int IndexOf(LengthPercentageOrNumber item)
        {
            return ((IList<LengthPercentageOrNumber>)_list).IndexOf(item);
        }

        public void Insert(int index, LengthPercentageOrNumber item)
        {
            //Remove units because Child elements do not inherit the relative values as specified for their parent; they inherit the computed values.
            var strippedContext = new LengthPercentageOrNumber(item.Value, new LengthContext(_owner, LengthUnit.None));
            ((ICollection<LengthPercentageOrNumber>)_list).Add(strippedContext);
            ((IList<LengthPercentageOrNumber>)_list).Insert(index, strippedContext);
        }

        public bool Remove(LengthPercentageOrNumber item)
        {
            return ((ICollection<LengthPercentageOrNumber>)_list).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<LengthPercentageOrNumber>)_list).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_list).GetEnumerator();
        }
    }

    /// <summary>
    /// Child elements do not inherit the relative values as specified for their parent; they inherit the computed values.
    /// </summary>
    public enum LengthUnit
    {
        Unknown = -1,
        /// <summary>
        /// 
        /// </summary>
        None,
        /// <summary>
        /// Percent is relative to least distant viewbox dimensions.
        /// If the length is inherently horizontal, like "dx", then the percentage is relative to the least distant viewbox width.
        /// If the length is inherently vertical, like "dy", then the percentage is relative to the least distant viewbox height.
        /// Otherwise the percentage is relative to the least distant viewbox diagonal. 
        /// </summary>
        /// <remarks>
        /// Setting a unit of <see cref="Percent"/> may be converted into <see cref="PercentWidth"/>, <see cref="PercentHeight"/>, or <see cref="PercentDiagonal"/>
        /// </remarks>
        Percent,
        /// <summary>
        /// Percentage is relative to the least distant viewbox width.
        /// </summary>
        PercentWidth,
        /// <summary>
        /// Percentage is relative to the least distant viewbox height.
        /// </summary>
        PercentHeight,
        /// <summary>
        /// Percentage is relative to the least distant viewbox diagonal
        /// </summary>
        PercentDiagonal,
        /// <summary>
        /// Relative to font size of the element
        /// </summary>
        em,

        /// <summary>
        /// Relative to x-height of the element’s font
        /// </summary>
        ex,

        /// <summary>
        /// Relative to character advance of the “0” (ZERO, U+0030) glyph in the element’s font
        /// </summary>
        ch,

        /// <summary>
        /// Relative to font size of the root element
        /// </summary>
        rem,

        /// <summary>
        /// Relative to 1% of viewport’s width
        /// </summary>
        vw,

        /// <summary>
        /// Relative to 1% of viewport’s height
        /// </summary>
        vh,

        /// <summary>
        /// Relative to 1% of viewport’s smaller dimension
        /// </summary>
        vmin,

        /// <summary>
        /// Relative to 1% of viewport’s larger dimension
        /// </summary>
        vmax,

        /// <summary>
        /// centimeters	1cm = 96px/2.54
        /// </summary>
        cm,
        /// <summary>
        /// millimeters	1mm = 1/10th of 1cm
        /// </summary>
        mm,
        /// <summary>
        /// quarter-millimeters	1Q = 1/40th of 1cm
        /// </summary>
        Q,
        /// <summary>
        /// inches	1in = 2.54cm = 96px
        /// </summary>
        @in,
        /// <summary>
        /// picas	1pc = 1/6th of 1in
        /// </summary>
        pc,
        /// <summary>
        /// points	1pt = 1/72nd of 1in
        /// </summary>
        pt,
        /// <summary>
        /// pixels	1px = 1/96th of 1in
        /// </summary>
        px,



    }
    public enum LengthOrientation
    {
        None,
        Horizontal,
        Vertical,
    }
    public class LengthContext
    {
        public Shape Owner { get; set; }
        public LengthUnit Unit { get; set; }
        private static readonly Dictionary<string, LengthUnit> _unitMap = new Dictionary<string, LengthUnit>()
        {
            {"em", LengthUnit.em},
            {"ex", LengthUnit.ex},
            {"ch", LengthUnit.ch},
            {"rem", LengthUnit.rem},
            {"vw", LengthUnit.vw},
            {"vh", LengthUnit.vh},
            {"vmin", LengthUnit.vmin},
            {"vmax", LengthUnit.vmax},
            {"cm", LengthUnit.cm},
            {"mm", LengthUnit.mm},
            {"Q", LengthUnit.Q},
            {"in", LengthUnit.@in},
            {"pc", LengthUnit.pc},
            {"pt", LengthUnit.pt},
            {"px", LengthUnit.px},
        };

        public LengthContext(Shape owner, LengthUnit unit)
        {
            Owner = owner;
            Unit = unit;
        }

        public static LengthUnit Parse(string text, LengthOrientation orientation = LengthOrientation.None)
        {
            if (String.IsNullOrEmpty(text))
            {
                return LengthUnit.None;
            }
            string trimmed = text.Trim();
            if(trimmed == "%")
            {
                switch (orientation)
                {
                    case LengthOrientation.Horizontal:
                        return LengthUnit.PercentWidth;
                    case LengthOrientation.Vertical:
                        return LengthUnit.PercentHeight;
                    default:
                        return LengthUnit.PercentDiagonal;
                }
            }
            if(_unitMap.TryGetValue(trimmed, out LengthUnit unit))
            {
                return unit;
            }
            return LengthUnit.Unknown;
        }
    }

    public struct LengthPercentageOrNumber
    {
        private static readonly Regex _lengthRegex = new Regex(@"(?<Value>\d+(?:\.\d+)?)\s*(?<Unit>%|\w+)?", RegexOptions.Compiled | RegexOptions.Singleline);
        private readonly LengthContext _context;
        private readonly double _value;
        public double Value => ResolveValue();

        
        private static double ResolveAbsoluteValue(double value, LengthContext context)
        {
            switch (context.Unit)
            {
                case LengthUnit.cm:
                    return value * 35.43;
                case LengthUnit.mm:
                    return value * 3.54;
                case LengthUnit.Q:
                    return value * 3.54 / 4d;
                case LengthUnit.@in:
                    return value * 90d;
                case LengthUnit.pc:
                    return value * 15d;
                case LengthUnit.pt:
                    return value * 1.25;
                case LengthUnit.px:
                    return value * 90d / 96d;
                case LengthUnit.Unknown:
                case LengthUnit.None:
                default:
                    return value;
            }
        }
        private static double ResolveViewboxValue(double value, LengthContext context)
        {
            double height;
            double width;
            if (context.Owner.Svg.ViewBox.HasValue)
            {
                height = context.Owner.Svg.ViewBox.Value.Height;
                width = context.Owner.Svg.ViewBox.Value.Width;
            }
            else
            {
                height = context.Owner.Svg.Size.Height;
                width = context.Owner.Svg.Size.Width;
            }
            switch (context.Unit)
            {
                case LengthUnit.Percent:
                    throw new NotSupportedException("Percent without specific orientation is not supported. Use PercentWidth, PercentHeight, or PercentDiagonal instead.");
                case LengthUnit.PercentDiagonal:
                    return (value / 100d) * Math.Sqrt(Math.Pow(width, 2d) + Math.Pow(height, 2d));
                case LengthUnit.vw:
                case LengthUnit.PercentWidth:
                    return (value / 100d) * width;
                case LengthUnit.vh:
                case LengthUnit.PercentHeight:
                    return (value / 100d) * height;
                case LengthUnit.vmin:
                    return (value / 100d) * Math.Min(width, height);
                case LengthUnit.vmax:
                    return (value / 100d) * Math.Max(width, height);
                case LengthUnit.Unknown:
                case LengthUnit.None:
                default:
                    return value;
            }
        }
        private static double ResolveRelativeValue(double value, LengthContext context)
        {
            switch (context.Unit)
            {
                case LengthUnit.em:
                    return value * context.Owner.TextStyle.FontSize;
                case LengthUnit.ex:
                    return value * context.Owner.TextStyle.GetTypeface().XHeight;
                case LengthUnit.ch:
                    var glyphTypeface = context.Owner.TextStyle.GetGlyphTypeface();
                    return value * glyphTypeface.AdvanceWidths[glyphTypeface.CharacterToGlyphMap['0']];
                case LengthUnit.rem:
                    return value * context.Owner.GetRoot().TextStyle.FontSize;
                case LengthUnit.Unknown:
                case LengthUnit.None:
                default:
                    return value;
            }
        }
        private double ResolveValue()
        {
            if (_context == null)
            {
                return _value; // No context, return raw value
            
            }

            switch (_context.Unit)
            {
                case LengthUnit.Percent:
                case LengthUnit.PercentWidth:
                case LengthUnit.PercentHeight:
                case LengthUnit.PercentDiagonal:
                case LengthUnit.vw:
                case LengthUnit.vh:
                case LengthUnit.vmin:
                case LengthUnit.vmax:
                    return ResolveViewboxValue(_value, _context);
                case LengthUnit.em:
                case LengthUnit.ex:
                case LengthUnit.ch:
                case LengthUnit.rem:
                    return ResolveRelativeValue(_value, _context);
                case LengthUnit.cm:
                case LengthUnit.mm:
                case LengthUnit.Q:
                case LengthUnit.@in:
                case LengthUnit.pc:
                case LengthUnit.pt:
                case LengthUnit.px:
                    return ResolveAbsoluteValue(_value, _context);
                case LengthUnit.Unknown:
                case LengthUnit.None:
                default:
                    return _value;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context">If null, units will be ignored</param>
        public LengthPercentageOrNumber(double value, LengthContext context)
        {
            _context = context;
            _value = value;
        }
        public static LengthPercentageOrNumber Parse(Shape owner, string value, LengthOrientation orientation = LengthOrientation.None)
        {
            var lengthMatch = _lengthRegex.Match(value.Trim());
            if(!lengthMatch.Success || !Double.TryParse(lengthMatch.Groups["Value"].Value, out double d))
            {
                throw new ArgumentException($"Invalid length/percentage/number value: {value}");
            }
            LengthContext context;
            if (lengthMatch.Groups["Unit"].Success)
            {
                string unitStr = lengthMatch.Groups["Unit"].Value;
                LengthUnit unit = LengthContext.Parse(unitStr, orientation);
                if (unit == LengthUnit.Unknown)
                {
                    throw new ArgumentException($"Unknown length unit: {unitStr}");
                }
                context = new LengthContext(owner, unit);
            }
            else
            {
                // Default to pixels if no unit is specified
                context = new LengthContext(owner, LengthUnit.px);
            }
            return new LengthPercentageOrNumber(d, context);
        }
        
    }

    

    

    public enum LengthAdjustment
    {
        None,
        /// <summary>
        /// Indicates that only the advance values are adjusted. The glyphs themselves are not stretched or compressed.
        /// </summary>
        Spacing,
        /// <summary>
        /// Indicates that the advance values are adjusted and the glyphs themselves stretched or compressed in one axis (i.e., a direction parallel to the inline-base direction).
        /// </summary>
        SpacingAndGlyphs
    }


    [DebuggerDisplay("{DebugDisplayText}")]
    public class TextShapeBase: Shape, ITextNode
    {
        protected TextShapeBase(SVG svg, XmlNode node, Shape parent) : base(svg, node, parent)
        {
        }

        private string DebugDisplayText => GetDebugDisplayText(new StringBuilder());
        private string GetDebugDisplayText(StringBuilder sb)
        {
            if (Children.Count == 0)
            {
                return "<Empty>";
            }
            foreach(var child in Children)
            {
                if (child is TextString textString)
                {
                    sb.Append(textString.Text);
                }
                else if (child is TextSpan textSpan)
                {
                    sb.Append('(');
                    textSpan.GetDebugDisplayText(sb);
                    sb.Append(')');
                }
            }

            return sb.ToString();
        }

        public LengthPercentageOrNumberList X { get; protected set; }
        public LengthPercentageOrNumberList Y { get; protected set; } 
        public LengthPercentageOrNumberList DX { get; protected set; }
        public LengthPercentageOrNumberList DY { get; protected set; }
        public List<double> Rotate { get; protected set; } = new List<double>();
        public LengthPercentageOrNumber? TextLength { get; set; }
        public LengthAdjustment LengthAdjust { get; set; } = LengthAdjustment.Spacing;
        public List<ITextChild> Children { get; } = new List<ITextChild>();
        public CharacterLayout FirstCharacter => GetFirstCharacter();
        public CharacterLayout LastCharacter => GetLastCharacter();
        public string Text => GetText();
        public int Length => GetLength();

        public CharacterLayout[] GetCharacters()
        {
            return Children.SelectMany(c => c.GetCharacters()).ToArray();
        }

        public CharacterLayout GetFirstCharacter()
        {
            foreach(var child in Children)
            {
                if (child.GetFirstCharacter() is CharacterLayout firstChar)
                {
                    return firstChar;
                }
            }
            throw new InvalidOperationException("No characters found in text node.");
        }
        public CharacterLayout GetLastCharacter()
        {
            for (int i = Children.Count - 1; i >= 0; i--)
            {
                if (Children[i].GetLastCharacter() is CharacterLayout LastChar)
                {
                    return LastChar;
                }
            }
            throw new InvalidOperationException("No characters found in text node.");
        }

        public int GetLength()
        {
            return Children.Sum(c => c.GetLength());
        }

        public string GetText()
        {
            return string.Concat(Children.Select(c => c.GetText()));
        }

        protected override void ParseAtStart(SVG svg, XmlNode node)
        {
            base.ParseAtStart(svg, node);

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name)
                {
                    case "x":
                        X = new LengthPercentageOrNumberList(this, attr.Value, LengthOrientation.Horizontal);
                        break;
                    case "y":
                        Y = new LengthPercentageOrNumberList(this, attr.Value, LengthOrientation.Vertical);
                        break;
                    case "dx":
                        DX = new LengthPercentageOrNumberList(this, attr.Value, LengthOrientation.Horizontal);
                        break;
                    case "dy":
                        DY = new LengthPercentageOrNumberList(this, attr.Value, LengthOrientation.Vertical);
                        break;
                    case "rotate":
                        Rotate = attr.Value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(v => double.Parse(v)).ToList();
                        break;
                    case "textLength":
                        TextLength = LengthPercentageOrNumber.Parse(this, attr.Value);
                        break;
                    case "lengthAdjust":
                        LengthAdjust = Enum.TryParse(attr.Value, true, out LengthAdjustment adj) ? adj : LengthAdjustment.Spacing;
                        break;
                }
            }
            if(X is null)
            {
                X = LengthPercentageOrNumberList.Empty(this, LengthOrientation.Horizontal);
            }
            if (Y is null)
            {
                Y = LengthPercentageOrNumberList.Empty(this, LengthOrientation.Vertical);
            }
            if (DX is null)
            {
                DX = LengthPercentageOrNumberList.Empty(this, LengthOrientation.Horizontal);
            }
            if (DY is null)
            {
                DY = LengthPercentageOrNumberList.Empty(this, LengthOrientation.Vertical);
            }

            ParseChildren(svg, node);
        }

        protected void ParseChildren(SVG svg, XmlNode node)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Text || child.NodeType == XmlNodeType.CDATA)
                {
                    var text = child.InnerText.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        Children.Add(new TextString(this, text));
                    }
                }
                else if (child.NodeType == XmlNodeType.Element && child.Name == "tspan")
                {
                    var span = new TextSpan(svg, child, this);
                    Children.Add(span);
                }
                // Future support for <textPath>, <tref>, etc. could go here
            }
        }

    }

    public class Text : TextShapeBase
    {
        public Text(SVG svg, XmlNode node, Shape parent) : base(svg, node, parent)
        {
        }

    }
    public interface ITextChild : ITextNode
    {
        Shape Parent { get; set; }
    }
    public interface ITextNode
    {
        CharacterLayout GetFirstCharacter();
        CharacterLayout GetLastCharacter();
        string GetText();
        int GetLength();
        CharacterLayout[] GetCharacters();
    }

    [DebuggerDisplay("{Text}")]
    /// <summary>
    /// Text not wrapped in a <tspan> element.
    /// </summary>
    public class TextString : ITextChild
    {
        public CharacterLayout[] Characters { get; set; }
        public Shape Parent { get; set; }
        public int Index { get; set; }
        private static readonly Regex _trimmedWhitespace = new Regex(@"\s+", RegexOptions.Compiled | RegexOptions.Singleline);
        public TextString(Shape parent, string text)
        {
            Parent = parent;
            string trimmed = _trimmedWhitespace.Replace(text.Trim(), " ");
            Characters = new CharacterLayout[trimmed.Length];
            for(int i = 0; i < trimmed.Length; i++)
            {
                var c = trimmed[i];
                Characters[i] = new CharacterLayout(c);
            }
        }
        public CharacterLayout GetFirstCharacter()
        {
            return Characters.FirstOrDefault();
        }
        public CharacterLayout GetLastCharacter()
        {
            return Characters.LastOrDefault();
        }
        public CharacterLayout FirstCharacter => GetFirstCharacter();
        public CharacterLayout LastCharacter => GetLastCharacter();
        public string Text => GetText();
        public int Length => GetLength();

        public TextStyle TextStyle { get; internal set; }

        public string GetText()
        {
            return new string(Characters.Select(c => c.Character).ToArray());
        }

        public int GetLength()
        {
            return Characters.Length;
        }

        public CharacterLayout[] GetCharacters()
        {
            return Characters;
        }
    }

    public class TextSpan : TextShapeBase, ITextChild
    {

        public TextSpan(SVG svg, XmlNode node, Shape parent) : base(svg, node, parent)
        {
        }

    }
    
    public static partial class TextRender
    {
        internal class TextCursor
        {
            public Point Position { get; set; }
            public Matrix Transform { get; set; } = Matrix.Identity;
            public TextCursor(Point position)
            {
                Position = position;
            }
            public void MoveTo(double x, double y)
            {
                Position = new Point(x, y);
            }
            public void Offset(double dx, double dy)
            {
                Position = new Point(Position.X + dx, Position.Y + dy);
            }
            public void Rotate(double angleDegrees)
            {
                Transform.RotateAt(angleDegrees, Position.X, Position.Y);
            }
        }



    }
    public class TextPath : TextShapeBase, ITextChild
    {
        protected TextPath(SVG svg, XmlNode node, Shape parent) : base(svg, node, parent)
        {
            throw new NotImplementedException("TextPath is not yet implemented.");
        }
    }
    /// <summary>
    /// Represents a per-character layout result.
    /// </summary>
    public class CharacterLayout
    {
        private CharacterLayout()
        {
            // Default constructor for array creation
        }
        public CharacterLayout(char character)
        {
            Character = character;
        }
        public char Character { get; set; } = '\0';
        public int GlobalIndex { get; set; }
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double DX { get; set; } = Double.NaN;
        public double DY { get; set; } = Double.NaN;
        public double Rotation { get; set; } = Double.NaN;
        public bool Hidden { get; set; } = false;
        public bool Addressable { get; set; } = true;
        public bool Middle { get; set; } = false;
        public bool AnchoredChunk { get; set; } = false;
        public bool FirstCharacterInResolvedDescendant { get; internal set; }
        public bool DoesPositionX { get; internal set; }
        public bool DoesPositionY { get; internal set; }


    }
    public enum WritingMode
    {
        Horizontal,
        Vertical,
        VerticalRightToLeft
    }

    public static class EnumerableExtensions
    {
        public static int IndexOfFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            int i = 0;
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    return i;
                }
                i++;
            }
            return -1; // Not found
        }
    }

    public class TextRender2
    {
        private sealed class TextRenderState : IDisposable
        {
            private bool _disposedValue;

            public TextRenderState(Text root, WritingMode writingMode)
            {

                string text = root.GetText();
                Setup(root, text, writingMode);
                InitializeResolveArrays(text.Length);
            }
            public bool Setup(Text root, string text, WritingMode writingMode)
            {
                int globalIndex = 0;
                SetGlobalIndicies(root, ref globalIndex);
                _characters = root.GetCharacters();
                SetFlagsAndAssignInitialPositions(root, text);
                if (_characters.Length == 0)
                {
                    return false;
                }
                IsHorizontal = writingMode == WritingMode.Horizontal;
                return true;
            }
            public int BidiLevel { get; private set; } = 0;
            public bool IsSideways{ get; private set; }
            public bool IsHorizontal{ get; private set; }
            private CharacterLayout[] _characters;
            private double[] _resolvedX ;
            private double[] _resolvedY ;
            private double[] _resolvedDx;
            private double[] _resolvedDy;
            private double[] _resolvedRotate;
            private int[] _xBaseIndicies;
            private int[] _yBaseIndicies;
            private static T[] CreateRepeatedArray<T>(int count, T element) where T : struct
            {
                var result = new T[count];
                for (int i = 0; i < count; i++)
                {
                    result[i] = element;
                }
                return result;
            }

            private void InitializeResolveArrays(int length)
            {
                _xBaseIndicies = CreateRepeatedArray(length, -1);
                _yBaseIndicies = CreateRepeatedArray(length, -1);
                if (length > 0)
                {
                    _xBaseIndicies[0] = 0;
                    _yBaseIndicies[0] = 0;
                }
                _resolvedX = CreateRepeatedArray(length, double.NaN);
                _resolvedY = CreateRepeatedArray(length, double.NaN);
                _resolvedDx = CreateRepeatedArray(length, 0d);
                _resolvedDy = CreateRepeatedArray(length, 0d);
                _resolvedRotate = CreateRepeatedArray(length, double.NaN);
            }
            public void Resolve(TextShapeBase textSpan)
            {
                int index = textSpan.GetFirstCharacter().GlobalIndex;
                LengthPercentageOrNumberList x = textSpan.X;
                LengthPercentageOrNumberList y = textSpan.Y;
                LengthPercentageOrNumberList dx = textSpan.DX;
                LengthPercentageOrNumberList dy = textSpan.DY;
                List<double> rotate = textSpan.Rotate;
                //}

                var arrays = new List<Tuple<LengthPercentageOrNumberList, double[]>>();
                arrays.Add(new Tuple<LengthPercentageOrNumberList, double[]>(x, _resolvedX));
                arrays.Add(new Tuple<LengthPercentageOrNumberList, double[]>(y, _resolvedY));
                arrays.Add(new Tuple<LengthPercentageOrNumberList, double[]>(dx, _resolvedDx));
                arrays.Add(new Tuple<LengthPercentageOrNumberList, double[]>(dy, _resolvedDy));

                foreach(var tuple in arrays)
                {
                    var list = tuple.Item1;
                    var resolvedArray = tuple.Item2;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (index + i >= resolvedArray.Length)
                        {
                            break;
                        }
                        resolvedArray[index + i] = list[i].Value;
                    }
                }

                for (int i = 0; i < rotate.Count; i++)
                {
                    if (index + i >= _resolvedRotate.Length)
                    {
                        break;
                    }
                    _resolvedRotate[index + i] = rotate[i];
                }
                foreach (var child in textSpan.Children.OfType<TextShapeBase>())
                {
                    Resolve(child);
                }
                ApplyResolutions();
            }
            private static void SetGlobalIndicies(ITextNode textNode, ref int globalIndex)
            {
                if (textNode is TextShapeBase textNodeBase)
                {
                    foreach (var child in textNodeBase.Children)
                    {
                        SetGlobalIndicies(child, ref globalIndex);
                    }
                }
                else if (textNode is TextString textString)
                {
                    foreach (var c in textString.Characters)
                    {
                        c.GlobalIndex = globalIndex++;
                    }
                }
            }
            private void FillInGaps()
            {
                FillInGaps(_resolvedX, 0d, _xBaseIndicies);
                FillInGaps(_resolvedY, 0d, _yBaseIndicies);
                FillInGaps(_resolvedRotate, 0d);
            }
            private static void FillInGaps(double[] list, double? initialValue = null, int[] baseIndicies = null)
            {
                if (list == null || list.Length == 0)
                {
                    return;
                }
                if (Double.IsNaN(list[0]) && initialValue.HasValue && !Double.IsNaN(initialValue.Value))
                {
                    list[0] = initialValue.Value;
                }
                double current = list[0];
                int currentBaseIndex = 0;
                for (int i = 1; i < list.Length; i++)
                {
                    if (Double.IsNaN(list[i]))
                    {
                        list[i] = current;
                    }
                    else
                    {
                        current = list[i];
                        currentBaseIndex = i;
                    }
                    if (baseIndicies != null)
                    {
                        baseIndicies[i] = currentBaseIndex;
                    }
                }
            }
            private void ApplyResolutions()
            {
                FillInGaps();
                for (int i = 0; i < _characters.Length; i++)
                {
                    int xBaseIndex = _xBaseIndicies[i];
                    int yBaseIndex = _yBaseIndicies[i];
                    _characters[i].X = _resolvedX[xBaseIndex];
                    _characters[i].Y = _resolvedY[yBaseIndex];
                    _characters[i].DX = _resolvedDx.Skip(xBaseIndex).Take(i - xBaseIndex + 1).Sum();
                    _characters[i].DY = _resolvedDy.Skip(yBaseIndex).Take(i - yBaseIndex + 1).Sum();
                    _characters[i].Rotation = _resolvedRotate[i];
                    _characters[i].DoesPositionX = _xBaseIndicies[_characters[i].GlobalIndex] == _characters[i].GlobalIndex;
                    _characters[i].DoesPositionY = _yBaseIndicies[_characters[i].GlobalIndex] == _characters[i].GlobalIndex;
                }

            }
            /// <summary>
            /// Preliminary, Need Implementation
            /// </summary>
            /// <param name="c"></param>
            /// <returns></returns>
            private static bool IsNonRenderedCharacter(char c)
            {
                // Check for non-rendered characters like zero-width space, etc.
                return char.IsControl(c) || char.IsWhiteSpace(c) && c != ' ';
            }

            private static bool IsBidiControlCharacter(char c)
            {
                // Check for Bidi control characters
                return c == '\u2066' || c == '\u2067' || c == '\u2068' || c == '\u2069' ||
                    c == '\u200E' || c == '\u200F' || c == '\u202A' || c == '\u202B' || c == '\u202C' || c == '\u202D' || c == '\u202E';
            }

            /// <summary>
            /// discarded during layout due to being a collapsed white space character, a soft hyphen character, collapsed segment break, or a bidi control character
            /// </summary>
            /// <param name="c"></param>
            /// <returns></returns>
            private static bool WasDiscardedDuringLayout(char c)
            {
                return IsBidiControlCharacter(c) ||
                       c == '\u00AD' || // Soft hyphen
                       c == '\u200B' || // Zero-width space
                       c == '\u200C' || // Zero-width non-joiner
                       c == '\u200D' || // Zero-width joiner
                       char.IsWhiteSpace(c) && c != ' '; // Collapsed whitespace characters
            }

            private static bool IsAddressable(int index, char c, int beginningCharactersTrimmed, int startOfTrimmedEnd)
            {
                return !IsNonRenderedCharacter(c) &&
                         !WasDiscardedDuringLayout(c) &&
                       index >= beginningCharactersTrimmed &&
                       index < startOfTrimmedEnd;
            }
            private static readonly Regex _trimmedWhitespace = new Regex(@"(?<Start>^\s*).*(?<End>\s*$)", RegexOptions.Compiled | RegexOptions.Singleline);
            private static readonly Regex _lineStarts = new Regex(@"^ *\S", RegexOptions.Compiled | RegexOptions.Multiline);
            private static bool IsTypographicCharacter(char c, int index, string text)
            {
                return !IsNonRenderedCharacter(c); //It's not clear what a typographic character is in this context, so we assume all non-rendered characters are not typographic.
            }
            private static HashSet<int> GetLineBeginnings(string text)
            {
                HashSet<int> lineStartIndicies = new HashSet<int>();
                var lineStarts = _lineStarts.Matches(text);
                foreach (Match lineStart in lineStarts)
                {
                    lineStartIndicies.Add(lineStart.Index + lineStart.Length - 1);
                }
                return lineStartIndicies;
            }
            public void SetFlagsAndAssignInitialPositions(Text root, string text)
            {
                var trimmedText = _trimmedWhitespace.Match(text);
                int beginningCharactersTrimmed = trimmedText.Groups["Start"].Success ? trimmedText.Groups["Start"].Length : 0;
                int endingCharactersTrimmed = trimmedText.Groups["End"].Success ? trimmedText.Groups["End"].Length : 0;
                int startOfTrimmedEnd = text.Length - endingCharactersTrimmed;
                var lineBeginnings = GetLineBeginnings(text);
                for (int i = 0; i < _characters.Length; i++)
                {
                    var c = _characters[i];
                    bool isTypographic = IsTypographicCharacter(text[i], i, text);
                    c.Addressable = IsAddressable(i, text[i], beginningCharactersTrimmed, startOfTrimmedEnd);
                    c.Middle = i > 0 && isTypographic;
                    c.AnchoredChunk = lineBeginnings.Contains(i);
                }
            }

            private void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        _characters = null;
                        _resolvedX = null;
                        _resolvedY = null;
                        _resolvedDx = null;
                        _resolvedDy = null;
                        _resolvedRotate = null;
                    }

                    _disposedValue = true;
                }
            }


            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        public static readonly DependencyProperty TSpanElementProperty = DependencyProperty.RegisterAttached("TSpanElement", typeof(TextSpan), typeof(DependencyObject));
        public static void SetElement(DependencyObject obj, TextSpan value)
        {
            obj.SetValue(TSpanElementProperty, value);
        }
        public static TextSpan GetElement(DependencyObject obj)
        {
            return (TextSpan)obj.GetValue(TSpanElementProperty);
        }

        public GeometryGroup BuildTextGeometry(Text text, WritingMode writingMode = WritingMode.Horizontal)
        {
            using(TextRenderState state = new TextRenderState(text, writingMode))
            {
                if (!state.Setup(text, text.GetText(), writingMode))
                {
                    return null; // No characters to render
                }
                return CreateGeometry(text, state);
            }
        }
        private GeometryGroup CreateGeometry(Text root, TextRenderState state)
        {
            state.Resolve(root);

            List<TextString> textStrings = new List<TextString>();
            TextStyleStack textStyleStacks = new TextStyleStack();
            PopulateTextStrings(textStrings, root, textStyleStacks);
            GeometryGroup geometryGroup = new GeometryGroup();
            var baselineOrigin = new Point(root.X.FirstOrDefault().Value, root.Y.FirstOrDefault().Value);
            foreach (TextString textString in textStrings)
            {
                if(CreateRun(textString, state, ref baselineOrigin) is GlyphRun run)
                {
                    geometryGroup.Children.Add(run.BuildGeometry());
                }
            }

            geometryGroup.Transform = root.Transform;
            return geometryGroup;
        }

        private static GlyphRun CreateRun(TextString textString, TextRenderState state, ref Point baselineOrigin)
        {
            var textStyle = textString.TextStyle;
            var characterInfos = textString.GetCharacters();
            if(characterInfos is null ||characterInfos.Length == 0)
            {
                return null;
            }
            var font = textStyle.GetTypeface();
            if (!font.TryGetGlyphTypeface(out var glyphFace))
            {
                return null;
            }
            string deviceFontName = null;
            IList<ushort> clusterMap = null;
            IList<bool> caretStops = null;
            XmlLanguage language = null;
            var glyphOffsets = characterInfos.Select(c => new Point(c.DX, -c.DY)).ToList();
            var renderingEmSize = textStyle.FontSize;
            var characters = characterInfos.Select(c => c.Character).ToArray();
            var glyphIndices = characters.Select(c => glyphFace.CharacterToGlyphMap[c]).ToList();
            var advanceWidths = glyphIndices.Select(c => glyphFace.AdvanceWidths[c] * renderingEmSize).ToArray();

            //if (characterInfos[0].X)


            if (characterInfos[0].DoesPositionX)
            {
                baselineOrigin.X = characterInfos[0].X;
            }
            if (characterInfos[0].DoesPositionY)
            {
                baselineOrigin.Y = characterInfos[0].Y;
            }

            GlyphRun run = new GlyphRun(glyphFace, state.BidiLevel, state.IsSideways, renderingEmSize,
#if !DOTNET40 && !DOTNET45 && !DOTNET46
                (float)DpiUtil.PixelsPerDip,
#endif
                glyphIndices, baselineOrigin, advanceWidths, glyphOffsets, characters, deviceFontName, clusterMap, caretStops, language);
            
            var newX = baselineOrigin.X + advanceWidths.Sum();
            var newY = baselineOrigin.Y ;

            baselineOrigin = new Point(newX, newY);
            return run;
        }
        

        private sealed class TextStyleStack
        {
            private readonly Stack<TextStyle> _stack = new Stack<TextStyle>();
            internal void Push(TextStyle textStyle)
            {
                if (textStyle == null)
                {
                    throw new ArgumentNullException(nameof(textStyle), "TextStyle cannot be null.");
                }
                if(_stack.Count == 0)
                {
                    _stack.Push(textStyle);
                    return;
                }
                _stack.Push(TextStyle.Merge(_stack.Peek(), textStyle));
            }

            internal TextStyle Pop()
            {
                return _stack.Pop();
            }
            internal TextStyle Peek()
            {
                return _stack.Peek();
            }
        }

        private void PopulateTextStrings(List<TextString> textStrings, ITextNode node, TextStyleStack textStyleStacks)
        {
            if(node is TextShapeBase span)
            {
                textStyleStacks.Push(span.TextStyle);
                foreach (var child in span.Children)
                {
                    PopulateTextStrings(textStrings, child, textStyleStacks);
                }
                _ = textStyleStacks.Pop();
            }
            else if(node is TextString textString)
            {
                textString.TextStyle = textStyleStacks.Peek();
                textStrings.Add(textString);
            }
        }

        


        

        

        


    }

    public class TextLengthResolver
    {
        private readonly CharacterLayout[] _result;
        private readonly bool _horizontal;
        private readonly double[] _resolveDx;
        private readonly double[] _resolveDy;

        public TextLengthResolver(CharacterLayout[] result, bool horizontal, double[] resolveDx, double[] resolveDy)
        {
            _result = result;
            _horizontal = horizontal;
            _resolveDx = resolveDx;
            _resolveDy = resolveDy;
        }


        /// <summary>
        /// Define resolved descendant node as a descendant of node with a valid ‘textLength’ attribute that is not itself a descendant node of a descendant node that has a valid ‘textLength’ attribute
        /// </summary>
        /// <param name="textNode"></param>
        /// <param name="descendant"></param>
        /// <returns></returns>
        private bool IsResolvedDescendantNode(ITextNode textNode, ITextNode descendant)
        {
            if (textNode is TextShapeBase textShape)
            {
                if (textShape.TextLength != null && !textShape.Children.Any(c => c is TextShapeBase child && child.TextLength != null))
                {
                    return true;
                }
                foreach (var child in textShape.Children)
                {
                    if (IsResolvedDescendantNode(child, descendant))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public void ResolveTextLength(ITextNode textNode, string text, ref bool in_text_path)
        {
            if (textNode is TextShapeBase textNodeBase)
            {
                foreach (var child in textNodeBase.Children)
                {
                    ResolveTextLength(child, text, ref in_text_path);
                }
            }
            if (textNode is TextSpan textSpan && textSpan.TextLength != null && !IsResolvedDescendantNode(textNode, textSpan))
            {
                // Calculate total advance width
                double totalAdvance = 0;
                for (int i = 0; i < _result.Length; i++)
                {
                    if (_result[i].Addressable)
                    {
                        totalAdvance += _resolveDx[i];
                    }
                }
                // Calculate scaling factor
                double scaleFactor = textSpan.TextLength.Value.Value / totalAdvance;
                // Apply scaling to dx and dy
                for (int i = 0; i < _result.Length; i++)
                {
                    if (_result[i].Addressable)
                    {
                        _resolveDx[i] *= scaleFactor;
                        _resolveDy[i] *= scaleFactor;
                    }
                }
            }
        }

        private static double GetAdvance(CharacterLayout characterLayout)
        {
            throw new NotImplementedException();
        }

        public void ResolveTextSpanTextLength(TextSpan textSpan, string text, ref bool in_text_path)
        {
            double a = Double.PositiveInfinity;
            double b = Double.NegativeInfinity;
            int i = textSpan.GetFirstCharacter().GlobalIndex;
            int j = textSpan.GetLastCharacter().GlobalIndex;
            for (int k = i; k <= j; k++)
            {
                if (!_result[k].Addressable)
                {
                    continue;
                }
                if (_result[k].Character == '\r' || _result[k].Character == '\n')
                {
                    //No adjustments due to ‘textLength’ are made to a node with a forced line break.
                    return;
                }
                double pos = _horizontal ? _result[k].X : _result[k].Y;
                double advance = GetAdvance(_result[k]); //This advance will be negative for RTL horizontal text.

                a = Math.Min(Math.Min(a, pos), pos + advance);
                b = Math.Max(Math.Max(b, pos), pos + advance);

            }
            if (!Double.IsPositiveInfinity(a))
            {
                double delta = textSpan.TextLength.Value.Value - (b - a);
                int n = 0;// textSpan.GetTypographicCharacterCount();
                int resolvedDescendantNodes = GetResolvedDescendantNodeCount(textSpan, ref n);
                n += resolvedDescendantNodes - 1;//Each resolved descendant node is treated as if it were a single typographic character in this context.
                var δ = delta / n;
                double shift = 0;
                for (int k = i; k <= j; k++)
                {
                    if (_horizontal)
                    {
                        _result[k].X += shift;
                    }
                    else
                    {
                        _result[k].Y += shift;
                    }
                    if (!_result[k].Middle && IsNotACharacterInAResolvedDescendantNodeOtherThanTheFirstCharacter(textSpan, _result[k]))
                    {
                        shift += δ;
                    }
                }
            }
        }

        internal int GetResolvedDescendantNodeCount(ITextNode node, ref int n)
        {
            int resolvedDescendantNodes = 0;
            if (node is TextSpan textSpan)
            {
                n = textSpan.Children.Count;
                for (int c = 0; c < textSpan.Children.Count; c++)
                {
                    if (textSpan.Children[c].GetText() is string ccontent)
                    {
                        n += String.IsNullOrEmpty(ccontent) ? 0 : ccontent.Length;
                    }
                    else
                    {
                        _result[n].FirstCharacterInResolvedDescendant = true;
                        resolvedDescendantNodes++;
                    }
                }
            }
            else if (node is TextString textString)
            {
                n = textString.GetLength();
            }
            return resolvedDescendantNodes;
        }
        private static bool IsNotACharacterInAResolvedDescendantNodeOtherThanTheFirstCharacter(ITextNode textNode, CharacterLayout character)
        {
            throw new NotImplementedException("This method needs to be implemented based on the specific rules for resolved descendant nodes.");
        }

    }




}
