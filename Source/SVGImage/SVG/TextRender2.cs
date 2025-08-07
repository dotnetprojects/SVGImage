using System.Collections.Generic;
using System.Windows.Media;
using System.Windows;
using System.Reflection;
using SVGImage.SVG.Shapes;
using System.Linq;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG
{
    public class TextRender2 : TextRenderBase
    {
        public override GeometryGroup BuildTextGeometry(TextShape shape)
        {
            return BuildGlyphRun(shape, 0, 0);
        }

        // Use GlyphRun to build the text. This allows us to define letter and word spacing
        // http://books.google.com/books?id=558i6t1dKEAC&pg=PA485&source=gbs_toc_r&cad=4#v=onepage&q&f=false
        static GeometryGroup BuildGlyphRun(TextShape shape, double xoffset, double yoffset)
        {
            GeometryGroup gp = new GeometryGroup();
            return BuildTextSpan(shape);
        }

        static GeometryGroup BuildTextSpan(TextShape shape)
        {
            double x = shape.X.FirstOrDefault().Value;
            double y = shape.Y.FirstOrDefault().Value;
            GeometryGroup gp = new GeometryGroup();
            BuildTextSpan(gp, shape.TextStyle, shape, ref x, ref y);
            return gp;
        }

        

        static void BuildTextSpan(GeometryGroup gp, TextStyle textStyle,
            TextShapeBase tspan, ref double x, ref double y)
        {
            foreach (ITextNode child in tspan.Children)
            {
                if (child is TextString textString)
                {
                    string txt = textString.Text;
                    double totalwidth = 0;
                    double baseline = y;

                    if (textString.TextStyle.BaseLineShift == "sub")
                        baseline += textString.TextStyle.FontSize * 0.5; /* * cap height ? fontSize*/;
                    if (textString.TextStyle.BaseLineShift == "super")
                        baseline -= tspan.TextStyle.FontSize + (textString.TextStyle.FontSize * 0.25)/*font.CapsHeight * fontSize*/;

                    Geometry gm = BuildGlyphRun(textString.TextStyle, txt, x, baseline, ref totalwidth);
                    TextRender2.SetElement(gm, (TextShapeBase)textString.Parent);
                    gp.Children.Add(gm);
                    x += totalwidth;
                }
                else if (child is TextShapeBase childTspan)
                {
                    BuildTextSpan(gp, textStyle, childTspan, ref x, ref y);
                }
            }
        }

        static Geometry BuildGlyphRun(TextStyle textStyle, string text, double x, double y, ref double totalwidth)
        {
            if (string.IsNullOrEmpty(text))
                return new GeometryGroup();

            double fontSize = textStyle.FontSize;
            GlyphRun glyphs = null;
            Typeface font = new Typeface(new FontFamily(textStyle.FontFamily),
                textStyle.Fontstyle,
                textStyle.Fontweight,
                FontStretch.FromOpenTypeStretch(9),
                new FontFamily("Arial Unicode MS"));
            GlyphTypeface glyphFace;
            double baseline = y;
            if (font.TryGetGlyphTypeface(out glyphFace))
            {
#if DPI_AWARE
                glyphs = new GlyphRun((float)DpiUtil.PixelsPerDip);
#else
                glyphs = new GlyphRun();
#endif
                ((System.ComponentModel.ISupportInitialize)glyphs).BeginInit();
                glyphs.GlyphTypeface = glyphFace;
                glyphs.FontRenderingEmSize = fontSize;
                List<char> textChars = new List<char>();
                List<ushort> glyphIndices = new List<ushort>();
                List<double> advanceWidths = new List<double>();
                totalwidth = 0;
                char[] charsToSkip = new char[] { '\t', '\r', '\n' };
                for (int i = 0; i < text.Length; ++i)
                {
                    char textchar = text[i];
                    int codepoint = textchar;
                    //if (charsToSkip.Any<char>(item => item == codepoint))
                    //	continue;
                    ushort glyphIndex;
                    if (!glyphFace.CharacterToGlyphMap.TryGetValue(codepoint, out glyphIndex))
                        continue;
                    textChars.Add(textchar);
                    double glyphWidth = glyphFace.AdvanceWidths[glyphIndex];
                    glyphIndices.Add(glyphIndex);
                    advanceWidths.Add(glyphWidth * fontSize + textStyle.LetterSpacing);
                    if (char.IsWhiteSpace(textchar))
                        advanceWidths[advanceWidths.Count - 1] += textStyle.WordSpacing;
                    totalwidth += advanceWidths[advanceWidths.Count - 1];
                }
                glyphs.Characters = textChars.ToArray();
                glyphs.GlyphIndices = glyphIndices.ToArray();
                glyphs.AdvanceWidths = advanceWidths.ToArray();

                // calculate text alignment
                double alignmentoffset = 0;
                if (textStyle.TextAlignment == TextAlignment.Center)
                    alignmentoffset = totalwidth / 2;
                if (textStyle.TextAlignment == TextAlignment.Right)
                    alignmentoffset = totalwidth;

                baseline = y;
                glyphs.BaselineOrigin = new Point(x - alignmentoffset, baseline);
                ((System.ComponentModel.ISupportInitialize)glyphs).EndInit();
            }
            else
                return new GeometryGroup();

            // add text decoration to geometry
            GeometryGroup gp = new GeometryGroup();
            gp.Children.Add(glyphs.BuildGeometry());
            if (textStyle.TextDecoration != null)
            {
                double decorationPos = 0;
                double decorationThickness = 0;
                if (textStyle.TextDecoration[0].Location == TextDecorationLocation.Strikethrough)
                {
                    decorationPos = baseline - (font.StrikethroughPosition * fontSize);
                    decorationThickness = font.StrikethroughThickness * fontSize;
                }
                if (textStyle.TextDecoration[0].Location == TextDecorationLocation.Underline)
                {
                    decorationPos = baseline - (font.UnderlinePosition * fontSize);
                    decorationThickness = font.UnderlineThickness * fontSize;
                }
                if (textStyle.TextDecoration[0].Location == TextDecorationLocation.OverLine)
                {
                    decorationPos = baseline - fontSize;
                    decorationThickness = font.StrikethroughThickness * fontSize;
                }
                Rect bounds = new Rect(gp.Bounds.Left, decorationPos, gp.Bounds.Width, decorationThickness);
                gp.Children.Add(new RectangleGeometry(bounds));

            }
            return gp;
        }
    }
}