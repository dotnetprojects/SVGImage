using System.Collections.Generic;
using System.Windows.Media;
using System.Windows;

namespace SVGImage.SVG.Shapes
{
    using Utils;
    using System.Linq;
    using System.Windows.Markup;
    using System;
    using System.Reflection;
    using System.Windows.Documents;

    /// <summary>
    /// This class is responsible for rendering text shapes in SVG.
    /// </summary>
    public sealed partial class TextRender : TextRenderBase
    {
        public override GeometryGroup BuildTextGeometry(TextShape text)
        {

            using(TextRenderState state = new TextRenderState())
            {
                if (!state.Setup(text))
                {
                    return null; // No characters to render
                }
                return CreateGeometry(text, state);
            }
        }



        public static readonly DependencyProperty TextSpanTextStyleProperty = DependencyProperty.RegisterAttached(
            "TextSpanTextStyle", typeof(TextStyle), typeof(DependencyObject));
        private static void SetTextSpanTextStyle(DependencyObject obj, TextStyle value)
        {
            obj.SetValue(TextSpanTextStyleProperty, value);
        }
        public static TextStyle GetTextSpanTextStyle(DependencyObject obj)
        {
            return (TextStyle)obj.GetValue(TextSpanTextStyleProperty);
        }

        private sealed class TextChunk
        {
            public List<GlyphRun> GlyphRuns { get; set; } = new List<GlyphRun>();
            public Dictionary<GlyphRun, List<Rect>> BackgroundDecorations { get; set; } = new Dictionary<GlyphRun, List<Rect>>();
            public Dictionary<GlyphRun, List<Rect>> ForegroundDecorations { get; set; } = new Dictionary<GlyphRun, List<Rect>>();
            public Dictionary<GlyphRun, Rect> GlyphRunBounds { get; set; } = new Dictionary<GlyphRun, Rect>();
            public Dictionary<GlyphRun, TextStyle> TextStyles { get; set; } = new Dictionary<GlyphRun, TextStyle>();
            public Dictionary<GlyphRun, TextShapeBase> TextContainers { get; set; } = new Dictionary<GlyphRun, TextShapeBase>();
            public TextAlignment TextAlignment { get; set; }

            public GeometryGroup BuildGeometry()
            {
                double alignmentOffset = GetAlignmentOffset();
                bool nonZeroAlignmentOffset = !alignmentOffset.IsNearlyZero();
                GeometryGroup geometryGroup = new GeometryGroup();
                foreach (var glyphRun in GlyphRuns)
                {
                    var runGeometry = !nonZeroAlignmentOffset ? glyphRun.BuildGeometry() : glyphRun.CreateOffsetRun(alignmentOffset, 0).BuildGeometry();
                    geometryGroup.Children.Add(runGeometry);
                    if (TextStyles.TryGetValue(glyphRun, out TextStyle textStyle))
                    {
                        TextRender.SetTextSpanTextStyle(runGeometry, textStyle);
                    }
                    if (TextContainers.TryGetValue(glyphRun, out TextShapeBase textContainer))
                    {
                        TextRender.SetElement(runGeometry, textContainer);
                    }
                    if (BackgroundDecorations.TryGetValue(glyphRun, out List<Rect> backgroundDecorations))
                    {
                        foreach (var decoration in backgroundDecorations)
                        {
                            if (nonZeroAlignmentOffset)
                            {
                                decoration.Offset(alignmentOffset, 0);
                            }
                            //Underline and OverLine should be drawn behind the text
                            geometryGroup.Children.Insert(0, new RectangleGeometry(decoration));
                        }
                    }
                    if (ForegroundDecorations.TryGetValue(glyphRun, out List<Rect> foregroundDecorations))
                    {
                        foreach (var decoration in foregroundDecorations)
                        {
                            if (nonZeroAlignmentOffset)
                            {
                                decoration.Offset(alignmentOffset, 0);
                            }
                            //Strikethrough should be drawn on top of the text
                            geometryGroup.Children.Add(new RectangleGeometry(decoration));
                        }
                    }
                }
                return geometryGroup;
            }

            private Rect GetBoundingBox()
            {
                if (GlyphRunBounds.Count == 0)
                {
                    return Rect.Empty;
                }
                Rect boundingBox = GlyphRunBounds.First().Value;
                foreach (var kvp in GlyphRunBounds)
                {
                    boundingBox.Union(kvp.Value);
                }
                return boundingBox;
            }

            private double GetAlignmentOffset()
            {
                if(TextAlignment == TextAlignment.Left)
                {
                    return 0; // No offset needed for left alignment
                }
                var boundingBox = GetBoundingBox();
                double totalWidth = boundingBox.Width;
                double alignmentOffset = 0;
                switch (TextAlignment)
                {
                    case TextAlignment.Left:
                        break;
                    case TextAlignment.Right:
                        alignmentOffset = totalWidth;
                        break;
                    case TextAlignment.Center:
                        alignmentOffset = totalWidth / 2d;
                        break;
                    case TextAlignment.Justify:
                        // Justify is not implemented
                        break;
                    default:
                        break;
                }
                return alignmentOffset;
            }
        }
        private static GeometryGroup CreateGeometry(TextShape root, TextRenderState state)
        {
            state.Resolve(root);

            List<TextString> textStrings = new List<TextString>();
            TextStyleStack textStyleStacks = new TextStyleStack();
            PopulateTextStrings(textStrings, root, textStyleStacks);
            GeometryGroup mainGeometryGroup = new GeometryGroup();
            var baselineOrigin = new Point(root.X.FirstOrDefault().Value, root.Y.FirstOrDefault().Value);
            var isSideways = root.WritingMode == WritingMode.HorizontalTopToBottom;
            TextAlignment currentTextAlignment = root.TextStyle.TextAlignment;
            List<TextChunk> textChunks = new List<TextChunk>();
            bool newTextChunk = false;
            TextChunk currentTextChunk = null;
            foreach (TextString textString in textStrings)
            {
                var textStyle = textString.TextStyle;
                Typeface font = textString.TextStyle.GetTypeface();
                if (CreateRun(textString, state, font, isSideways, baselineOrigin, out Point newBaseline, out newTextChunk, ref currentTextAlignment) is GlyphRun run)
                {
                    if (newTextChunk)
                    {
                        if(currentTextChunk != null)
                        {
                            // Add the current text chunk to the list
                            textChunks.Add(currentTextChunk);
                        }
                        currentTextChunk = new TextChunk();
                        currentTextChunk.TextAlignment = currentTextAlignment;
                    }
                    var runGeometry = run.BuildGeometry();
                    currentTextChunk.GlyphRuns.Add(run);
                    currentTextChunk.TextStyles[run] = textStyle;
                    currentTextChunk.GlyphRunBounds[run] = runGeometry.Bounds;
                    currentTextChunk.TextContainers[run] = (TextShapeBase)textString.Parent;
                    if (textStyle.TextDecoration != null && textStyle.TextDecoration.Count > 0)
                    {
                        GetTextDecorations(runGeometry, textStyle, font, baselineOrigin, out List<Rect> backgroundDecorations, out List<Rect> foregroundDecorations);
                        if(backgroundDecorations.Count > 0)
                        {
                            currentTextChunk.BackgroundDecorations[run] = backgroundDecorations;
                        }
                        if (foregroundDecorations.Count > 0)
                        {
                            currentTextChunk.ForegroundDecorations[run] = foregroundDecorations;
                        }
                    }
                }
                baselineOrigin = newBaseline;
            }
            textChunks.Add(currentTextChunk);

            foreach(var textChunk in textChunks)
            {
                if (textChunk.GlyphRuns.Count == 0)
                {
                    continue; // No glyphs to render in this chunk
                }
                GeometryGroup geometryGroup = textChunk.BuildGeometry();
                mainGeometryGroup.Children.Add(geometryGroup);
            }

            mainGeometryGroup.Transform = root.Transform;
            return mainGeometryGroup;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Not perfect, the lines are not continuous across multiple text strings.
        /// </remarks>
        /// <param name="geometry"></param>
        /// <param name="textStyle"></param>
        /// <param name="font"></param>
        /// <param name="baselineOrigin"></param>
        /// <param name="backgroundDecorations"></param>
        /// <param name="foregroundDecorations"></param>
        private static void GetTextDecorations(Geometry geometry, TextStyle textStyle, Typeface font, Point baselineOrigin, out List<Rect> backgroundDecorations, out List<Rect> foregroundDecorations)
        {
            backgroundDecorations = new List<Rect>();
            foregroundDecorations = new List<Rect>();
            double decorationPos = 0;
            double decorationThickness = 0;
            double fontSize = textStyle.FontSize;
            double baselineY = baselineOrigin.Y;
            foreach(TextDecorationLocation textDecorationLocation in textStyle.TextDecoration.Select(td=>td.Location))
            {
                if (textDecorationLocation == TextDecorationLocation.Strikethrough)
                {
                    decorationPos = baselineY - (font.StrikethroughPosition * fontSize);
                    decorationThickness = font.StrikethroughThickness * fontSize;
                    Rect bounds = new Rect(geometry.Bounds.Left, decorationPos, geometry.Bounds.Width, decorationThickness);
                    foregroundDecorations.Add(bounds);
                }
                else if (textDecorationLocation == TextDecorationLocation.Underline)
                {
                    decorationPos = baselineY - (font.UnderlinePosition * fontSize);
                    decorationThickness = font.UnderlineThickness * fontSize;
                    Rect bounds = new Rect(geometry.Bounds.Left, decorationPos, geometry.Bounds.Width, decorationThickness);
                    backgroundDecorations.Add(bounds);
                }
                else if (textDecorationLocation == TextDecorationLocation.OverLine)
                {
                    decorationPos = baselineY - fontSize;
                    decorationThickness = font.StrikethroughThickness * fontSize;
                    Rect bounds = new Rect(geometry.Bounds.Left, decorationPos, geometry.Bounds.Width, decorationThickness);
                    backgroundDecorations.Add(bounds);
                }
            }
        }

        private static GlyphRun CreateRun(TextString textString, TextRenderState state, Typeface font, bool isSideways, Point baselineOrigin, out Point newBaseline, out bool newTextChunk, ref TextAlignment currentTextAlignment)
        {
            newTextChunk = textString.FirstCharacter.GlobalIndex == 0;
            var textStyle = textString.TextStyle;
            var characterInfos = textString.GetCharacters();
            if(characterInfos is null ||characterInfos.Length == 0)
            {
                newBaseline = baselineOrigin;
                return null;
            }
            if (!font.TryGetGlyphTypeface(out var glyphFace))
            {
                newBaseline = baselineOrigin;
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
            var advanceWidths = WrapInThousandthOfEmRealDoubles(renderingEmSize, glyphIndices.Select(c => glyphFace.AdvanceWidths[c] * renderingEmSize).ToArray());

            if (characterInfos[0].DoesPositionX)
            {
                baselineOrigin.X = characterInfos[0].X;
                newTextChunk = true;
            }
            if (characterInfos[0].DoesPositionY)
            {
                baselineOrigin.Y = characterInfos[0].Y;
                newTextChunk = true;
            }
            else if(textString.TextStyle.TextAlignment != currentTextAlignment)
            {
                newTextChunk = true;
            }

            double baselineShift = 0;
            baselineShift += BaselineHelper.EstimateBaselineShiftValue(textStyle, textString.Parent);
            //if (textStyle.BaseLineShift == "sub")
            //{
            //    baselineShift += textStyle.FontSize * 0.5; /* * cap height ? fontSize*/
            //}
            //else if (textStyle.BaseLineShift == "super")
            //{
            //    baselineShift -= textStyle.FontSize + (textStyle.FontSize * 0.25)/*font.CapsHeight * fontSize*/;
            //}

            double totalWidth = advanceWidths.Sum();


            GlyphRun run = new GlyphRun(glyphFace, state.BidiLevel, isSideways, renderingEmSize,
#if !DOTNET40 && !DOTNET45 && !DOTNET46
                (float)DpiUtil.PixelsPerDip,
#endif
                glyphIndices, new Point(baselineOrigin.X, baselineOrigin.Y + baselineShift), advanceWidths, glyphOffsets, characters, deviceFontName, clusterMap, caretStops, language);

            var newX = baselineOrigin.X + totalWidth;
            var newY = baselineOrigin.Y ;

            newBaseline = new Point(newX, newY);
            return run;
        }

        private static readonly Type _thousandthOfEmRealDoublesType = Type.GetType("MS.Internal.TextFormatting.ThousandthOfEmRealDoubles, PresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        private static readonly ConstructorInfo _thousandthOfEmRealDoublesConstructor = _thousandthOfEmRealDoublesType?.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(double), typeof(IList<double>)}, null);

        /// <summary>
        /// Microsoft's GlyphRun converts the advance widths to thousandths of an em when using EndInit.
        /// This wrapper method is used to compare apples to apples
        /// </summary>
        /// <param name="renderingEmSize"></param>
        /// <param name="advanceWidths"></param>
        /// <returns></returns>
        private static IList<double> WrapInThousandthOfEmRealDoubles(double renderingEmSize, IList<double> advanceWidths)
        {
            return (IList<double>)_thousandthOfEmRealDoublesConstructor?.Invoke(new object[] { renderingEmSize, advanceWidths }) ?? advanceWidths;
        }


        private static void PopulateTextStrings(List<TextString> textStrings, ITextNode node, TextStyleStack textStyleStacks)
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




}
