using System.Windows;
using System.Windows.Media;

using SVGImage.SVG.PaintServer;
using SVGImage.SVG.Shapes;

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

		public Brush StrokeBrush(SVG svg, SVGRender svgRender, Shape shape, double elementOpacity, Rect bounds, PaintServer.PaintServer parent)
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
		                if (p.Fill != null && !(p.Stroke.Color is InheritPaintServer))
		                    return p.Stroke.Color.GetBrush(this.Opacity * elementOpacity, svg, svgRender, bounds);
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
