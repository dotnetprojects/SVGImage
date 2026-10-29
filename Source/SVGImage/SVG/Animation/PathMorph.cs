using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG.Animation
{
    internal sealed class PathMorph
    {
        private readonly PathGeometry[] frames;
        internal PathMorph(string underlying, AnimationTrack track)
        {
            frames = track.Values.Select(value => SvgPathData.Parse(value == "__underlying__" ? underlying : value)).ToArray();
            if (frames.Any(frame => frame.Figures.Count == 0)) throw new FormatException("Path morph values must contain a valid path.");
            if (track.Mode != "discrete")
                foreach (var frame in frames.Skip(1))
                {
                    if (frame.Figures.Count != frames[0].Figures.Count) throw Incompatible();
                    for (int i = 0; i < frame.Figures.Count; i++)
                    {
                        var a = frames[0].Figures[i]; var b = frame.Figures[i];
                        if (a.IsClosed != b.IsClosed || a.Segments.Count != b.Segments.Count) throw Incompatible();
                        for (int j = 0; j < a.Segments.Count; j++)
                        {
                            if (a.Segments[j].GetType() != b.Segments[j].GetType()) throw Incompatible();
                            if (a.Segments[j] is ArcSegment aa && b.Segments[j] is ArcSegment ab &&
                                (aa.IsLargeArc != ab.IsLargeArc || aa.SweepDirection != ab.SweepDirection)) throw Incompatible();
                        }
                    }
                }
        }
        private static FormatException Incompatible() => new FormatException("Path morph requires matching figures, segment types and arc flags; use calcMode='discrete' for incompatible paths.");
        internal PathFigureCollection Sample(int index, double progress)
        {
            var a = frames[index]; var b = frames[Math.Min(index + 1, frames.Length - 1)];
            if (progress == 0) return a.Figures.Clone();
            var figures = new PathFigureCollection();
            for (int i = 0; i < a.Figures.Count; i++)
            {
                var first = a.Figures[i]; var second = b.Figures[i];
                var figure = new PathFigure { StartPoint = Mix(first.StartPoint, second.StartPoint, progress), IsClosed = first.IsClosed, IsFilled = true };
                for (int j = 0; j < first.Segments.Count; j++)
                {
                    var x = first.Segments[j]; var y = second.Segments[j];
                    if (x is LineSegment line && y is LineSegment line2)
                        figure.Segments.Add(new LineSegment(Mix(line.Point, line2.Point, progress), true));
                    else if (x is BezierSegment cubic && y is BezierSegment cubic2)
                        figure.Segments.Add(new BezierSegment(Mix(cubic.Point1, cubic2.Point1, progress), Mix(cubic.Point2, cubic2.Point2, progress), Mix(cubic.Point3, cubic2.Point3, progress), true));
                    else if (x is QuadraticBezierSegment quad && y is QuadraticBezierSegment quad2)
                        figure.Segments.Add(new QuadraticBezierSegment(Mix(quad.Point1, quad2.Point1, progress), Mix(quad.Point2, quad2.Point2, progress), true));
                    else if (x is ArcSegment arc && y is ArcSegment arc2)
                        figure.Segments.Add(new ArcSegment(Mix(arc.Point, arc2.Point, progress), new Size(Mix(arc.Size.Width, arc2.Size.Width, progress), Mix(arc.Size.Height, arc2.Size.Height, progress)), Mix(arc.RotationAngle, arc2.RotationAngle, progress), arc.IsLargeArc, arc.SweepDirection, true));
                }
                figures.Add(figure);
            }
            return figures;
        }
        private static double Mix(double a, double b, double t) => a + (b - a) * t;
        private static Point Mix(Point a, Point b, double t) => new Point(Mix(a.X,b.X,t), Mix(a.Y,b.Y,t));
    }
}
