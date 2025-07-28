using System;
using System.Collections.Generic;

namespace SVGImage.SVG.Utils
{
    internal sealed class TextStyleStack
    {
        private readonly Stack<TextStyle> _stack = new Stack<TextStyle>();
        internal void Push(TextStyle textStyle)
        {
            if (textStyle == null)
            {
                throw new ArgumentNullException(nameof(textStyle), $"{nameof(textStyle)} cannot be null.");
            }
            if (_stack.Count == 0)
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




}
