using System.Windows;
using System.Windows.Media;

namespace SVGImage.SVG.PaintServers
{
    public class SolidColorPaintServer : PaintServer
    {
        public Color Color { get; set; }

        public SolidColorPaintServer(PaintServerManager owner, Color c)
            : base(owner)
        {
            this.Color = c;
        }

        public SolidColorPaintServer(PaintServerManager owner, Brush newBrush) : base(owner)
        {
            Brush = newBrush;
        }

        public override Brush GetBrush(double opacity, SVG svg, SVGRender svgRender, Rect bounds)
        {
            byte a = (byte)(255 * opacity / 100);
            if (Brush != null && Brush is SolidColorBrush s)
            {
                if (opacity < 100)
                {
                    return new SolidColorBrush(Color.FromArgb(a, s.Color.R, s.Color.G, s.Color.B));
                }
                return Brush;
            }
            Color c = this.Color;
            Color newcol = Color.FromArgb(a, c.R, c.G, c.B);
            Brush = new SolidColorBrush(newcol);
            return Brush;
        }
    }
}
