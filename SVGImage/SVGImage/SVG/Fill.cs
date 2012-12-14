using System.Windows.Media;

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
		public PaintServer Color {get; set;}
		public double Opacity {get; set;}
		public Fill(SVG svg)
		{
			this.FillRule = eFillRule.nonzero;
			this.Color = new SolidColor(svg.PaintServers, Colors.LightSeaGreen);
			this.Opacity = 100;
		}
		public Brush FillBrush(SVG svg)
		{
			if (this.Color != null)
				return this.Color.GetBrush(this.Opacity, svg);
			return null;
		}
	}
}
