//using System.Collections.Generic;
//using System.Windows.Media;
//using System.Windows;
//using System.Reflection;
//using System.Windows.Media.TextFormatting;

//namespace SVGImage.SVG
//{
//    static class TextRender
//    {
//        private static int dpiX = 0;
//        private static int dpiY = 0;

//        static public GeometryGroup BuildTextGeometry(TextShape shape)
//        {
//            double cursorX = 0;
//            double cursorY = 0;
//            return BuildTextSpan(shape.TextSpan, shape.TextStyle, ref cursorX, ref cursorY);
//            //return BuildGlyphRun(shape, 0, 0);
//        }

//        // Use GlyphRun to build the text. This allows us to define letter and word spacing
//        // http://books.google.com/books?id=558i6t1dKEAC&pg=PA485&source=gbs_toc_r&cad=4#v=onepage&q&f=false
//        static GeometryGroup BuildGlyphRun(TextShape shape)
//        {
//            GeometryGroup gp = new GeometryGroup();
//            double totalwidth = 0;
//            if (shape.TextSpan == null)
//            {
//                string txt = shape.Text;
//                gp.Children.Add(BuildGlyphRun(shape.TextStyle, txt, shape.X, shape.Y, ref totalwidth,
//    shape.TextSpan.XList,
//    shape.TextSpan.YList,
//    shape.TextSpan.DXList,
//    shape.TextSpan.DYList));
//                return gp;
//            }
//            double cursorX = 0;
//            double cursorY = 0;
//            return BuildTextSpan(shape.TextSpan, shape.TextStyle, ref cursorX, ref cursorY);
//        }

//        //static GeometryGroup BuildTextSpan(TextShape shape)
//        //{
//        //    double x = shape.X;
//        //    double y = shape.Y;
//        //    GeometryGroup gp = new GeometryGroup();
//        //    BuildTextSpan(gp, shape.TextStyle, shape.TextSpan, ref x, ref y);
//        //    return gp;
//        //}

//        private static GeometryGroup BuildTextSpan(TextSpan tspan, TextStyle style, ref double cursorX, ref double cursorY)
//        {
//            GeometryGroup group = new GeometryGroup();
//            double localX = cursorX;
//            double localY = cursorY;

//            foreach (TextSpan child in tspan.Children)
//            {
//                double spanX = localX;
//                double spanY = localY;

//                // Apply absolute positioning if x/y exist
//                if (child.XList.Count > 0)
//                {
//                    spanX = child.XList[0];
//                }
//                else if (child.DXList.Count > 0)
//                {
//                    spanX += child.DXList[0];
//                }

//                if (child.YList.Count > 0)
//                {
//                    spanY = child.YList[0];
//                }
//                else if (child.DYList.Count > 0)
//                {
//                    spanY += child.DYList[0];
//                }

//                double totalWidth = 0.0;

//                if (child.ElementType == TextSpan.eElementType.Text)
//                {
//                    Geometry gm = BuildGlyphRun(
//                        child.TextStyle ?? style,
//                        child.Text,
//                        spanX,
//                        spanY,
//                        ref totalWidth,
//                        child.XList,
//                        child.YList,
//                        child.DXList,
//                        child.DYList
//                    );

//                    group.Children.Add(gm);
//                }
//                else
//                {
//                    Geometry gm = BuildTextSpan(child, child.TextStyle ?? style, ref spanX, ref spanY);
//                    group.Children.Add(gm);
//                }

//                // Advance cursor only if this tspan didn't reset x
//                if (child.XList.Count == 0)
//                {
//                    localX = spanX + totalWidth;
//                }
//                else
//                {
//                    localX = spanX; // reset after absolute x
//                }

//                if (child.YList.Count == 0)
//                {
//                    localY = spanY;
//                }
//                else
//                {
//                    localY = spanY;
//                }
//            }

//            cursorX = localX;
//            cursorY = localY;
//            return group;
//        }

//        public static DependencyProperty TSpanElementProperty = DependencyProperty.RegisterAttached(
//            "TSpanElement", typeof(TextSpan), typeof(DependencyObject));
//        public static void SetElement(DependencyObject obj, TextSpan value)
//        {
//            obj.SetValue(TSpanElementProperty, value);
//        }
//        public static TextSpan GetElement(DependencyObject obj)
//        {
//            return (TextSpan)obj.GetValue(TSpanElementProperty);
//        }
//        static void BuildTextSpan(GeometryGroup gp, TextStyle textStyle, TextSpan tspan, ref double x, ref double y)
//        {
//            //int xi = 0, yi = 0, dxi = 0, dyi = 0;
//            //BuildTextSpan(gp, textStyle, tspan, ref x, ref y, ref xi, ref yi, ref dxi, ref dyi);
//            var builtSpan = BuildTextSpan(tspan, textStyle, ref x, ref y);
//            gp.Children.Add(builtSpan);

//        }





//        //static void BuildTextSpan(GeometryGroup gp, TextStyle textStyle, 
//        //    TextSpan tspan, ref double x, ref double y,
//        //    ref int xIndex, ref int yIndex, ref int dxIndex, ref int dyIndex)
//        //{
//        //    foreach (TextSpan child in tspan.Children)
//        //    {
//        //        double spanX = x;
//        //        double spanY = y;

//        //        if (child.XList.Count > xIndex) { spanX = child.XList[xIndex++]; x = spanX; }
//        //        if (child.YList.Count > yIndex) { spanY = child.YList[yIndex++]; y = spanY; }

//        //        if (child.DXList.Count > dxIndex) { spanX += child.DXList[dxIndex++]; x = spanX; }
//        //        if (child.DYList.Count > dyIndex) { spanY += child.DYList[dyIndex++]; y = spanY; }

//        //        if (child.ElementType == TextSpan.eElementType.Text)
//        //        {
//        //            // Inherit position attributes from parent if not defined
//        //            List<double> xList = (child.XList.Count > 0) ? child.XList : tspan.XList;
//        //            List<double> yList = (child.YList.Count > 0) ? child.YList : tspan.YList;
//        //            List<double> dxList = (child.DXList.Count > 0) ? child.DXList : tspan.DXList;
//        //            List<double> dyList = (child.DYList.Count > 0) ? child.DYList : tspan.DYList;
//        //            string txt = child.Text;
//        //            double totalwidth = 0;
//        //            double baseline = y;

//        //            if (child.TextStyle.BaseLineShift == "sub")
//        //                baseline += child.TextStyle.FontSize * 0.5;
//        //            if (child.TextStyle.BaseLineShift == "super")
//        //                baseline -= tspan.TextStyle.FontSize + (child.TextStyle.FontSize * 0.25);

//        //            Geometry gm = BuildGlyphRun(child.TextStyle, txt, spanX, baseline, ref totalwidth,
//        //                    xList, yList, dxList, dyList);
//        //            TextRender.SetElement(gm, child);
//        //            gp.Children.Add(gm);
//        //            x += totalwidth;
//        //        }
//        //        else if (child.ElementType == TextSpan.eElementType.Tag)
//        //        {
//        //            BuildTextSpan(gp, textStyle, child, ref x, ref y, ref xIndex, ref yIndex, ref dxIndex, ref dyIndex);
//        //        }
//        //    }
//        //}

//        static Geometry BuildGlyphRun(
//    TextStyle textStyle,
//    string text,
//    double x,
//    double y,
//    ref double totalWidth,
//    List<double> xList,
//    List<double> yList,
//    List<double> dxList,
//    List<double> dyList)
//        {
//            if (string.IsNullOrEmpty(text))
//            {
//                return Geometry.Empty;
//            }

//            if (dpiX == 0 || dpiY == 0)
//            {
//                var sysPara = typeof(SystemParameters);
//                var dpiXProperty = sysPara.GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static);
//                var dpiYProperty = sysPara.GetProperty("Dpi", BindingFlags.NonPublic | BindingFlags.Static);

//                dpiX = (int)dpiXProperty.GetValue(null, null);
//                dpiY = (int)dpiYProperty.GetValue(null, null);
//            }
//            double fontSize = textStyle.FontSize;
//            Typeface font = new Typeface(new FontFamily(textStyle.FontFamily),
//                textStyle.Fontstyle,
//                textStyle.Fontweight,
//                FontStretch.FromOpenTypeStretch(9),
//                new FontFamily("Arial Unicode MS"));
//            GlyphTypeface glyphFace;

//            if (!font.TryGetGlyphTypeface(out glyphFace))
//            {
//                return new GeometryGroup();
//            }


//            List<char> textChars = new List<char>();
//            List<ushort> glyphIndices = new List<ushort>();
//            List<double> advanceWidths = new List<double>();

//            for (int i = 0; i < text.Length; i++)
//            {
//                char textchar = text[i];
//                int codepoint = textchar;
//                if (!glyphFace.CharacterToGlyphMap.TryGetValue(codepoint, out var glyphIndex))
//                {
//                    glyphIndices[i] = 0; // fallback to default glyph
//                }
//                glyphIndices.Add(glyphIndex);

//                double glyphWidth = glyphFace.AdvanceWidths[glyphIndex] * fontSize + textStyle.LetterSpacing;
//                if (char.IsWhiteSpace(textchar))
//                    glyphWidth += textStyle.WordSpacing;

//                textChars.Add(textchar);
//                advanceWidths.Add(glyphWidth);
//            }

//            Point baseline = new Point(x, y);
//            List<Point> origins = new List<Point>();
//            double currentX = x;
//            double currentY = y;

//            for (int i = 0; i < text.Length; i++)
//            {
//                if (xList != null && i < xList.Count)
//                {
//                    currentX = xList[i];
//                }
//                else if (dxList != null && i < dxList.Count)
//                {
//                    currentX += dxList[i];
//                }

//                if (yList != null && i < yList.Count)
//                {
//                    currentY = yList[i];
//                }
//                else if (dyList != null && i < dyList.Count)
//                {
//                    currentY += dyList[i];
//                }

//                origins.Add(new Point(currentX, -currentY));
//                currentX += advanceWidths[i];
//            }

//            totalWidth = currentX - x;

//            GlyphRun glyphs = new GlyphRun(
//                glyphFace,
//                0,
//                false,
//                fontSize,
//#if !(DOTNET40 || DOTNET45 || DOTNET46)
//                (float)(new DpiScale(dpiX, dpiY)).PixelsPerDip,
//#endif
//            glyphIndices,
//                new Point(baseline.X - 50, baseline.Y + 20),
//                advanceWidths,
//                origins,
//                null,
//                null,
//                null,
//                null,
//                null
//            );



//            // add text decoration to geometry
//            GeometryGroup gp = new GeometryGroup();
//            gp.Children.Add(glyphs.BuildGeometry());
//            if (textStyle.TextDecoration != null)
//            {
//                double decorationPos = 0;
//                double decorationThickness = 0;
//                if (textStyle.TextDecoration[0].Location == TextDecorationLocation.Strikethrough)
//                {
//                    decorationPos = baseline.Y - (font.StrikethroughPosition * fontSize);
//                    decorationThickness = font.StrikethroughThickness * fontSize;
//                }
//                if (textStyle.TextDecoration[0].Location == TextDecorationLocation.Underline)
//                {
//                    decorationPos = baseline.Y - (font.UnderlinePosition * fontSize);
//                    decorationThickness = font.UnderlineThickness * fontSize;
//                }
//                if (textStyle.TextDecoration[0].Location == TextDecorationLocation.OverLine)
//                {
//                    decorationPos = baseline.Y - fontSize;
//                    decorationThickness = font.StrikethroughThickness * fontSize;
//                }
//                Rect bounds = new Rect(gp.Bounds.Left, decorationPos, gp.Bounds.Width, decorationThickness);
//                gp.Children.Add(new RectangleGeometry(bounds));

//            }
//            return gp;
//        }


//        //        static Geometry BuildGlyphRun(TextStyle textStyle, string text, double x, double y, ref double totalwidth,
//        //    List<double> xList = null,
//        //    List<double> yList = null,
//        //    List<double> dxList = null,
//        //    List<double> dyList = null)
//        //        {
//        //            if (string.IsNullOrEmpty(text))
//        //                return new GeometryGroup();

//        //            if (dpiX == 0 || dpiY == 0)
//        //            {
//        //                var sysPara = typeof(SystemParameters);
//        //                var dpiXProperty = sysPara.GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static);
//        //                var dpiYProperty = sysPara.GetProperty("Dpi", BindingFlags.NonPublic | BindingFlags.Static);

//        //                dpiX = (int)dpiXProperty.GetValue(null, null);
//        //                dpiY = (int)dpiYProperty.GetValue(null, null);
//        //            }
//        //            double fontSize = textStyle.FontSize;
//        //            GlyphRun glyphs = null;
//        //            Typeface font = new Typeface(new FontFamily(textStyle.FontFamily), 
//        //                textStyle.Fontstyle, 
//        //                textStyle.Fontweight,
//        //                FontStretch.FromOpenTypeStretch(9),
//        //                new FontFamily("Arial Unicode MS"));
//        //            GlyphTypeface glyphFace;
//        //            double baseline = y;
//        //            if (!font.TryGetGlyphTypeface(out glyphFace))
//        //            {
//        //                return new GeometryGroup();
//        //            }
//        //#if DOTNET40 || DOTNET45 || DOTNET46
//        //                glyphs = new GlyphRun();
//        //#else
//        //                var dpiScale = new DpiScale(dpiX, dpiY);

//        //                glyphs = new GlyphRun((float)dpiScale.PixelsPerDip);
//        //#endif
//        //                ((System.ComponentModel.ISupportInitialize)glyphs).BeginInit();
//        //                glyphs.GlyphTypeface = glyphFace;
//        //                glyphs.FontRenderingEmSize = fontSize;
//        //                List<char> textChars = new List<char>();
//        //                List<ushort> glyphIndices = new List<ushort>();
//        //                List<double> advanceWidths = new List<double>();
//        //                totalwidth = 0;
//        //                char[] charsToSkip = new char[] { '\t', '\r', '\n' };
//        //                List<Point> glyphOffsets = new List<Point>();

//        //                double currentX = x;
//        //                double currentY = y;
//        //                for (int i = 0; i < text.Length; ++i)
//        //                {
//        //                    char textchar = text[i];
//        //                    int codepoint = textchar;

//        //                    if (!glyphFace.CharacterToGlyphMap.TryGetValue(codepoint, out ushort glyphIndex))
//        //                        continue;

//        //                    textChars.Add(textchar);

//        //                    double glyphWidth = glyphFace.AdvanceWidths[glyphIndex] * fontSize + textStyle.LetterSpacing;
//        //                    if (char.IsWhiteSpace(textchar))
//        //                        glyphWidth += textStyle.WordSpacing;

//        //                    // Absolute overrides (apply once, not cumulative)
//        //                    if (!(xList is null) && i < xList.Count)
//        //                        currentX = xList[i];
//        //                    if (!(yList is null) && i < yList.Count)
//        //                        currentY = yList[i];

//        //                    // Relative offsets (cumulative)
//        //                    if (!(dxList is null) && i < dxList.Count)
//        //                        currentX += dxList[i];
//        //                    if (!(dyList is null) && i < dyList.Count)
//        //                        currentY += dyList[i];

//        //                    glyphIndices.Add(glyphIndex);
//        //                    advanceWidths.Add(0); // Width will be zero since position is handled by offset
//        //                    glyphOffsets.Add(new Point(currentX, -currentY));

//        //                    currentX += glyphWidth;
//        //                    totalwidth += glyphWidth;



//        //                    //char textchar = text[i];
//        //                    //int codepoint = textchar;
//        //                    ////if (charsToSkip.Any<char>(item => item == codepoint))
//        //                    ////	continue;
//        //                    //ushort glyphIndex;
//        //                    //if (glyphFace.CharacterToGlyphMap.TryGetValue(codepoint, out glyphIndex) == false)
//        //                    //    continue;
//        //                    //textChars.Add(textchar);
//        //                    //double glyphWidth = glyphFace.AdvanceWidths[glyphIndex];
//        //                    //glyphIndices.Add(glyphIndex);
//        //                    //advanceWidths.Add(glyphWidth * fontSize + textStyle.LetterSpacing);
//        //                    //if (char.IsWhiteSpace(textchar))
//        //                    //    advanceWidths[advanceWidths.Count - 1] += textStyle.WordSpacing;
//        //                    //totalwidth += advanceWidths[advanceWidths.Count - 1];
//        //                }
//        //                glyphs.GlyphOffsets = glyphOffsets.ToArray();
//        //                //glyphs.GlyphOffsets = new Point[] { new Point(glyphOffsets[0].X, -glyphOffsets[0].Y), new Point(glyphOffsets[1].X, -glyphOffsets[1].Y), new Point(glyphOffsets[2].X, -glyphOffsets[2].Y) };
//        //                glyphs.Characters = textChars.ToArray();
//        //                glyphs.GlyphIndices = glyphIndices.ToArray();
//        //                glyphs.AdvanceWidths = advanceWidths.ToArray();

//        //                // calculate text alignment
//        //                double alignmentoffset = 0;
//        //                if (textStyle.TextAlignment == TextAlignment.Center)
//        //                    alignmentoffset = totalwidth / 2;
//        //                if (textStyle.TextAlignment == TextAlignment.Right)
//        //                    alignmentoffset = totalwidth;

//        //                baseline = y;
//        //                //glyphs.BaselineOrigin = new Point(x - alignmentoffset, baseline);
//        //                glyphs.BaselineOrigin = new Point(0 - alignmentoffset, 0);
//        //                ((System.ComponentModel.ISupportInitialize)glyphs).EndInit();


//        //            // add text decoration to geometry
//        //            GeometryGroup gp = new GeometryGroup();
//        //            gp.Children.Add(glyphs.BuildGeometry());
//        //            if (textStyle.TextDecoration != null)
//        //            {
//        //                double decorationPos = 0;
//        //                double decorationThickness = 0;
//        //                if (textStyle.TextDecoration[0].Location == TextDecorationLocation.Strikethrough)
//        //                {
//        //                    decorationPos = baseline - (font.StrikethroughPosition * fontSize);
//        //                    decorationThickness = font.StrikethroughThickness * fontSize;
//        //                }
//        //                if (textStyle.TextDecoration[0].Location == TextDecorationLocation.Underline)
//        //                {
//        //                    decorationPos = baseline - (font.UnderlinePosition * fontSize);
//        //                    decorationThickness = font.UnderlineThickness * fontSize;
//        //                }
//        //                if (textStyle.TextDecoration[0].Location == TextDecorationLocation.OverLine)
//        //                {
//        //                    decorationPos =   baseline - fontSize;
//        //                    decorationThickness = font.StrikethroughThickness * fontSize;
//        //                }
//        //                Rect bounds = new Rect(gp.Bounds.Left, decorationPos, gp.Bounds.Width, decorationThickness);
//        //                gp.Children.Add(new RectangleGeometry(bounds));

//        //            }
//        //            return gp;
//        //        }
//    }
//}
