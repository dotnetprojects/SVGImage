using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SVGImage.SVG.Utils
{
    internal static class MaskDrawing
    {
        // SVG objectBoundingBox excludes stroke, clipping and filter expansion.
        internal static Rect ObjectBounds(Drawing drawing)
        {
            if (drawing is GeometryDrawing geometry) return geometry.Geometry.Bounds;
            if (drawing is DrawingGroup group)
            {
                Rect bounds = Rect.Empty;
                foreach (var child in group.Children) bounds.Union(ObjectBounds(child));
                if (!bounds.IsEmpty && group.Transform != null) bounds = group.Transform.TransformBounds(bounds);
                return bounds;
            }
            return drawing.Bounds;
        }

        internal static Brush Create(DrawingGroup drawing, Rect region, bool alpha, bool linearRgb, int rasterLimit = 0)
        {
            if (region.IsEmpty) return Brushes.Transparent;
            drawing.ClipGeometry = new RectangleGeometry(region);
            if (alpha || IsWhite(drawing))
            {
                // White luminance equals one. Keep the common icon case fully vector-based.
                return new DrawingBrush(drawing)
                {
                    ViewboxUnits = BrushMappingMode.Absolute, Viewbox = region,
                    ViewportUnits = BrushMappingMode.Absolute, Viewport = region,
                    Stretch = Stretch.Fill, TileMode = TileMode.None
                };
            }

            // Composite first: an opaque black shape over white must erase the mask.
            // Converting individual brushes to alpha before compositing would lose that.
            double longest = Math.Max(region.Width, region.Height);
            double scale = rasterLimit > 0 ? rasterLimit / longest : Math.Min(2048 / longest, Math.Max(1, 1024 / longest));
            int width = Math.Max(1, (int)Math.Ceiling(region.Width * scale));
            int height = Math.Max(1, (int)Math.Ceiling(region.Height * scale));
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.PushTransform(new MatrixTransform(width / region.Width, 0, 0, height / region.Height,
                    -region.X * width / region.Width, -region.Y * height / region.Height));
                dc.DrawDrawing(drawing);
            }
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            int stride = width * 4;
            var pixels = new byte[stride * height];
            bitmap.CopyPixels(pixels, stride, 0);
            for (int i = 0; i < pixels.Length; i += 4)
            {
                double a = pixels[i + 3];
                double luminance = linearRgb && a > 0
                    ? a * (0.2125 * Linear(pixels[i + 2] / a) + 0.7154 * Linear(pixels[i + 1] / a) + 0.0721 * Linear(pixels[i] / a))
                    : 0.2125 * pixels[i + 2] + 0.7154 * pixels[i + 1] + 0.0721 * pixels[i];
                byte value = (byte)Math.Max(0, Math.Min(255, Math.Round(luminance)));
                pixels[i] = pixels[i + 1] = pixels[i + 2] = pixels[i + 3] = value;
            }
            var source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Pbgra32, null, pixels, stride);
            source.Freeze();
            return new ImageBrush(source)
            {
                ViewportUnits = BrushMappingMode.Absolute, Viewport = region,
                Stretch = Stretch.Fill, TileMode = TileMode.None
            };
        }

        private static double Linear(double value)
        {
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        internal static bool WhiteOnly(Drawing drawing) => IsWhite(drawing);

        private static bool IsWhite(Drawing drawing)
        {
            if (drawing is DrawingGroup group)
            {
                foreach (var child in group.Children) if (!IsWhite(child)) return false;
                return true;
            }
            if (drawing is GeometryDrawing geometry)
                return IsWhite(geometry.Brush) && (geometry.Pen == null || IsWhite(geometry.Pen.Brush));
            if (drawing is GlyphRunDrawing glyph) return IsWhite(glyph.ForegroundBrush);
            return false;
        }

        private static bool IsWhite(Brush brush)
        {
            if (brush == null) return true;
            if (brush is SolidColorBrush solid) return IsWhite(solid.Color);
            if (brush is GradientBrush gradient)
            {
                foreach (var stop in gradient.GradientStops) if (!IsWhite(stop.Color)) return false;
                return true;
            }
            if (brush is DrawingBrush drawing) return IsWhite(drawing.Drawing);
            return false;
        }

        private static bool IsWhite(Color color)
        {
            return color.A == 0 || (color.R == 255 && color.G == 255 && color.B == 255);
        }
    }
}
