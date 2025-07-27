using System.Windows;

namespace SVGImage.SVG
{
    using Shapes;
    using System.Windows.Media;

    public sealed class TextStyle
    {
        //This should be configurable in some way.
        private static readonly TextStyle _defaults = new TextStyle()
        {
            FontFamily = "Arial Unicode MS, Verdana",
            FontSize = 12,
            Fontweight = FontWeights.Normal,
            Fontstyle = FontStyles.Normal,
            TextAlignment = System.Windows.TextAlignment.Left,
            WordSpacing = 0,
            LetterSpacing = 0,
            BaseLineShift = string.Empty,
        };
        private string _fontFamily;
        private double? _fontSize;
        private FontWeight? _fontweight;
        private FontStyle? _fontstyle;
        private TextAlignment? _textAlignment;
        private double? _wordSpacing;
        private double? _letterSpacing;
        private string _baseLineShift;

        public TextStyle(TextStyle aCopy)
        {
            this.Copy(aCopy);
        }

        private TextStyle()
        {

        }

        public TextStyle(Shape owner)
        {
            if (owner.Parent != null)
            {
                this.Copy(owner.Parent.TextStyle);
            }
        }

        public Typeface GetTypeface()
        {
            return  new Typeface(new FontFamily(FontFamily),
            Fontstyle,
            Fontweight,
            FontStretch.FromOpenTypeStretch(9),
            new FontFamily(_defaults.FontFamily));
        }

        public GlyphTypeface GetGlyphTypeface()
        {
            var typeface = GetTypeface();
            GlyphTypeface glyphTypeface;
            if (typeface.TryGetGlyphTypeface(out glyphTypeface))
            {
                return glyphTypeface;
            }
            return null;
        }

        public string FontFamily { get => _fontFamily ?? _defaults.FontFamily; set => _fontFamily = value; }
        public double FontSize { get => _fontSize ?? _defaults.FontSize; set => _fontSize = value; }
        public FontWeight Fontweight { get => _fontweight ?? _defaults.Fontweight; set => _fontweight = value; }
        public FontStyle Fontstyle { get => _fontstyle ?? _defaults.Fontstyle; set => _fontstyle = value; }
        public TextDecorationCollection TextDecoration {get; set;}
        public TextAlignment TextAlignment { get => _textAlignment ?? _defaults.TextAlignment; set => _textAlignment = value; }
        public double WordSpacing { get => _wordSpacing ?? _defaults.WordSpacing; set => _wordSpacing = value; }
        public double LetterSpacing { get => _letterSpacing ?? _defaults.LetterSpacing; set => _letterSpacing = value; }
        public string BaseLineShift { get => _baseLineShift ?? _defaults.BaseLineShift; set => _baseLineShift = value; }

        private void Copy(TextStyle aCopy)
        {
            if (aCopy == null)
                return;
            this._fontFamily = aCopy._fontFamily;
            this._fontSize = aCopy._fontSize;
            this._fontweight = aCopy._fontweight;
            this._fontstyle = aCopy._fontstyle;
            this._textAlignment = aCopy._textAlignment;
            this._wordSpacing = aCopy._wordSpacing;
            this._letterSpacing = aCopy._letterSpacing;
            this._baseLineShift = aCopy._baseLineShift;
        }

        public static TextStyle Merge(TextStyle baseStyle, TextStyle overrides)
        {
            var result = new TextStyle();
            result._fontFamily = overrides._fontFamily ?? baseStyle._fontFamily;
            result._fontSize = overrides._fontSize ?? baseStyle._fontSize;
            result._fontweight = overrides._fontweight ?? baseStyle._fontweight;
            result._fontstyle = overrides._fontstyle ?? baseStyle._fontstyle;
            result._textAlignment = overrides._textAlignment ?? baseStyle._textAlignment;
            result._wordSpacing = overrides._wordSpacing ?? baseStyle._wordSpacing;
            result._letterSpacing = overrides._letterSpacing ?? baseStyle._letterSpacing;
            result._baseLineShift = overrides._baseLineShift ?? baseStyle._baseLineShift;
            return result;
        }

    }
    
}
