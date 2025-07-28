using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SVGImage.SVG.Shapes
{
    public sealed partial class TextRender
    {
        /// <summary>
        /// Represents the state of the text rendering process, including character layout and position resolution.
        /// This class is responsible for setting up the text layout, resolving positions and transformations, and applying them to the characters.
        /// </summary>
        private sealed class TextRenderState : IDisposable
        {
            private bool _disposedValue;
            private CharacterLayout[] _characters;
            private double[] _resolvedX;
            private double[] _resolvedY;
            private double[] _resolvedDx;
            private double[] _resolvedDy;
            private double[] _resolvedRotate;
            private int[] _xBaseIndicies;
            private int[] _yBaseIndicies;
            public int BidiLevel { get; private set; } = 0;
            private void ResetState()
            {
                _characters = null;
                _resolvedX = null;
                _resolvedY = null;
                _resolvedDx = null;
                _resolvedDy = null;
                _resolvedRotate = null;
                _xBaseIndicies = null;
                _yBaseIndicies = null;
            }
            /// <summary>
            /// Initializes the text render state with the root TextShape.
            /// </summary>
            /// <param name="root">
            /// The root TextShape to process. This should contain all the text nodes and their children.
            /// </param>
            /// <returns>
            /// Returns true if the setup was successful, false if there were no characters to process.
            /// </returns>
            public bool Setup(TextShape root)
            {
                string text = root.GetText();
                int globalIndex = 0;
                SetGlobalIndicies(root, ref globalIndex);
                _characters = root.GetCharacters();
                SetFlagsAndAssignInitialPositions(root, text);
                if (_characters.Length == 0)
                {
                    return false;
                }
                return true;
            }
            /// <summary>
            /// Creates an array of a specified length, filled with a specified element.
            /// </summary>
            /// <typeparam name="T">
            /// The type of the element to fill the array with. Must be a struct type.
            /// </typeparam>
            /// <param name="count">
            /// The length of the array to create.
            /// </param>
            /// <param name="element">
            /// The element to fill the array with. This should be a value type (struct).
            /// </param>
            /// <returns>
            /// Returns an array of type T, filled with the specified element.
            /// </returns>
            private static T[] CreateRepeatedArray<T>(int count, T element) where T : struct
            {
                var result = new T[count];
                for (int i = 0; i < count; i++)
                {
                    result[i] = element;
                }
                return result;
            }
            /// <summary>
            /// Initializes the arrays used for resolving text positions and transformations.
            /// </summary>
            /// <param name="length">
            /// The length of the arrays to initialize. This should match the number of characters in the text.
            /// </param>
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

            /// <summary>
            /// Recursively resolves the text span's position and offset values.
            /// </summary>
            /// <param name="textSpan">
            /// The TextShapeBase to resolve. This should contain the text span's position and offset values.
            /// </param>
            public void Resolve(TextShapeBase textSpan)
            {
                string text = textSpan.GetText();
                InitializeResolveArrays(text.Length);
                ResolveInternal(textSpan);
                ApplyResolutions();
            }

            /// <summary>
            /// Recursively resolves the text span's position and offset values.
            /// </summary>
            /// <param name="textSpan">
            /// The TextShapeBase to resolve. This should contain the text span's position and offset values.
            /// </param>
            private void ResolveInternal(TextShapeBase textSpan)
            {
                int index = textSpan.GetFirstCharacter().GlobalIndex;
                LengthPercentageOrNumberList x = textSpan.X;
                LengthPercentageOrNumberList y = textSpan.Y;
                LengthPercentageOrNumberList dx = textSpan.DX;
                LengthPercentageOrNumberList dy = textSpan.DY;
                List<double> rotate = textSpan.Rotate;

                var arrays = new List<Tuple<LengthPercentageOrNumberList, double[]>>();
                arrays.Add(new Tuple<LengthPercentageOrNumberList, double[]>(x, _resolvedX));
                arrays.Add(new Tuple<LengthPercentageOrNumberList, double[]>(y, _resolvedY));
                arrays.Add(new Tuple<LengthPercentageOrNumberList, double[]>(dx, _resolvedDx));
                arrays.Add(new Tuple<LengthPercentageOrNumberList, double[]>(dy, _resolvedDy));

                foreach (var tuple in arrays)
                {
                    var list = tuple.Item1;
                    var resolvedArray = tuple.Item2;
                    for (int i = 0; i < list.Count; i++)
                    {
                        // Check if the index is within bounds of the resolved array
                        if (index + i >= resolvedArray.Length)
                        {
                            break;
                        }
                        resolvedArray[index + i] = list[i].Value;
                    }
                }

                for (int i = 0; i < rotate.Count; i++)
                {
                    // Check if the index is within bounds of the resolved array
                    if (index + i >= _resolvedRotate.Length)
                    {
                        break;
                    }
                    _resolvedRotate[index + i] = rotate[i];
                }
                foreach (var child in textSpan.Children.OfType<TextShapeBase>())
                {
                    ResolveInternal(child);
                }
            }
            /// <summary>
            /// Recursively sets the global indices for each character in the text node.
            /// </summary>
            /// <param name="textNode">
            /// The text node to process. This should be a <see cref="TextShapeBase"/> or <see cref="TextString"/> containing characters.
            /// </param>
            /// <param name="globalIndex">
            /// A reference to the global index counter. This will be incremented for each character processed.
            /// </param>
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
            /// <summary>
            /// Fills in gaps in the resolved position and rotation arrays.
            /// </summary>
            private void FillInGaps()
            {
                FillInGaps(_resolvedX, 0d, _xBaseIndicies);
                FillInGaps(_resolvedY, 0d, _yBaseIndicies);
                FillInGaps(_resolvedRotate, 0d);
            }
            /// <summary>
            /// Fills in gaps in the specified list with the initial value or the last known value.
            /// </summary>
            /// <param name="list">
            /// The list to fill in gaps for. This should be an array of doubles.
            /// </param>
            /// <param name="initialValue">
            /// The initial value to use for filling gaps. If null, the first value in the list will be used, even if it is NaN.
            /// </param>
            /// <param name="baseIndicies">
            /// An optional array of base indices to track the last known index for each position in the list.
            /// </param>
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
            /// <summary>
            /// Applies the resolved positions and transformations to the characters.
            /// </summary>
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
            public void SetFlagsAndAssignInitialPositions(TextShape root, string text)
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
                        ResetState();
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

    }
}
