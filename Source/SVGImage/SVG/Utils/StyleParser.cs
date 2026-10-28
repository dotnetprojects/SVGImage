using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SVGImage.SVG.Utils
{
    internal static class StyleParser
    {
        internal static List<string> SplitTopLevel(string text, char separator)
        {
            var result = new List<string>();
            int start = 0, depth = 0; char quote = '\0';
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quote != '\0') { if (c == quote && (i == 0 || text[i - 1] != '\\')) quote = '\0'; }
                else if (c == '\'' || c == '"') quote = c;
                else if (c == '(' || c == '[' || c == '{') depth++;
                else if (c == ')' || c == ']' || c == '}') depth--;
                else if (c == separator && depth == 0) { result.Add(text.Substring(start, i - start)); start = i + 1; }
            }
            result.Add(text.Substring(start));
            return result;
        }

        internal static int Closing(string text, int start, char open, char close)
        {
            int depth = 0; char quote = '\0';
            for (int i = start; i < text.Length; i++)
            {
                char c = text[i];
                if (quote != '\0') { if (c == quote && (i == 0 || text[i - 1] != '\\')) quote = '\0'; }
                else if (c == '\'' || c == '"') quote = c;
                else if (c == open) depth++;
                else if (c == close && --depth == 0) return i;
            }
            return -1;
        }

        private static string WithoutComments(string text)
        {
            var result = new StringBuilder(); char quote = '\0';
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quote != '\0') { if (c == quote && (i == 0 || text[i - 1] != '\\')) quote = '\0'; }
                else if (c == '\'' || c == '"') quote = c;
                else if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
                {
                    int end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    if (end < 0) break;
                    i = end + 1; result.Append(' '); continue;
                }
                result.Append(c);
            }
            return result.ToString();
        }

        public static List<StyleItem> SplitStyle(SVG svg, string fullstyle)
        {
            var result = new List<StyleItem>();
            foreach (string declaration in SplitTopLevel(WithoutComments(fullstyle), ';'))
            {
                var parts = SplitTopLevel(declaration, ':');
                if (parts.Count < 2) continue;
                string name = parts[0].Trim();
                string value = string.Join(":", parts.GetRange(1, parts.Count - 1)).Trim();
                if (!Regex.IsMatch(name, @"^(?:--)?[a-zA-Z_][\w-]*$")) continue;
                if (value.Length == 0 && !name.StartsWith("--")) continue;
                result.Add(new StyleItem(name.StartsWith("--") ? name : name.ToLowerInvariant(), value));
            }
            return result;
        }

        internal static IEnumerable<StyleItem> Blocks(string style)
        {
            style = WithoutComments(style);
            int position = 0;
            while (position < style.Length)
            {
                int start = style.IndexOf('{', position);
                int semicolon = style.IndexOf(';', position);
                if (semicolon >= 0 && (start < 0 || semicolon < start)) { position = semicolon + 1; continue; }
                if (start < 0) yield break;
                int end = Closing(style, start, '{', '}');
                if (end < 0) yield break;
                yield return new StyleItem(style.Substring(position, start - position).Trim(), style.Substring(start + 1, end - start - 1));
                position = end + 1;
            }
        }

        public static void ParseStyle(SVG svg, string style)
        {
            foreach (var block in Blocks(style))
            {
                var keyframe = Regex.Match(block.Name, @"^@(?:-webkit-)?keyframes\s+([\w-]+)$");
                if (keyframe.Success) svg.CssKeyframes[keyframe.Groups[1].Value] = block.Value;
                else if (!block.Name.StartsWith("@"))
                    foreach (string selector in SplitTopLevel(block.Name, ','))
                        svg.StyleSheet.Add(selector.Trim(), SplitStyle(svg, block.Value));
            }
        }
    }
}
