using System;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace ClipArtViewer
{
    public class SVGViewCanvas : Canvas
    {
        TranslateTransform m_offsetTransform = new TranslateTransform();
        ScaleTransform m_scaleTransform = new ScaleTransform();

        Point? m_mouseDownPoint;

        List<Drawing> m_drawings = new List<Drawing>();
        List<Drawing> m_highLights = new List<Drawing>();
        Label m_label;

        public SVGViewCanvas()
        {
            this.ClipToBounds = true;
            m_label = new Label();
            this.Children.Add(m_label);
            m_label.Content = "abs";
            //m_label.Background = Brushes.DarkBlue;
            //m_label.Foreground = Brushes.Cornsilk;
        }

        public double Zoom
        {
            get {
                return m_scaleTransform.ScaleX * 100;
            }
            set {
                m_scaleTransform.ScaleX = value / 100;
                m_scaleTransform.ScaleY = m_scaleTransform.ScaleX;
                InvalidateVisual();
            }
        }

        public void SetDrawing(Drawing drawing)
        {
            m_drawings.Clear();
            if (drawing == null)
            {
                m_highLights.Clear();
                this.InvalidateVisual();
                return;
            }
            m_drawings.Add(drawing);

            Rect r = drawing.Bounds;
            m_offsetTransform.X = -r.Left;
            m_offsetTransform.Y = -r.Top;

            double xscale = this.ActualWidth / r.Width;
            double yscale = this.ActualHeight / r.Height;
            double scale = xscale;
            if (scale > yscale)
                scale = yscale;
            if (scale < 1)
            {
                Zoom = scale * 100;
                m_mouseDownPoint = null;
                m_offsetTransform.X = -r.Left * scale;
                m_offsetTransform.Y = -r.Top * scale;
            }
            else
            {
                //Zoom = 100;

                Zoom = scale * 100;
                m_mouseDownPoint = null;
                m_offsetTransform.X = -r.Left * scale;
                m_offsetTransform.Y = -r.Top * scale;

            }

            InvalidateVisual();
        }

        public void AddDrawing(Drawing drawing)
        {
            m_drawings.Add(drawing);
        }

        public void AddHighlight(Drawing drawing)
        {
            m_highLights.Add(drawing);
            InvalidateVisual();
        }

        public void ClearHighligh()
        {
            m_highLights.Clear();
            InvalidateVisual();
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Point newpoint = e.GetPosition(this);
            if (m_mouseDownPoint != null)
            {
                Point diff = (Point)((Vector)newpoint - (Vector)m_mouseDownPoint);
                m_mouseDownPoint = newpoint;
                m_offsetTransform.X += diff.X;
                m_offsetTransform.Y += diff.Y;
            }
            UpdateLabel(newpoint);
        }

        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (m_mouseDownPoint == null)
                m_mouseDownPoint = e.GetPosition(this);
        }

        protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            m_mouseDownPoint = null;
        }

        void UpdateLabel(Point mousepoint)
        {
            Point cp = CanvasFromMouse(mousepoint);
            Point mp = MouseFromCanvas(cp);
            Point topleft = ScreenToInch(CanvasFromMouse(new Point(0, 0)));
            Point bottomright = ScreenToInch(CanvasFromMouse(new Point(this.ActualWidth, this.ActualHeight)));
            string s = string.Format("Mouse {0}, Canvas {1} - {2}", mousepoint, cp, mp);
            //string s = string.Format("topleft {0,3}, bottomright {1,3}", topleft, bottomright);
            m_label.Content = s;
        }

        Point MouseFromCanvas(Point canvaspoint)
        {
            double mx = (canvaspoint.X * m_scaleTransform.ScaleX) + m_offsetTransform.X;
            double my = (canvaspoint.Y * m_scaleTransform.ScaleY) + m_offsetTransform.Y;
            return new Point(mx, my);
        }

        Point CanvasFromMouse(Point mousepoint)
        {
            double cx = (mousepoint.X - m_offsetTransform.X) / m_scaleTransform.ScaleX;
            double cy = (mousepoint.Y - m_offsetTransform.Y) / m_scaleTransform.ScaleY;
            return new Point(cx, cy);
        }

        protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            double diff = 0.9;
            if (e.Delta < 0)
                diff = 1.1;

            Point mousebefore = e.GetPosition(this);
            Point before = CanvasFromMouse(mousebefore);

            m_scaleTransform.ScaleX *= diff;
            m_scaleTransform.ScaleY *= diff;

            Point mouseafter = MouseFromCanvas(before);
            m_offsetTransform.X += mousebefore.X - mouseafter.X;
            m_offsetTransform.Y += mousebefore.Y - mouseafter.Y;

            UpdateLabel(e.GetPosition(this));
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            dc.PushTransform(m_offsetTransform);
            dc.PushTransform(m_scaleTransform);
            dc.DrawDrawing(Grid());
            if (m_highLights.Count == 0)
            {
                foreach (DrawingGroup v in m_drawings)
                    dc.DrawDrawing(v);
            }
            foreach (DrawingGroup v in m_highLights)
                dc.DrawDrawing(v);
            dc.Pop();
            dc.Pop();
        }

        DrawingGroup Grid()
        {
            int gridsize = 5;
            DrawingGroup dg = new DrawingGroup();
            GeometryGroup gg = new GeometryGroup();
            gg.Children.Add(new LineGeometry(InchToScreen(0, -gridsize), InchToScreen(0, gridsize)));
            gg.Children.Add(new LineGeometry(InchToScreen(-gridsize, 0), InchToScreen(gridsize, 0)));

            GeometryDrawing gd = new GeometryDrawing();
            gd.Geometry = gg;
            double width = 0.5 * (1 / m_scaleTransform.ScaleX);
            gd.Pen = new Pen(Brushes.DarkBlue, width);
            dg.Children.Add(gd);

            // minor grid
            gd = new GeometryDrawing();
            gg = new GeometryGroup();
            double x = -gridsize;
            while (x < gridsize)
            {
                gg.Children.Add(new LineGeometry(InchToScreen(x, -gridsize), InchToScreen(x, gridsize)));
                gg.Children.Add(new LineGeometry(InchToScreen(-gridsize, x), InchToScreen(gridsize, x)));
                x += 1;
            }
            gd.Geometry = gg;
            width = 0.15 * (1 / m_scaleTransform.ScaleX);
            gd.Pen = new Pen(Brushes.DarkBlue, width);
            dg.Children.Add(gd);

            return dg;

        }

        static double InchToScreen(double inchValue)
        {
            return inchValue * 96; // DPI is always 96 in WPF ??
        }

        static Point ScreenToInch(Point screenPoint)
        {
            return new Point(screenPoint.X / 96, screenPoint.Y / 96);
        }

        static Point InchToScreen(double inchX, double inchY)
        {
            return new Point(InchToScreen(inchX), InchToScreen(inchY));
        }

        static Point InchToScreen(Point inchPoint)
        {
            return new Point(InchToScreen(inchPoint.X), InchToScreen(inchPoint.Y));
        }
    }
}
