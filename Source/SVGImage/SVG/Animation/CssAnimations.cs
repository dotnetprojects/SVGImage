using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SVGImage.SVG.Shapes;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG.Animation
{
    internal static class CssAnimations
    {
        internal static readonly string[] Longhands = { "animation-name", "animation-duration", "animation-timing-function", "animation-delay",
            "animation-iteration-count", "animation-direction", "animation-fill-mode", "animation-play-state" };
        internal static readonly string[] Defaults = { "none", "0s", "ease", "0s", "1", "normal", "none", "running" };

        internal static Dictionary<string, string> Expand(string shorthand)
        {
            var columns = Longhands.Select(p => new List<string>()).ToArray();
            foreach (string item in StyleParser.SplitTopLevel(shorthand, ','))
            {
                var values = (string[])Defaults.Clone();
                int time = 0;
                foreach (string token in Regex.Matches(item.Trim(), @"[\w-]+\([^)]*\)|[^\s]+").Cast<Match>().Select(m => m.Value))
                {
                    if (Regex.IsMatch(token, @"^[+-]?(?:[\d.]+)(?:ms|s)$")) { if (time < 2) values[time++ == 0 ? 1 : 3] = token; }
                    else if (token == "infinite" || Regex.IsMatch(token, @"^[\d.]+$")) values[4] = token;
                    else if (new[] { "normal", "reverse", "alternate", "alternate-reverse" }.Contains(token)) values[5] = token;
                    else if (new[] { "forwards", "backwards", "both" }.Contains(token)) values[6] = token;
                    else if (token == "none") values[6] = token;
                    else if (token == "paused" || token == "running") values[7] = token;
                    else if (IsTiming(token)) values[2] = token;
                    else values[0] = token.Trim('\'', '"');
                }
                for (int i = 0; i < values.Length; i++) columns[i].Add(values[i]);
            }
            return Longhands.Select((p, i) => new { p, value = string.Join(",", columns[i]) }).ToDictionary(p => p.p, p => p.value);
        }

        private static bool IsTiming(string value) => new[] { "linear", "ease", "ease-in", "ease-out", "ease-in-out", "step-start", "step-end" }.Contains(value) ||
            value.StartsWith("steps(") || value.StartsWith("cubic-bezier(");

        internal static IEnumerable<AnimationTrack> Build(SVG svg, Shape shape)
        {
            var properties = new Dictionary<string, string>();
            string shorthand;
            if (shape.CssProperties.TryGetValue("animation", out shorthand))
                foreach (var p in Expand(shorthand)) properties[p.Key] = p.Value;
            foreach (var p in shape.CssProperties.Where(p => Longhands.Contains(p.Key))) properties[p.Key] = p.Value;
            Func<int, int, string> get = (property, index) =>
            {
                string text;
                if (!properties.TryGetValue(Longhands[property], out text)) return Defaults[property];
                var list = StyleParser.SplitTopLevel(text, ',');
                return list[index % list.Count].Trim();
            };
            var names = StyleParser.SplitTopLevel(get(0, 0), ',');
            string nameList;
            if (properties.TryGetValue("animation-name", out nameList)) names = StyleParser.SplitTopLevel(nameList, ',');
            for (int animation = 0; animation < names.Count; animation++)
            {
                string definition;
                if (!svg.CssKeyframes.TryGetValue(names[animation].Trim().Trim('\'', '"'), out definition)) continue;
                var frames = new SortedDictionary<double, Dictionary<string, string>>();
                foreach (var frame in StyleParser.Blocks(definition))
                    foreach (string key in StyleParser.SplitTopLevel(frame.Name, ','))
                    {
                        double offset;
                        string token = key.Trim();
                        if (token == "from") offset = 0;
                        else if (token == "to") offset = 1;
                        else if (!token.EndsWith("%") || !double.TryParse(token.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out offset)) continue;
                        else offset /= 100;
                        if (offset < 0 || offset > 1) continue;
                        if (!frames.ContainsKey(offset)) frames[offset] = new Dictionary<string, string>();
                        foreach (var p in StyleParser.SplitStyle(svg, frame.Value))
                            if (!Regex.IsMatch(p.Value, @"!\s*important\s*$", RegexOptions.IgnoreCase))
                            {
                                string value = CssStyleSheet.Substitute(p.Value, name => shape.CssProperties.ContainsKey(name) ? shape.CssProperties[name] : null);
                                if (value != null) frames[offset][p.Name] = value;
                            }
                    }
                foreach (string property in new[] { "transform", "opacity" })
                {
                    var selected = new SortedDictionary<double, string>();
                    foreach (var frame in frames)
                        if (frame.Value.ContainsKey(property)) selected[frame.Key] = frame.Value[property];
                    if (selected.Count == 0) continue;
                    string underlying = property == "transform" ? "__underlying__" : shape.Opacity.ToString(CultureInfo.InvariantCulture);
                    if (!selected.ContainsKey(0)) selected[0] = underlying;
                    if (!selected.ContainsKey(1)) selected[1] = underlying;
                    double count;
                    string repeat = get(4, animation);
                    if (repeat == "infinite") count = double.PositiveInfinity;
                    else if (!double.TryParse(repeat, NumberStyles.Float, CultureInfo.InvariantCulture, out count)) continue;
                    string fill = get(6, animation);
                    yield return new AnimationTrack
                    {
                        IsCss = true, Attribute = property, Type = property == "transform" ? "css" : null,
                        Begin = "0s", Duration = AnimationTrack.Clock(get(1, animation)), Delay = AnimationTrack.Clock(get(3, animation)),
                        Repeat = count, Direction = get(5, animation), Freeze = fill == "forwards" || fill == "both",
                        Backwards = fill == "backwards" || fill == "both", Paused = get(7, animation) == "paused",
                        Mode = "linear", Values = selected.Values.ToArray(), Times = selected.Keys.ToArray(),
                        CssTiming = selected.Keys.Select(t => frames.ContainsKey(t) && frames[t].ContainsKey("animation-timing-function")
                            ? frames[t]["animation-timing-function"] : get(2, animation)).ToArray()
                    };
                }
            }
        }

        internal static double Ease(string function, double progress, bool before)
        {
            var values = AnimationTrack.Numbers(function);
            if (function == "step-start") { function = "steps(1,start)"; values = new[] { 1.0 }; }
            if (function == "step-end") { function = "steps(1,end)"; values = new[] { 1.0 }; }
            if (function.StartsWith("steps("))
            {
                if (values.Length == 0 || values[0] < 1 || values[0] != Math.Floor(values[0])) return progress;
                double steps = values[0];
                bool start = function.Contains("start") || function.Contains("jump-both");
                double current = Math.Floor(progress * steps) + (start ? 1 : 0);
                if (before && progress * steps == Math.Floor(progress * steps)) current--;
                double jumps = steps + (function.Contains("jump-both") ? 1 : function.Contains("jump-none") ? -1 : 0);
                return jumps <= 0 ? progress : Math.Max(0, Math.Min(1, current / jumps));
            }
            if (function == "linear") return progress;
            if (function == "ease") values = new[] { .25, .1, .25, 1 };
            if (function == "ease-in") values = new[] { .42, 0, 1, 1 };
            if (function == "ease-out") values = new[] { 0, 0, .58, 1 };
            if (function == "ease-in-out") values = new[] { .42, 0, .58, 1 };
            if (values.Length != 4 || values[0] < 0 || values[0] > 1 || values[2] < 0 || values[2] > 1) return progress;
            double low = 0, high = 1, t = progress;
            for (int i = 0; i < 35; i++)
            {
                t = (low + high) / 2;
                double x = Bezier(t, values[0], values[2]);
                if (x < progress) low = t; else high = t;
            }
            return progress == 0 || progress == 1 ? progress : Bezier(t, values[1], values[3]);
        }
        private static double Bezier(double t, double a, double b) => 3 * (1 - t) * (1 - t) * t * a + 3 * (1 - t) * t * t * b + t * t * t;
    }
}
