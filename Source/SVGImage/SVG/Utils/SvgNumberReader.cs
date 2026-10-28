using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;

namespace SVGImage.SVG.Utils
{
    internal sealed class SvgNumberReader
    {
        private static readonly Regex Number = new Regex(@"\G[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?", RegexOptions.CultureInvariant);
        private readonly string text;
        internal int Position;
        internal SvgNumberReader(string text) { this.text = text ?? ""; }
        internal void Skip() { while (Position < text.Length && (char.IsWhiteSpace(text[Position]) || text[Position] == ',')) Position++; }
        internal bool End { get { Skip(); return Position >= text.Length; } }
        internal char Peek { get { Skip(); return Position < text.Length ? text[Position] : '\0'; } }
        internal bool Read(out double value)
        {
            Skip(); value = 0;
            var match = Number.Match(text, Position);
            if (!match.Success || !double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || double.IsInfinity(value) || double.IsNaN(value)) return false;
            Position += match.Length; return true;
        }
        internal bool Flag(out double value)
        {
            Skip(); value = 0;
            if (Peek != '0' && Peek != '1') return false;
            value = text[Position++] - '0'; return true;
        }
        internal static double[] List(string text)
        {
            var reader = new SvgNumberReader(text); var values = new List<double>();
            while (!reader.End) { double value; if (!reader.Read(out value)) return new double[0]; values.Add(value); }
            return values.ToArray();
        }
        internal static Point[] Points(string text)
        {
            var reader = new SvgNumberReader(text); var points = new List<Point>();
            double x, y;
            while (reader.Read(out x) && reader.Read(out y)) points.Add(new Point(x, y));
            return points.ToArray();
        }
    }
}
