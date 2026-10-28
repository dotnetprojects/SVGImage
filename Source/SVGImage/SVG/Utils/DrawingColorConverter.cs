using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SVGImage.SVG.Utils
{
    /// <summary>Creates grayscale copies of SVG drawings without changing their geometry or alpha.</summary>
    public static class DrawingColorConverter
    {
        public static Drawing ToGrayscale(Drawing drawing)
        {
            if (drawing == null) return null;
            var copy = drawing.CloneCurrentValue();
            MapColors(copy, Gray);
            return copy;
        }

        internal static void MapColors(Drawing drawing, Func<Color, Color> map)
        {
            if (drawing is DrawingGroup group)
            {
                foreach (var child in group.Children) MapColors(child, map);
            }
            else if (drawing is GeometryDrawing geometry)
            {
                geometry.Brush = ConvertBrush(geometry.Brush, map);
                if (geometry.Pen != null)
                {
                    geometry.Pen = geometry.Pen.CloneCurrentValue();
                    geometry.Pen.Brush = ConvertBrush(geometry.Pen.Brush, map);
                }
            }
            else if (drawing is GlyphRunDrawing glyph)
                glyph.ForegroundBrush = ConvertBrush(glyph.ForegroundBrush, map);
            else if (drawing is ImageDrawing image)
                image.ImageSource = ConvertImage(image.ImageSource, map);
        }

        private static Brush ConvertBrush(Brush brush, Func<Color, Color> map)
        {
            if (brush == null) return null;
            brush = brush.CloneCurrentValue();
            if (brush is SolidColorBrush solid) solid.Color = map(solid.Color);
            else if (brush is GradientBrush gradient)
            {
                foreach (var stop in gradient.GradientStops) stop.Color = map(stop.Color);
            }
            else if (brush is DrawingBrush drawing) MapColors(drawing.Drawing, map);
            else if (brush is ImageBrush image) image.ImageSource = ConvertImage(image.ImageSource, map);
            return brush;
        }

        private static Color Gray(Color color)
        {
            byte gray = Gray(color.R, color.G, color.B);
            return Color.FromArgb(color.A, gray, gray, gray);
        }

        private static byte Gray(byte red, byte green, byte blue)
        {
            return (byte)Math.Round(0.299 * red + 0.587 * green + 0.114 * blue);
        }

        private static ImageSource ConvertImage(ImageSource source, Func<Color, Color> map)
        {
            if (source is DrawingImage drawing)
            {
                var copy = drawing.Drawing.CloneCurrentValue();
                MapColors(copy, map);
                return new DrawingImage(copy);
            }
            if (!(source is BitmapSource bitmap)) return source;
            var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Pbgra32, null, 0);
            int stride = checked(converted.PixelWidth * 4);
            var pixels = new byte[checked(stride * converted.PixelHeight)];
            converted.CopyPixels(pixels, stride, 0);
            for (int i = 0; i < pixels.Length; i += 4)
            {
                double alpha = pixels[i + 3];
                if (alpha == 0) continue;
                var color = map(Color.FromArgb((byte)alpha, (byte)Math.Round(pixels[i + 2] * 255 / alpha),
                    (byte)Math.Round(pixels[i + 1] * 255 / alpha), (byte)Math.Round(pixels[i] * 255 / alpha)));
                pixels[i] = (byte)Math.Round(color.B * color.A / 255.0);
                pixels[i + 1] = (byte)Math.Round(color.G * color.A / 255.0);
                pixels[i + 2] = (byte)Math.Round(color.R * color.A / 255.0);
                pixels[i + 3] = color.A;
            }
            var result = BitmapSource.Create(bitmap.PixelWidth, bitmap.PixelHeight,
                bitmap.DpiX, bitmap.DpiY, PixelFormats.Pbgra32, null, pixels, stride);
            result.Freeze();
            return result;
        }
    }
}
