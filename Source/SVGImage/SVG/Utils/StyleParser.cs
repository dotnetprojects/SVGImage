using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SVGImage.SVG.Utils
{
    internal static class StyleParser
    {
        private static Regex _regexStyle =
            new Regex("([\\.,<>a-zA-Z0-9: \\-#]*){([^}]*)}", RegexOptions.Compiled | RegexOptions.Singleline);

        public static List<StyleItem> SplitStyle(SVG svg, string fullstyle)
        {
            List<StyleItem> list = new List<StyleItem>();
            if (fullstyle.Length == 0)
                return list;
            // style contains attributes in format of "attrname:value;attrname:value"
            string[] attrs = fullstyle.Split(';');
            foreach (string attr in attrs)
            {
                string[] s = attr.Split(':');
                if (s.Length != 2)
                    continue;
                list.Add(new StyleItem(s[0].Trim(), s[1].Trim()));
            }
            return list;
        }

        public static void ParseStyle(SVG svg, string style)
        {
            style = Regex.Replace(style, @"/\*.*?\*/", "", RegexOptions.Singleline);
            var keyframe = Regex.Match(style, @"@(?:-webkit-)?keyframes\s+([\w-]+)\s*\{");
            while (keyframe.Success)
            {
                int start = keyframe.Index + keyframe.Length, end = start, depth = 1;
                while (end < style.Length && depth > 0)
                {
                    if (style[end] == '{') depth++;
                    if (style[end] == '}') depth--;
                    end++;
                }
                if (depth != 0) break;
                svg.CssKeyframes[keyframe.Groups[1].Value] = style.Substring(start, end - start - 1);
                style = style.Remove(keyframe.Index, end - keyframe.Index);
                keyframe = Regex.Match(style, @"@(?:-webkit-)?keyframes\s+([\w-]+)\s*\{");
            }
            var svgStyles = svg.Styles;

            var match = _regexStyle.Match(style);
            while (match.Success)
            {
                var name  = match.Groups[1].Value.Trim();
                var value = match.Groups[2].Value.Trim();
                foreach (var nm in name.Split(','))
                {
                    if (!svgStyles.ContainsKey(nm))
                    {
                        svgStyles.Add(nm, new List<StyleItem>());
                    }

                    foreach (StyleItem styleitem in SplitStyle(svg, value))
                    {
                        svgStyles[nm].Add(new StyleItem(styleitem.Name, styleitem.Value));
                    }
                }

                match = match.NextMatch();
            }
        }
    }
}
