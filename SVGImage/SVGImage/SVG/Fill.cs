using System.Windows;
using System.Windows.Media;

using SVGImage.SVG.PaintServer;
using SVGImage.SVG.Shapes;

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

		public Brush FillBrush(SVG svg, SVGRender svgRender, Shape shape, double elementOpacity, Rect bounds)
		{
		    if (this.Color != null)
		    {
		        if (this.Color is CurrentColorPaintServer)
		        {
		            return shape.Color.GetBrush(this.Opacity * elementOpacity, svg, svgRender, bounds);
                }
                if (this.Color is InheritPaintServer)
		        {
		            var p = shape.RealParent ?? shape.Parent;
		            while (p != null)
		            {
                        if (p.Fill!=null && !(p.Fill.Color is InheritPaintServer))
                            return p.Fill.Color.GetBrush(this.Opacity * elementOpacity, svg, svgRender, bounds);
                        p = p.RealParent ?? p.Parent;
		            }

		            return null;
		        }
		        return this.Color.GetBrush(this.Opacity * elementOpacity, svg, svgRender, bounds);
		    }

            return null;
		}
	}
}
