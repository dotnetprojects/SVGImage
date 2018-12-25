using System.Windows;
using System.Windows.Media;

namespace SVGImage.SVG.PaintServer
{
    // http://www.w3.org/TR/SVGTiny12/painting.html#PaintServers
    public abstract class PaintServer
    {
        public PaintServerManager Owner { get; private set; }

        public PaintServer(PaintServerManager owner)
        {
            this.Owner = owner;
        }

        public abstract Brush GetBrush(double opacity, SVG svg, SVGRender svgRender, Rect bounds);
    }
}
