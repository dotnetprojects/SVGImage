using System.Collections.Generic;
using System.Text;

namespace SVGImage.SVG.Utils
{
    internal static class SubAndSuperScriptHelper
    {
        private static readonly Dictionary<char, char> _subscriptMap = new Dictionary<char, char>
        {
            {'0', '₀'}, {'1', '₁'}, {'2', '₂'}, {'3', '₃'}, {'4', '₄'},
            {'5', '₅'}, {'6', '₆'}, {'7', '₇'}, {'8', '₈'}, {'9', '₉'},
            {'+', '₊'}, {'-', '₋'}, {'=', '₌'}
        };
        private static readonly Dictionary<char, char> _superscriptMap = new Dictionary<char, char>
        {
            {'0', '⁰'}, {'1', '¹'}, {'2', '²'}, {'3', '³'}, {'4', '⁴'},
            {'5', '⁵'}, {'6', '⁶'}, {'7', '⁷'}, {'8', '⁸'}, {'9', '⁹'},
            {'+', '⁺'}, {'-', '⁻'}, {'=', '⁼'}
        };
        public static string ToSubscript(this string input)
        {
            return Convert(input, _subscriptMap);
        }
        public static string ToSuperscript(this string input)
        {
            return Convert(input, _superscriptMap);
        }

        private static string Convert(string input, Dictionary<char, char> map)
        {
            var sb = new StringBuilder();
            foreach (var c in input)
            {
                sb.Append(map.TryGetValue(c, out var mappedChar) ? mappedChar : c);
            }
            return sb.ToString();
        }
    }
}
