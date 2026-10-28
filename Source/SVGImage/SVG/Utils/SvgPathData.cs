using System;
using System.Windows;
using System.Windows.Media;

namespace SVGImage.SVG.Utils
{
    // SVG path parsing stops at an invalid segment and retains completed segments.
    // Arc flags are single tokens, including adjacent 0/1 flags without whitespace.
    internal static class SvgPathData
    {
        internal static PathGeometry Parse(string data)
        {
            var result = new PathGeometry { FillRule = FillRule.Nonzero };
            var reader = new SvgNumberReader(data);
            Point current = new Point(), start = current, control = current;
            PathFigure figure = null;
            char command = '\0', previous = '\0';
            while (!reader.End)
            {
                char next = reader.Peek;
                if (char.IsLetter(next)) { command = next; reader.Position++; }
                char upper = char.ToUpperInvariant(command);
                bool relative = char.IsLower(command);
                if (result.Figures.Count == 0 && upper != 'M') break;
                if (upper == 'Z')
                {
                    if (figure != null) figure.IsClosed = true;
                    current = start; previous = 'Z'; command = '\0'; continue;
                }
                int count = upper == 'M' || upper == 'L' || upper == 'T' ? 2 : upper == 'H' || upper == 'V' ? 1 : upper == 'C' ? 6 : upper == 'S' || upper == 'Q' ? 4 : upper == 'A' ? 7 : 0;
                if (count == 0) break;
                var values = new double[count]; bool valid = true;
                for (int i = 0; i < count; i++) if (!(upper == 'A' && (i == 3 || i == 4) ? reader.Flag(out values[i]) : reader.Read(out values[i]))) { valid = false; break; }
                if (!valid) break;
                Point end = current;
                if (upper == 'M')
                {
                    current = Position(values, 0, relative, current); start = current;
                    figure = new PathFigure { StartPoint = current, IsFilled = true };
                    result.Figures.Add(figure);
                    command = relative ? 'l' : 'L'; previous = 'M'; continue;
                }
                if (figure == null || figure.IsClosed)
                {
                    figure = new PathFigure { StartPoint = current, IsFilled = true }; result.Figures.Add(figure); start = current;
                }
                switch (upper)
                {
                    case 'L': end = Position(values, 0, relative, current); figure.Segments.Add(new LineSegment(end, true)); break;
                    case 'H': end = new Point(values[0] + (relative ? current.X : 0), current.Y); figure.Segments.Add(new LineSegment(end, true)); break;
                    case 'V': end = new Point(current.X, values[0] + (relative ? current.Y : 0)); figure.Segments.Add(new LineSegment(end, true)); break;
                    case 'C':
                        var first = Position(values, 0, relative, current); control = Position(values, 2, relative, current); end = Position(values, 4, relative, current);
                        figure.Segments.Add(new BezierSegment(first, control, end, true)); break;
                    case 'S':
                        var reflected = previous == 'C' || previous == 'S' ? Reflect(control, current) : current;
                        control = Position(values, 0, relative, current); end = Position(values, 2, relative, current);
                        figure.Segments.Add(new BezierSegment(reflected, control, end, true)); break;
                    case 'Q':
                        control = Position(values, 0, relative, current); end = Position(values, 2, relative, current);
                        figure.Segments.Add(new QuadraticBezierSegment(control, end, true)); break;
                    case 'T':
                        control = previous == 'Q' || previous == 'T' ? Reflect(control, current) : current; end = Position(values, 0, relative, current);
                        figure.Segments.Add(new QuadraticBezierSegment(control, end, true)); break;
                    case 'A':
                        end = Position(values, 5, relative, current);
                        double rx = Math.Abs(values[0]), ry = Math.Abs(values[1]);
                        if (rx == 0 || ry == 0) figure.Segments.Add(new LineSegment(end, true));
                        else if (end != current) figure.Segments.Add(new ArcSegment(end, new Size(rx, ry), values[2], values[3] == 1, values[4] == 1 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, true));
                        break;
                }
                current = end; previous = upper;
            }
            return result;
        }
        private static Point Position(double[] values, int index, bool relative, Point current) => new Point(values[index] + (relative ? current.X : 0), values[index + 1] + (relative ? current.Y : 0));
        private static Point Reflect(Point control, Point current) => new Point(2 * current.X - control.X, 2 * current.Y - control.Y);
    }
}
