using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using SVGImage.SVG.Shapes;

namespace SVGImage.SVG.Utils
{
    internal static class TextPathLayout
    {
        internal static TextPath Find(Shape shape)
        {
            for (; shape != null; shape = shape.Parent) if (shape is TextPath path) return path;
            return null;
        }
        internal static Geometry Build(GlyphRun run, TextPath textPath, double anchorOffset)
        {
            var target = textPath.Svg.GetShape(textPath.Href);
            if (!(target is PathShape)) return Geometry.Empty;
            var geometry = PathGeometry.CreateFromGeometry(ShapeGeometry.Create(target));
            double length = Length(geometry);
            if (length <= 0) return Geometry.Empty;
            double offset = XmlUtil.GetDoubleValue(textPath.StartOffset, length) + run.BaselineOrigin.X - anchorOffset;
            var result = new GeometryGroup { FillRule = FillRule.Nonzero };
            for (int i = 0; i < run.GlyphIndices.Count; i++)
            {
                double advance = run.AdvanceWidths[i];
                double distance = offset + advance / 2;
                if (distance >= 0 && distance <= length)
                {
                    geometry.GetPointAtFractionLength(distance / length, out Point point, out Point tangent);
                    var glyph = run.GlyphTypeface.GetGlyphOutline(run.GlyphIndices[i], run.FontRenderingEmSize, run.FontRenderingEmSize).CloneCurrentValue();
                    var shift = run.GlyphOffsets != null && i < run.GlyphOffsets.Count ? run.GlyphOffsets[i] : new Point();
                    var matrix = Matrix.Identity;
                    matrix.Translate(-advance / 2 + shift.X, run.BaselineOrigin.Y - shift.Y);
                    matrix.Rotate(Math.Atan2(tangent.Y, tangent.X) * 180 / Math.PI);
                    matrix.Translate(point.X, point.Y);
                    glyph.Transform = new MatrixTransform(matrix);
                    result.Children.Add(glyph);
                }
                offset += advance;
            }
            return result;
        }
        private static double Length(PathGeometry geometry)
        {
            double length = 0;
            var flattened = geometry.GetFlattenedPathGeometry(.01, ToleranceType.Absolute);
            foreach (var figure in flattened.Figures)
            {
                var previous = figure.StartPoint;
                foreach (var segment in figure.Segments)
                {
                    if (segment is LineSegment line) { length += (line.Point - previous).Length; previous = line.Point; }
                    else if (segment is PolyLineSegment poly)
                        foreach (var point in poly.Points) { length += (point - previous).Length; previous = point; }
                }
                if (figure.IsClosed) length += (figure.StartPoint - previous).Length;
            }
            return length;
        }
    }
}
