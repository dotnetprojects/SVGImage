using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using SVGImage.SVG.Animation;

namespace SVGImage.SVG.Utils
{
    // A deliberately bounded SVG author-style cascade. Unsupported selectors and
    // conditional rules are ignored as a whole, never flattened into global rules.
    internal sealed class CssStyleSheet
    {
        private sealed class Rule
        {
            internal CssSelector Selector;
            internal List<StyleItem> Declarations;
        }
        private sealed class Candidate
        {
            internal string Value;
            internal bool Important;
            internal int Specificity, Order;
        }
        private readonly List<Rule> rules = new List<Rule>();
        private readonly Dictionary<XmlNode, Dictionary<string, string>> cache = new Dictionary<XmlNode, Dictionary<string, string>>();
        private static readonly HashSet<string> Presentation = new HashSet<string>((
            "fill fill-rule fill-opacity stroke stroke-width stroke-opacity stroke-dasharray stroke-linecap stroke-linejoin " +
            "opacity color display visibility transform mask mask-type clip-path color-interpolation filter " +
            "font-family font-size font-weight font-style text-decoration text-anchor word-spacing letter-spacing baseline-shift " +
            "stop-color stop-opacity clip-rule overflow color-interpolation-filters flood-color flood-opacity").Split(' '));
        private static readonly Dictionary<string, string> Initial = new Dictionary<string, string>
        {
            { "fill", "black" }, { "stroke", "none" }, { "color", "black" }, { "opacity", "1" },
            { "fill-opacity", "1" }, { "stroke-opacity", "1" }, { "stroke-width", "1" },
            { "display", "inline" }, { "visibility", "visible" }, { "transform", "none" },
            { "animation", "none" }, { "fill-rule", "nonzero" }, { "stop-color", "black" }, { "stop-opacity", "1" }
        };
        static CssStyleSheet()
        {
            for (int i = 0; i < CssAnimations.Longhands.Length; i++) Initial[CssAnimations.Longhands[i]] = CssAnimations.Defaults[i];
        }
        private static bool Inherited(string name) => name == "color" || name.StartsWith("fill") || name.StartsWith("stroke") ||
            name.StartsWith("font-") || name == "visibility" || name == "clip-rule" || name == "color-interpolation-filters" || name == "text-anchor" || name == "word-spacing" || name == "letter-spacing";

        internal void Add(string selector, List<StyleItem> declarations)
        {
            var parsed = CssSelector.Parse(selector);
            if (parsed != null) rules.Add(new Rule { Selector = parsed, Declarations = declarations });
            cache.Clear();
        }

        internal Dictionary<string, string> Resolve(XmlNode node)
        {
            Dictionary<string, string> result;
            if (node == null || node.NodeType != XmlNodeType.Element) return new Dictionary<string, string>();
            if (cache.TryGetValue(node, out result)) return result;
            var winners = new Dictionary<string, Candidate>();
            int order = 0;
            Action<string, string, int, bool> add = null;
            add = (name, value, specificity, allowImportant) =>
            {
                name = name.StartsWith("--") ? name : name.ToLowerInvariant();
                bool important = allowImportant && Regex.IsMatch(value, @"!\s*important\s*$", RegexOptions.IgnoreCase);
                if (important) value = Regex.Replace(value, @"!\s*important\s*$", "", RegexOptions.IgnoreCase).Trim();
                if (name == "animation")
                {
                    var expanded = CssAnimations.Expand(value);
                    foreach (string property in CssAnimations.Longhands)
                    {
                        string item = value == "inherit" || value == "initial" || value == "unset" ? value :
                            value.Contains("var(") ? "__animation_var__" + value : expanded[property];
                        add(property, item + (important ? " !important" : ""), specificity, true);
                    }
                }
                var candidate = new Candidate { Value = value, Important = important, Specificity = specificity, Order = order++ };
                Candidate old;
                if (!winners.TryGetValue(name, out old) || (important && !old.Important) ||
                    (important == old.Important && (specificity > old.Specificity || specificity == old.Specificity && candidate.Order > old.Order)))
                    winners[name] = candidate;
            };
            foreach (XmlAttribute attribute in node.Attributes)
                if (Presentation.Contains(attribute.LocalName)) add(attribute.LocalName, attribute.Value, 0, false);
            foreach (var rule in rules)
                if (rule.Selector.Matches(node))
                    foreach (var declaration in rule.Declarations) add(declaration.Name, declaration.Value, rule.Selector.Specificity, true);
            foreach (var declaration in StyleParser.SplitStyle(null, XmlUtil.AttrValue(node, "style")))
                add(declaration.Name, declaration.Value, 1000000000, true);

            var parent = Resolve(node.ParentNode);
            result = parent.Where(p => p.Key.StartsWith("--")).ToDictionary(p => p.Key, p => p.Value);
            var custom = winners.Where(p => p.Key.StartsWith("--")).ToDictionary(p => p.Key, p => p.Value.Value);
            var resolved = new Dictionary<string, string>();
            var active = new HashSet<string>();
            var stack = new List<string>();
            var cycles = new HashSet<string>();
            Func<string, string> variable = null;
            variable = name =>
            {
                string value;
                if (resolved.TryGetValue(name, out value)) return value;
                if (!custom.TryGetValue(name, out value)) return result.TryGetValue(name, out value) ? value : null;
                if (active.Contains(name)) { foreach (var member in stack.Skip(stack.IndexOf(name))) cycles.Add(member); return null; }
                active.Add(name);
                stack.Add(name);
                if (value == "inherit" || value == "unset") value = parent.ContainsKey(name) ? parent[name] : null;
                else if (value == "initial") value = null;
                else value = Substitute(value, variable);
                active.Remove(name);
                stack.RemoveAt(stack.Count - 1);
                if (cycles.Contains(name)) value = null;
                resolved[name] = value;
                return value;
            };
            foreach (string name in custom.Keys)
            {
                string value = variable(name);
                if (value != null) result[name] = value;
                else result.Remove(name);
            }
            foreach (var winner in winners.Where(p => !p.Key.StartsWith("--")))
            {
                string value = Substitute(winner.Value.Value, variable);
                if (value != null && value.StartsWith("__animation_var__"))
                    value = CssAnimations.Expand(value.Substring("__animation_var__".Length))[winner.Key];
                if (value == "inherit" || value == "unset" && Inherited(winner.Key))
                    value = AncestorValue(node.ParentNode, winner.Key);
                else if (value == "initial" || value == "unset")
                    value = Initial.ContainsKey(winner.Key) ? Initial[winner.Key] : null;
                // Invalid var() declarations become unset, rather than reviving a
                // lower-priority declaration from the cascade.
                if (value == null && Inherited(winner.Key)) value = AncestorValue(node.ParentNode, winner.Key);
                if (value == null && Initial.ContainsKey(winner.Key)) value = Initial[winner.Key];
                if (value != null) result[winner.Key] = value;
            }
            cache[node] = result;
            return result;
        }

        private string AncestorValue(XmlNode node, string property)
        {
            for (; node != null && node.NodeType == XmlNodeType.Element; node = node.ParentNode)
            {
                string value;
                if (Resolve(node).TryGetValue(property, out value)) return value;
            }
            return Initial.ContainsKey(property) ? Initial[property] : null;
        }

        internal static string Substitute(string value, Func<string, string> lookup)
        {
            int start = value.IndexOf("var(", StringComparison.Ordinal);
            while (start >= 0)
            {
                int end = StyleParser.Closing(value, start + 3, '(', ')');
                if (end < 0) return null;
                var args = StyleParser.SplitTopLevel(value.Substring(start + 4, end - start - 4), ',');
                string name = args[0].Trim();
                if (!Regex.IsMatch(name, @"^--[\w-]+$")) return null;
                string replacement = lookup(name);
                if (replacement == null && args.Count > 1)
                    replacement = Substitute(string.Join(",", args.Skip(1)), lookup);
                if (replacement == null) return null;
                value = value.Substring(0, start) + replacement + value.Substring(end + 1);
                start = value.IndexOf("var(", StringComparison.Ordinal);
            }
            return value;
        }
    }

    internal sealed class CssSelector
    {
        private string[] parts;
        private char[] relations;
        internal int Specificity { get; private set; }
        internal static CssSelector Parse(string text)
        {
            var tokens = new List<string>(); var relations = new List<char>();
            int pos = 0;
            while (pos < text.Length)
            {
                while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
                int start = pos, depth = 0; char quote = '\0';
                while (pos < text.Length)
                {
                    char c = text[pos];
                    if (quote != '\0') { if (c == quote && (pos == 0 || text[pos - 1] != '\\')) quote = '\0'; }
                    else if (c == '\'' || c == '"') quote = c;
                    else if (c == '[') depth++;
                    else if (c == ']') depth--;
                    else if (depth == 0 && (char.IsWhiteSpace(c) || c == '>' || c == '+' || c == '~')) break;
                    pos++;
                }
                if (start == pos || depth != 0 || quote != '\0') return null;
                tokens.Add(text.Substring(start, pos - start));
                while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
                if (pos < text.Length)
                {
                    char relation = text[pos] == '>' || text[pos] == '+' || text[pos] == '~' ? text[pos++] : ' ';
                    relations.Add(relation);
                    if (pos == text.Length) return null;
                }
            }
            if (tokens.Count == 0 || relations.Count != tokens.Count - 1) return null;
            int specificity = 0;
            foreach (var token in tokens)
            {
                string rest = Regex.Replace(token, @"\[[\w-]+(?:\s*=\s*(?:'[^']*'|""[^""]*""|[\w-]+))?\]", "");
                rest = rest.Replace(":root", "");
                if (!Regex.IsMatch(rest, @"^(?:\*|[a-zA-Z_][\w-]*)?(?:[.#][\w-]+)*$")) return null;
                specificity += Regex.Matches(rest, @"#[\w-]+").Count * 1000000;
                specificity += (Regex.Matches(rest, @"\.[\w-]+").Count + Regex.Matches(token, @"\[").Count + (token.Contains(":root") ? 1 : 0)) * 1000;
                if (Regex.IsMatch(rest, @"^[a-zA-Z_]")) specificity++;
            }
            return new CssSelector { parts = tokens.ToArray(), relations = relations.ToArray(), Specificity = specificity };
        }

        internal bool Matches(XmlNode node) => Match(node, parts.Length - 1);
        private bool Match(XmlNode node, int index)
        {
            if (node == null || node.NodeType != XmlNodeType.Element || !Compound(node, parts[index])) return false;
            if (index == 0) return true;
            switch (relations[index - 1])
            {
                case '>': return Match(node.ParentNode, index - 1);
                case '+': return Match(Previous(node), index - 1);
                case '~': for (var p = Previous(node); p != null; p = Previous(p)) if (Match(p, index - 1)) return true; return false;
                default: for (var p = node.ParentNode; p != null; p = p.ParentNode) if (Match(p, index - 1)) return true; return false;
            }
        }
        private static XmlNode Previous(XmlNode node)
        {
            do { node = node.PreviousSibling; } while (node != null && node.NodeType != XmlNodeType.Element);
            return node;
        }
        private static bool Compound(XmlNode node, string selector)
        {
            if (selector.Contains(":root") && node.ParentNode?.NodeType == XmlNodeType.Element) return false;
            var type = Regex.Match(selector, @"^[a-zA-Z_][\w-]*");
            if (type.Success && node.LocalName != type.Value) return false;
            string plain = Regex.Replace(selector, @"\[[^\]]*\]", "");
            foreach (Match id in Regex.Matches(plain, @"#([\w-]+)"))
                if (XmlUtil.AttrValue(node, "id") != id.Groups[1].Value) return false;
            var classes = Regex.Split(XmlUtil.AttrValue(node, "class"), @"\s+");
            // Remove attribute contents so dots/hashes in quoted values are not selectors.
            foreach (Match cls in Regex.Matches(plain, @"\.([\w-]+)"))
                if (!classes.Contains(cls.Groups[1].Value)) return false;
            foreach (Match attr in Regex.Matches(selector, @"\[([\w-]+)(?:\s*=\s*('[^']*'|""[^""]*""|[\w-]+))?\]"))
            {
                var value = node.Attributes[attr.Groups[1].Value];
                if (value == null || attr.Groups[2].Success && value.Value != attr.Groups[2].Value.Trim('\'', '"')) return false;
            }
            return true;
        }
    }
}
