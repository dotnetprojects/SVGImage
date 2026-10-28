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
            ConvertDrawing(copy);
            return copy;
        }

        private static void ConvertDrawing(Drawing drawing)
        {
            if (drawing is DrawingGroup group)
            {
                foreach (var child in group.Children) ConvertDrawing(child);
            }
            else if (drawing is GeometryDrawing geometry)
            {
                geometry.Brush = ConvertBrush(geometry.Brush);
                if (geometry.Pen != null) geometry.Pen.Brush = ConvertBrush(geometry.Pen.Brush);
            }
            else if (drawing is GlyphRunDrawing glyph)
                glyph.ForegroundBrush = ConvertBrush(glyph.ForegroundBrush);
            else if (drawing is ImageDrawing image)
                image.ImageSource = ConvertImage(image.ImageSource);
        }

        private static Brush ConvertBrush(Brush brush)
        {
            if (brush is SolidColorBrush solid) solid.Color = Gray(solid.Color);
            else if (brush is GradientBrush gradient)
            {
                foreach (var stop in gradient.GradientStops) stop.Color = Gray(stop.Color);
            }
            else if (brush is DrawingBrush drawing) ConvertDrawing(drawing.Drawing);
            else if (brush is ImageBrush image) image.ImageSource = ConvertImage(image.ImageSource);
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

        private static ImageSource ConvertImage(ImageSource source)
        {
            if (source is DrawingImage drawing)
                return new DrawingImage(ToGrayscale(drawing.Drawing));
            if (!(source is BitmapSource bitmap)) return source;
            var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Pbgra32, null, 0);
            int stride = checked(converted.PixelWidth * 4);
            var pixels = new byte[checked(stride * converted.PixelHeight)];
            converted.CopyPixels(pixels, stride, 0);
            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte gray = Gray(pixels[i + 2], pixels[i + 1], pixels[i]);
                pixels[i] = pixels[i + 1] = pixels[i + 2] = gray;
            }
            var result = BitmapSource.Create(bitmap.PixelWidth, bitmap.PixelHeight,
                bitmap.DpiX, bitmap.DpiY, PixelFormats.Pbgra32, null, pixels, stride);
            result.Freeze();
            return result;
        }
    }
}
