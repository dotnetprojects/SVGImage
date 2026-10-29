using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace SVGImage.SVG.Utils
{
    internal static class CssColor
    {
        internal static bool TryParse(string text, out Color color)
        {
            color = Colors.Black;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim().ToLowerInvariant();
            try
            {
                if (text[0] == '#')
                {
                    string hex = text.Substring(1);
                    if (hex.Length == 3 || hex.Length == 4) hex = string.Concat(System.Linq.Enumerable.Select(hex, c => new string(c, 2)));
                    if (hex.Length != 6 && hex.Length != 8) return false;
                    uint value = uint.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    color = hex.Length == 6 ? Color.FromRgb((byte)(value >> 16), (byte)(value >> 8), (byte)value) : Color.FromArgb((byte)value, (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8));
                    return true;
                }
                var match = Regex.Match(text, @"\A(rgba?|hsla?)\((.*)\)\z", RegexOptions.Singleline);
                if (match.Success)
                {
                    string body = match.Groups[2].Value.Trim();
                    bool legacy = body.IndexOf(',') >= 0;
                    string[] values;
                    if (legacy)
                    {
                        if (body.IndexOf('/') >= 0) return false;
                        values = Regex.Split(body, @"\s*,\s*");
                        if (values.Length != 3 && values.Length != 4) return false;
                        // Legacy comma RGB requires homogeneous channel units.
                        // Modern whitespace RGB may mix numbers and percentages.
                        if (match.Groups[1].Value.StartsWith("rgb") &&
                            (values[0].EndsWith("%") != values[1].EndsWith("%") ||
                             values[0].EndsWith("%") != values[2].EndsWith("%"))) return false;
                    }
                    else
                    {
                        var parts = body.Split('/');
                        if (parts.Length > 2) return false;
                        var channels = Regex.Split(parts[0].Trim(), @"\s+");
                        if (channels.Length != 3) return false;
                        values = parts.Length == 1 ? channels : new[] { channels[0], channels[1], channels[2], parts[1].Trim() };
                    }
                    double alpha = values.Length == 4 ? Unit(values[3], 1) : 1;
                    if (match.Groups[1].Value.StartsWith("rgb"))
                        color = Color.FromArgb(Byte(alpha), Byte(Unit(values[0], 255)), Byte(Unit(values[1], 255)), Byte(Unit(values[2], 255)));
                    else
                    {
                        string hue = values[0]; double multiplier = 1;
                        if (hue.EndsWith("turn")) { multiplier = 360; hue = hue.Substring(0, hue.Length - 4); }
                        else if (hue.EndsWith("grad")) { multiplier = .9; hue = hue.Substring(0, hue.Length - 4); }
                        else if (hue.EndsWith("rad")) { multiplier = 180 / Math.PI; hue = hue.Substring(0, hue.Length - 3); }
                        else if (hue.EndsWith("deg")) hue = hue.Substring(0, hue.Length - 3);
                        double h = ((Number(hue) * multiplier % 360) + 360) % 360 / 60;
                        double s = Clamp(Unit(values[1], 1)), l = Clamp(Unit(values[2], 1));
                        double chroma = (1 - Math.Abs(2 * l - 1)) * s, x = chroma * (1 - Math.Abs(h % 2 - 1)), m = l - chroma / 2;
                        double r = h < 1 ? chroma : h < 2 ? x : h < 4 ? 0 : h < 5 ? x : chroma;
                        double g = h < 1 ? x : h < 3 ? chroma : h < 4 ? x : 0;
                        double b = h < 2 ? 0 : h < 3 ? x : h < 5 ? chroma : x;
                        color = Color.FromArgb(Byte(alpha), Byte(r + m), Byte(g + m), Byte(b + m));
                    }
                    return true;
                }
                if (!Regex.IsMatch(text, @"^[a-z]+$")) return false;
                if (text == "rebeccapurple") { color = Color.FromRgb(102, 51, 153); return true; }
                color = (Color)ColorConverter.ConvertFromString(text.Replace("grey", "gray"));
                return true;
            }
            catch (Exception e) when (e is FormatException || e is ArgumentException || e is OverflowException || e is NotSupportedException) { return false; }
        }
        private static double Number(string value) => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        private static double Unit(string value, double maximum) => value.EndsWith("%") ? Number(value.Substring(0, value.Length - 1)) / 100 : Number(value) / maximum;
        private static double Clamp(double value) => Math.Max(0, Math.Min(1, value));
        private static byte Byte(double value) => (byte)Math.Round(255 * Clamp(value));
    }
}
