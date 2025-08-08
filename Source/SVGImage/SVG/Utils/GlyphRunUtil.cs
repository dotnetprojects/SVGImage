using System;
using System.Windows;
using System.Windows.Media;

namespace SVGImage.SVG.Utils
{
    internal static class GlyphRunUtil
    {
        public static GlyphRun CreateOffsetRun(this GlyphRun value, double xOffset, double yOffset)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            return new GlyphRun(
                value.GlyphTypeface,
                value.BidiLevel,
                value.IsSideways,
                value.FontRenderingEmSize,
#if DPI_AWARE
                value.PixelsPerDip,
#endif
                value.GlyphIndices,
                new Point( value.BaselineOrigin.X + xOffset, value.BaselineOrigin.Y + yOffset),
                value.AdvanceWidths,
                value.GlyphOffsets,
                value.Characters,
                value.DeviceFontName,
                value.ClusterMap,
                value.CaretStops,
                value.Language);
        }
    }
}
