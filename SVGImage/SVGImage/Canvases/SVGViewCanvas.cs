using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace SVGImage.Canvases
{
	class SVGViewCanvas : Canvas
	{
		List<Drawing> m_drawings = new List<Drawing>();
		List<Drawing> m_highLights = new List<Drawing>();
		Label m_label;
		public double Zoom
		{
			get
			{
				return this.m_scaleTransform.ScaleX * 100;
			}
			set
			{
				this.m_scaleTransform.ScaleX = value / 100;
				this.m_scaleTransform.ScaleY = this.m_scaleTransform.ScaleX;
				this.InvalidateVisual();
			}
		}
		public SVGViewCanvas()
		{
			this.ClipToBounds = true;
			this.m_label = new Label();
			this.Children.Add(this.m_label);
			this.m_label.Content = "abs";
			//m_label.Background = Brushes.DarkBlue;
			//m_label.Foreground = Brushes.Cornsilk;
		}
		public void SetDrawing(Drawing drawing)
		{
			this.m_drawings.Clear();
			this.m_drawings.Add(drawing);
			
			Rect r = drawing.Bounds;
			this.m_offsetTransform.X = -r.Left;
			this.m_offsetTransform.Y = -r.Top;
			
			double xscale = this.ActualWidth / r.Width;
			double yscale = this.ActualHeight / r.Height;
			double scale = xscale;
			if (scale > yscale)
				scale = yscale;
			if (scale < 1)
			{
				this.Zoom = scale * 100;
				this.m_mouseDownPoint = null;
				this.m_offsetTransform.X = -r.Left * scale;
				this.m_offsetTransform.Y = -r.Top * scale;
			}
			else
			{
				//Zoom = 100;

				this.Zoom = scale * 100;
				this.m_mouseDownPoint = null;
				this.m_offsetTransform.X = -r.Left * scale;
				this.m_offsetTransform.Y = -r.Top * scale;

			}
			
			this.InvalidateVisual();
		}
		public void AddDrawing(Drawing drawing)
		{
			this.m_drawings.Add(drawing);
		}
		public void AddHighlight(Drawing drawing)
		{
			this.m_highLights.Add(drawing);
			this.InvalidateVisual();
		}
		public void ClearHighligh()
		{
			this.m_highLights.Clear();
			this.InvalidateVisual();
		}
		TranslateTransform m_offsetTransform = new TranslateTransform();
		ScaleTransform m_scaleTransform = new ScaleTransform();
		
		Point? m_mouseDownPoint;
		protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
		{
			base.OnMouseMove(e);
			Point newpoint = e.GetPosition(this);
			if (this.m_mouseDownPoint != null)
			{
				Point diff = (Point)((Vector)newpoint - (Vector)this.m_mouseDownPoint);
				this.m_mouseDownPoint = newpoint;
				this.m_offsetTransform.X += diff.X;
				this.m_offsetTransform.Y += diff.Y;
			}
			this.UpdateLabel(newpoint);
		}
		protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			if (this.m_mouseDownPoint == null)
				this.m_mouseDownPoint = e.GetPosition(this);
		}
		protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
		{
			base.OnMouseUp(e);
			this.m_mouseDownPoint = null;
		}
		void UpdateLabel(Point mousepoint)
		{
			Point cp = this.CanvasFromMouse(mousepoint);
			Point mp = this.MouseFromCanvas(cp);
			Point topleft = this.ScreenToInch(this.CanvasFromMouse(new Point(0,0)));
			Point bottomright = this.ScreenToInch(this.CanvasFromMouse(new Point(this.ActualWidth, this.ActualHeight)));
			string s = string.Format("Mouse {0}, Canvas {1} - {2}", mousepoint, cp, mp);
			//string s = string.Format("topleft {0,3}, bottomright {1,3}", topleft, bottomright);
			this.m_label.Content = s;
		}
		
		Point MouseFromCanvas(Point canvaspoint)
		{
			double mx = (canvaspoint.X * this.m_scaleTransform.ScaleX) + this.m_offsetTransform.X;
			double my = (canvaspoint.Y * this.m_scaleTransform.ScaleY) + this.m_offsetTransform.Y;
			return new Point(mx,my);
		}
		Point CanvasFromMouse(Point mousepoint)
		{
			double cx = (mousepoint.X - this.m_offsetTransform.X) / this.m_scaleTransform.ScaleX;
			double cy = (mousepoint.Y - this.m_offsetTransform.Y) / this.m_scaleTransform.ScaleY;
			return new Point(cx,cy);
		}
		protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e)
		{
			base.OnMouseWheel(e);
			double diff = 0.9;
			if (e.Delta < 0)
				diff = 1.1;

			Point mousebefore = e.GetPosition(this);
			Point before = this.CanvasFromMouse(mousebefore);

			this.m_scaleTransform.ScaleX *= diff;
			this.m_scaleTransform.ScaleY *= diff;
			
			Point mouseafter = this.MouseFromCanvas(before);
			this.m_offsetTransform.X += mousebefore.X - mouseafter.X;
			this.m_offsetTransform.Y += mousebefore.Y - mouseafter.Y;
			
			this.UpdateLabel(e.GetPosition(this));
			this.InvalidateVisual();
		}
		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);
			dc.PushTransform(this.m_offsetTransform);
			dc.PushTransform(this.m_scaleTransform);
			dc.DrawDrawing(this.Grid());
			if (this.m_highLights.Count == 0)
			{
			foreach (DrawingGroup v in this.m_drawings)
				dc.DrawDrawing(v);
			}
			foreach (DrawingGroup v in this.m_highLights)
				dc.DrawDrawing(v);
			dc.Pop();
			dc.Pop();
		}

		DrawingGroup Grid()
		{
			int gridsize = 5;
			DrawingGroup dg = new DrawingGroup();
			GeometryGroup gg = new GeometryGroup();
			gg.Children.Add(new LineGeometry(this.InchToScreen(0, -gridsize), this.InchToScreen(0,gridsize)));
			gg.Children.Add(new LineGeometry(this.InchToScreen(-gridsize, 0), this.InchToScreen(gridsize,0)));
			
			GeometryDrawing gd = new GeometryDrawing();
			gd.Geometry = gg;
			double width = 0.5 * (1/this.m_scaleTransform.ScaleX);
			gd.Pen = new Pen(Brushes.DarkBlue, width);
			dg.Children.Add(gd);

			// minor grid
			gd = new GeometryDrawing();
			gg = new GeometryGroup();
			double x = -gridsize;
			while (x < gridsize)
			{
				gg.Children.Add(new LineGeometry(this.InchToScreen(x, -gridsize), this.InchToScreen(x,gridsize)));
				gg.Children.Add(new LineGeometry(this.InchToScreen(-gridsize, x), this.InchToScreen(gridsize,x)));
				x += 1;
			}
			gd.Geometry = gg;
			width = 0.15 * (1/this.m_scaleTransform.ScaleX);
			gd.Pen = new Pen(Brushes.DarkBlue, width);
			dg.Children.Add(gd);

			return dg;

		}

		double InchToScreen(double inchValue)
		{
			return inchValue * 96; // DPI is always 96 in WPF ??
		}
		Point ScreenToInch(Point screenPoint)
		{
			return new Point(screenPoint.X / 96, screenPoint.Y / 96);
		}
		Point InchToScreen(double inchX, double inchY)
		{
			return new Point(this.InchToScreen(inchX), this.InchToScreen(inchY));
		}
		Point InchToScreen(Point inchPoint)
		{
			return new Point(this.InchToScreen(inchPoint.X), this.InchToScreen(inchPoint.Y));
		}
	}
}
