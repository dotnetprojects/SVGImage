using System.Windows;
using System.Windows.Media;

namespace SVGImage.SVG.PaintServers
{
    // http://www.w3.org/TR/SVGTiny12/painting.html#PaintServers
    public class PaintServer
    {
        public PaintServerManager Owner { get; private set; }

        protected Brush Brush = null;

        public PaintServer(PaintServerManager owner)
        {
            this.Owner = owner;
        }

        public PaintServer(PaintServerManager owner, Brush newBrush)
        {
            this.Owner = owner;
            this.Brush = newBrush;
        }

        public virtual Brush GetBrush(double opacity, SVG svg, SVGRender svgRender, Rect bounds)
        {
            return ApplyOpacity(Brush, opacity);
        }

        protected static Brush ApplyOpacity(Brush brush, double opacity)
        {
            if (brush == null || opacity >= 100) return brush;
            var result = brush.CloneCurrentValue();
            result.Opacity *= System.Math.Max(0, opacity / 100);
            return result;
        }

        public Brush GetBrush()
        {
            return Brush;
        }
    }
}
