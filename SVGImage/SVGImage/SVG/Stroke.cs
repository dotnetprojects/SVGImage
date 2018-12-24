using System.Windows.Media;

using SVGImage.SVG.PaintServer;

namespace SVGImage.SVG
{
	public class Stroke
	{
		public enum eLineCap
		{
			butt,
			round,
			square,
		}
		public enum eLineJoin
		{
			miter,
			round,
			bevel,
		}
		public PaintServer.PaintServer Color {get; set;}
		public double Width {get; set;}
		public double Opacity {get; set;}
		public eLineCap LineCap {get; set;}
		public eLineJoin LineJoin {get; set;}
		public double[] StrokeArray {get; set;} 
		public Stroke(SVG svg)
		{
			this.Color = new SolidColorPaintServer(svg.PaintServers, Colors.Black);
			this.Width = 1;
			this.LineCap = eLineCap.butt;
			this.LineJoin = eLineJoin.miter;
			this.Opacity = 100;
		}
		public Brush StrokeBrush(SVG svg, SVGRender svgRender, double elementOpacity)
		{
			if (this.Color != null)
				return this.Color.GetBrush(this.Opacity*elementOpacity, svg, svgRender);
			return null;
		}
	}
}
