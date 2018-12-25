using System.Windows;
using System.Windows.Media;

using SVGImage.SVG.PaintServer;

namespace SVGImage.SVG
{
	public class Fill
	{
		public enum eFillRule
		{
			nonzero,
			evenodd
		}
		public eFillRule FillRule { get; set;}
		public PaintServer.PaintServer Color {get; set;}
		public double Opacity {get; set;}
		public Fill(SVG svg)
		{
			this.FillRule = eFillRule.nonzero;
			this.Color = new SolidColorPaintServer(svg.PaintServers, Colors.LightSeaGreen);
			this.Opacity = 100;
		}
		public Brush FillBrush(SVG svg, SVGRender svgRender, double elementOpacity, Rect bounds)
		{
			if (this.Color != null)
				return this.Color.GetBrush(this.Opacity * elementOpacity, svg, svgRender, bounds);
			return null;
		}
	}
}
