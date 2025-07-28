using System.Collections.Generic;
using System.Windows.Media;
using System.Windows;

namespace SVGImage.SVG.Shapes
{
    using Utils;
    using System.Linq;
    using System.Windows.Markup;

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
        private static GeometryGroup CreateGeometry(TextShape root, TextRenderState state)
        {
            state.Resolve(root);

            List<TextString> textStrings = new List<TextString>();
            TextStyleStack textStyleStacks = new TextStyleStack();
            PopulateTextStrings(textStrings, root, textStyleStacks);
            GeometryGroup mainGeometryGroup = new GeometryGroup();
            var baselineOrigin = new Point(root.X.FirstOrDefault().Value, root.Y.FirstOrDefault().Value);
            var isSideways = root.WritingMode == WritingMode.HorizontalTopToBottom;
            foreach (TextString textString in textStrings)
            {
                GeometryGroup geometryGroup = new GeometryGroup();
                var textStyle = textString.TextStyle;
                Typeface font = textString.TextStyle.GetTypeface();
                if (CreateRun(textString, state, font, isSideways, baselineOrigin, out Point newBaseline) is GlyphRun run)
                {
                    var runGeometry = run.BuildGeometry();
                    geometryGroup.Children.Add(runGeometry);
                    if (textStyle.TextDecoration != null && textStyle.TextDecoration.Count > 0)
                    {
                        GetTextDecorations(geometryGroup, textStyle, font, baselineOrigin, out List<Rect> backgroundDecorations, out List<Rect> foregroundDecorations);
                        foreach (var decoration in backgroundDecorations)
                        {
                            //Underline and OverLine should be drawn behind the text
                            geometryGroup.Children.Insert(0, new RectangleGeometry(decoration));
                        }
                        foreach (var decoration in foregroundDecorations)
                        {
                            //Strikethrough should be drawn on top of the text
                            geometryGroup.Children.Add(new RectangleGeometry(decoration));
                        }
                    }
                    mainGeometryGroup.Children.Add(geometryGroup);
                }
                baselineOrigin = newBaseline;
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
        /// <param name="geometryGroup"></param>
        /// <param name="textStyle"></param>
        /// <param name="font"></param>
        /// <param name="baselineOrigin"></param>
        /// <param name="backgroundDecorations"></param>
        /// <param name="foregroundDecorations"></param>
        private static void GetTextDecorations(GeometryGroup geometryGroup, TextStyle textStyle, Typeface font, Point baselineOrigin, out List<Rect> backgroundDecorations, out List<Rect> foregroundDecorations)
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
                    Rect bounds = new Rect(geometryGroup.Bounds.Left, decorationPos, geometryGroup.Bounds.Width, decorationThickness);
                    foregroundDecorations.Add(bounds);
                }
                else if (textDecorationLocation == TextDecorationLocation.Underline)
                {
                    decorationPos = baselineY - (font.UnderlinePosition * fontSize);
                    decorationThickness = font.UnderlineThickness * fontSize;
                    Rect bounds = new Rect(geometryGroup.Bounds.Left, decorationPos, geometryGroup.Bounds.Width, decorationThickness);
                    backgroundDecorations.Add(bounds);
                }
                else if (textDecorationLocation == TextDecorationLocation.OverLine)
                {
                    decorationPos = baselineY - fontSize;
                    decorationThickness = font.StrikethroughThickness * fontSize;
                    Rect bounds = new Rect(geometryGroup.Bounds.Left, decorationPos, geometryGroup.Bounds.Width, decorationThickness);
                    backgroundDecorations.Add(bounds);
                }
            }
        }

        private static GlyphRun CreateRun(TextString textString, TextRenderState state, Typeface font, bool isSideways, Point baselineOrigin, out Point newBaseline)
        {
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
            var advanceWidths = glyphIndices.Select(c => glyphFace.AdvanceWidths[c] * renderingEmSize).ToArray();


            if (characterInfos[0].DoesPositionX)
            {
                baselineOrigin.X = characterInfos[0].X;
            }
            if (characterInfos[0].DoesPositionY)
            {
                baselineOrigin.Y = characterInfos[0].Y;
            }

            GlyphRun run = new GlyphRun(glyphFace, state.BidiLevel, isSideways, renderingEmSize,
#if !DOTNET40 && !DOTNET45 && !DOTNET46
                (float)DpiUtil.PixelsPerDip,
#endif
                glyphIndices, baselineOrigin, advanceWidths, glyphOffsets, characters, deviceFontName, clusterMap, caretStops, language);
            
            var newX = baselineOrigin.X + advanceWidths.Sum();
            var newY = baselineOrigin.Y ;

            newBaseline = new Point(newX, newY);
            return run;
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
