using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using SVGImage.SVG.Shapes;

namespace SVGImage.SVG.Utils
{
    internal static class ShapeGeometry
    {
        internal static Geometry Create(Shape shape, HashSet<Shape> active = null)
        {
            active = active ?? new HashSet<Shape>();
            if (shape == null || !shape.Display || shape.Visibility == Visibility.Hidden || !active.Add(shape)) return Geometry.Empty;
            try
            {
                Geometry geometry = Geometry.Empty;
                if (shape is RectangleShape rect && rect.Width > 0 && rect.Height > 0)
                    geometry = new RectangleGeometry(new Rect(rect.X, rect.Y, rect.Width, rect.Height), rect.RX, rect.RY);
                else if (shape is CircleShape circle && circle.R > 0)
                    geometry = new EllipseGeometry(new Point(circle.CX, circle.CY), circle.R, circle.R);
                else if (shape is EllipseShape ellipse && ellipse.RX > 0 && ellipse.RY > 0)
                    geometry = new EllipseGeometry(new Point(ellipse.CX, ellipse.CY), ellipse.RX, ellipse.RY);
                else if (shape is PathShape path)
                {
                    var parsed = SvgPathData.Parse(path.Data);
                    parsed.FillRule = shape.ClipRule;
                    geometry = parsed;
                }
                else if (shape is PolylineShape line) geometry = Points(line.Points, false, shape.ClipRule);
                else if (shape is PolygonShape polygon) geometry = Points(polygon.Points, true, shape.ClipRule);
                else if (shape is TextShape text) geometry = new TextRender().BuildTextGeometry(text) ?? Geometry.Empty;
                else if (shape is UseShape use)
                {
                    var target = shape.Svg.GetShape(use.hRef);
                    geometry = Create(target, active).CloneCurrentValue();
                    Matrix matrix = target is Symbol symbol && symbol.ViewBox.HasValue ? symbol.Viewport(use.X, use.Y, use.Width, use.Height) : new Matrix(1, 0, 0, 1, use.X, use.Y);
                    matrix.Prepend(geometry.Transform?.Value ?? Matrix.Identity);
                    geometry.Transform = new MatrixTransform(matrix);
                }
                else if (shape is Group group)
                {
                    foreach (var child in group.Elements)
                        geometry = new CombinedGeometry(GeometryCombineMode.Union, geometry, Create(child, active));
                }
                if (shape.Clip != null)
                    geometry = new CombinedGeometry(GeometryCombineMode.Intersect, geometry, shape.Clip.GetGeometry(geometry.Bounds, active));
                if (shape.Transform != null)
                {
                    geometry = geometry.CloneCurrentValue();
                    Matrix matrix = geometry.Transform?.Value ?? Matrix.Identity;
                    matrix.Append(shape.Transform.Value);
                    geometry.Transform = new MatrixTransform(matrix);
                }
                return geometry;
            }
            catch (FormatException) { return Geometry.Empty; }
            finally { active.Remove(shape); }
        }
        private static Geometry Points(Point[] points, bool closed, FillRule rule)
        {
            if (points == null || points.Length == 0) return Geometry.Empty;
            var path = new PathGeometry { FillRule = rule };
            var figure = new PathFigure { StartPoint = points[0], IsClosed = closed, IsFilled = true };
            for (int i = 1; i < points.Length; i++) figure.Segments.Add(new LineSegment(points[i], true));
            path.Figures.Add(figure);
            return path;
        }
    }
}
