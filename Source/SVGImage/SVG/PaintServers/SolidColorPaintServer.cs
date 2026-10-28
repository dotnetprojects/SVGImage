using System.Windows;
using System.Windows.Media;

namespace SVGImage.SVG.PaintServers
{
    public sealed class SolidColorPaintServer : PaintServer
    {
        public Color Color { get; set; }

        public SolidColorPaintServer(PaintServerManager owner, Color c)
            : base(owner)
        {
            this.Color = c;
        }

        public SolidColorPaintServer(PaintServerManager owner, Brush newBrush) 
            : base(owner)
        {
            Brush = newBrush;
        }

        public override Brush GetBrush(double opacity, SVG svg, SVGRender svgRender, Rect bounds)
        {
            if (Brush == null) Brush = new SolidColorBrush(this.Color);
            if (opacity >= 100) return Brush;
            // Preserve the color's alpha and keep the cached brush at full opacity.
            // Masks and artwork can share a color while using different fill-opacity.
            var result = Brush.CloneCurrentValue();
            result.Opacity *= System.Math.Max(0, opacity / 100);
            return result;
        }
    }
}
