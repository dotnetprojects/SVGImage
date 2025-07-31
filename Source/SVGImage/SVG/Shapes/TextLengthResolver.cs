using System;

namespace SVGImage.SVG.Shapes
{
    using System.Linq;

    /// <summary>
    /// This is an unfinished scaffold for resolving the 'textLength' attribute in SVG text elements.
    /// I was using the SVG 2.0 specification as a reference, but it is not complete, and it is confusing.
    /// </summary>
    internal class TextLengthResolver
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
