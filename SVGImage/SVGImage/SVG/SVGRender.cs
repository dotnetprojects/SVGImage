using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Markup;
using System.Xml;
using System.Windows;
using System.Windows.Media;

using SVGImage.SVG.Shapes;

namespace SVGImage.SVG
{
    // http://www.w3.org/TR/SVGTiny12/
    // http://commons.oreilly.com/wiki/index.php/SVG_Essentials
    public class SVGRender
    {
        private SVG m_svg;

        public SVG SVG
        {
            get
            {
                return this.m_svg;
            }
        }

        public DrawingGroup LoadDrawing(string filename)
        {
            this.m_svg = new SVG(filename);
            return this.CreateDrawing(this.m_svg);
        }

        public DrawingGroup LoadDrawing(Stream stream)
        {
            this.m_svg = new SVG(stream);

            /*var aa = new MemoryStream();
            var qq = XmlWriter.Create(aa, new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true });
            XamlWriter.Save(this.CreateDrawing(this.m_svg), qq);
            aa.Position = 0;
            var bb = new StreamReader(aa);
            var cc = bb.ReadToEnd();*/

            return this.CreateDrawing(this.m_svg);
        }

        public DrawingGroup CreateDrawing(SVG svg)
        {
            return this.LoadGroup(svg.Elements, svg.ViewBox);
        }

        public DrawingGroup CreateDrawing(Shape shape)
        {
            return this.LoadGroup(new Shape[] { shape }, null);
        }

        private GeometryDrawing NewDrawingItem(Shape shape, Geometry geometry)
        {
            GeometryDrawing item = new GeometryDrawing();
            Stroke stroke = shape.Stroke;
            if (stroke != null)
            {
                item.Pen = new Pen(stroke.StrokeBrush(this.SVG, shape.Opacity), stroke.Width);
                if (stroke.StrokeArray != null)
                {
                    item.Pen.DashCap = PenLineCap.Flat;
                    DashStyle ds = new DashStyle();
                    double scale = 1 / stroke.Width;
                    foreach (int dash in stroke.StrokeArray) ds.Dashes.Add(dash * scale);
                    item.Pen.DashStyle = ds;
                }
                switch (stroke.LineCap)
                {
                    case Stroke.eLineCap.butt:
                        item.Pen.StartLineCap = PenLineCap.Flat;
                        item.Pen.EndLineCap = PenLineCap.Flat;
                        break;
                    case Stroke.eLineCap.round:
                        item.Pen.StartLineCap = PenLineCap.Round;
                        item.Pen.EndLineCap = PenLineCap.Round;
                        break;
                    case Stroke.eLineCap.square:
                        item.Pen.StartLineCap = PenLineCap.Square;
                        item.Pen.EndLineCap = PenLineCap.Square;
                        break;
                }
                switch (stroke.LineJoin)
                {
                    case Stroke.eLineJoin.round:
                        item.Pen.LineJoin = PenLineJoin.Round;
                        break;
                    case Stroke.eLineJoin.miter:
                        item.Pen.LineJoin = PenLineJoin.Miter;
                        break;
                    case Stroke.eLineJoin.bevel:
                        item.Pen.LineJoin = PenLineJoin.Bevel;
                        break;
                }
            }
            if (shape.Fill != null)
            {
                item.Brush = shape.Fill.FillBrush(this.SVG, shape.Opacity);
                GeometryGroup g = new GeometryGroup();
                g.FillRule = FillRule.Nonzero;
                if (shape.Fill.FillRule == Fill.eFillRule.evenodd) g.FillRule = FillRule.EvenOdd;
                g.Children.Add(geometry);
                geometry = g;
            }
            if (shape.Transform != null) geometry.Transform = shape.Transform;

            // for debugging, if neither stroke or fill is set then set default pen
            //if (shape.Fill == null && shape.Stroke == null)
            //	item.Pen = new Pen(Brushes.Blue, 1);

            item.Geometry = geometry;
            return item;
        }

        private class ControlLine
        {
            public Point Ctrl { get; private set; }

            public Point Start { get; private set; }

            public ControlLine(Point start, Point ctrl)
            {
                this.Start = start;
                this.Ctrl = ctrl;
            }

            public GeometryDrawing Draw()
            {
                double size = 0.2;
                GeometryDrawing item = new GeometryDrawing();
                item.Brush = Brushes.Red;
                GeometryGroup g = new GeometryGroup();

                item.Pen = new Pen(Brushes.LightGray, size / 2);
                g.Children.Add(new LineGeometry(this.Start, this.Ctrl));

                g.Children.Add(new RectangleGeometry(new Rect(this.Start.X - size / 2, this.Start.Y - size / 2, size, size)));
                g.Children.Add(new EllipseGeometry(this.Ctrl, size, size));

                item.Geometry = g;
                return item;
            }
        }

        private DrawingGroup LoadGroup(IList<Shape> elements, Rect? viewBox)
        {
            List<ControlLine> debugPoints = new List<ControlLine>();
            DrawingGroup grp = new DrawingGroup();

            if (viewBox.HasValue) grp.ClipGeometry = new RectangleGeometry(viewBox.Value);

            foreach (Shape shape in elements)
            {
                if (shape is UseShape)
                {
                    UseShape useshape = shape as UseShape;
                    Group group = this.SVG.GetShape(useshape.hRef) as Group;
                    if (group != null)
                    {
                        Shape oldparent = group.Parent;
                        group.Parent = useshape; // this to get proper style propagated
                        DrawingGroup subgroup = this.LoadGroup(group.Elements, null);
                        if (group.Clip != null) subgroup.ClipGeometry = group.Clip.ClipGeometry;
                        subgroup.Transform = new TranslateTransform(useshape.X, useshape.Y);
                        grp.Children.Add(subgroup);
                        group.Parent = oldparent;
                    }
                    continue;

                }
                if (shape is Clip)
                {
                    DrawingGroup subgroup = this.LoadGroup((shape as Clip).Elements, null);
                    if (shape.Transform != null) subgroup.Transform = shape.Transform;
                    grp.Children.Add(subgroup);
                    continue;
                }
                if (shape is Group)
                {
                    DrawingGroup subgroup = this.LoadGroup((shape as Group).Elements, null);
                    if (shape.Clip != null)
                    {
                        subgroup.ClipGeometry = shape.Clip.ClipGeometry;
                    }
                    if (shape.Transform != null) subgroup.Transform = shape.Transform;
                    AddDrawingToGroup(grp, shape, subgroup);
                    continue;
                }
                if (shape is RectangleShape)
                {
                    RectangleShape r = shape as RectangleShape;
                    RectangleGeometry rect = new RectangleGeometry(new Rect(r.X, r.Y, r.Width, r.Height));
                    rect.RadiusX = r.RX;
                    rect.RadiusY = r.RY;
                    if (rect.RadiusX == 0 && rect.RadiusY > 0) rect.RadiusX = rect.RadiusY;
                    var di = this.NewDrawingItem(shape, rect);
                    AddDrawingToGroup(grp, shape, di);
                }
                if (shape is LineShape)
                {
                    LineShape r = shape as LineShape;
                    LineGeometry line = new LineGeometry(r.P1, r.P2);
                    var di = this.NewDrawingItem(shape, line);
                    AddDrawingToGroup(grp, shape, di);
                }
                if (shape is PolylineShape)
                {
                    PolylineShape r = shape as PolylineShape;
                    PathGeometry path = new PathGeometry();
                    PathFigure p = new PathFigure();
                    path.Figures.Add(p);
                    p.IsClosed = false;
                    p.StartPoint = r.Points[0];
                    for (int index = 1; index < r.Points.Length; index++)
                    {
                        p.Segments.Add(new LineSegment(r.Points[index], true));
                    }
                    var di = this.NewDrawingItem(shape, path);
                    AddDrawingToGroup(grp, shape, di);
                }
                if (shape is PolygonShape)
                {
                    PolygonShape r = shape as PolygonShape;
                    PathGeometry path = new PathGeometry();
                    PathFigure p = new PathFigure();
                    path.Figures.Add(p);
                    p.IsClosed = true;
                    p.StartPoint = r.Points[0];
                    for (int index = 1; index < r.Points.Length; index++)
                    {
                        p.Segments.Add(new LineSegment(r.Points[index], true));
                    }
                    var di = this.NewDrawingItem(shape, path);
                    AddDrawingToGroup(grp, shape, di);
                }
                if (shape is CircleShape)
                {
                    CircleShape r = shape as CircleShape;
                    EllipseGeometry c = new EllipseGeometry(new Point(r.CX, r.CY), r.R, r.R);
                    var di = this.NewDrawingItem(shape, c);
                    AddDrawingToGroup(grp, shape, di);
                }
                if (shape is EllipseShape)
                {
                    EllipseShape r = shape as EllipseShape;
                    EllipseGeometry c = new EllipseGeometry(new Point(r.CX, r.CY), r.RX, r.RY);
                    var di = this.NewDrawingItem(shape, c);
                    AddDrawingToGroup(grp, shape, di);
                }
                if (shape is ImageShape)
                {
                    ImageShape image = shape as ImageShape;
                    ImageDrawing i = new ImageDrawing(image.ImageSource, new Rect(image.X, image.Y, image.Width, image.Height));
                    AddDrawingToGroup(grp, shape, i);
                }
                if (shape is TextShape)
                {
                    GeometryGroup gp = TextRender.BuildTextGeometry(shape as TextShape);
                    if (gp != null)
                    {
                        foreach (Geometry gm in gp.Children)
                        {
                            TextShape.TSpan.Element tspan = TextRender.GetElement(gm);
                            if (tspan != null)
                            {
                                var di = this.NewDrawingItem(tspan, gm);
                                AddDrawingToGroup(grp, shape, di);
                            }
                            else
                            {
                                var di = this.NewDrawingItem(shape, gm);
                                AddDrawingToGroup(grp, shape, di);
                            }
                        }
                    }
                }
                if (shape is PathShape)
                {
                    PathShape r = shape as PathShape;
                    PathFigure p = null;
                    Point lastPoint = new Point(0, 0);

                    PathShape.CurveTo lastc = null;
                    Point lastcirPoint = new Point(0, 0);

                    PathGeometry path = new PathGeometry();
                    foreach (PathShape.PathElement element in r.Elements)
                    {
                        bool isRelative = element.IsRelative;
                        if (element is PathShape.MoveTo)
                        {
                            p = new PathFigure();
                            p.IsClosed = r.ClosePath;
                            if (isRelative) p.StartPoint = lastPoint + (Vector)((PathShape.MoveTo)element).Point;
                            else p.StartPoint = ((PathShape.MoveTo)element).Point;
                            lastPoint = p.StartPoint;
                            path.Figures.Add(p);
                            continue;
                        }
                        if (element is PathShape.LineTo)
                        {
                            PathShape.LineTo lineto = element as PathShape.LineTo;
                            foreach (Point point in lineto.Points)
                            {
                                if (isRelative)
                                {
                                    Point newpoint = lastPoint + (Vector)point;
                                    lastPoint = newpoint;
                                    p.Segments.Add(new LineSegment(newpoint, true));
                                }
                                else
                                {
                                    if (lineto.PositionType == PathShape.LineTo.eType.Point) lastPoint = point;
                                    if (lineto.PositionType == PathShape.LineTo.eType.Horizontal) lastPoint = new Point(point.X, lastPoint.Y);
                                    if (lineto.PositionType == PathShape.LineTo.eType.Vertical) lastPoint = new Point(lastPoint.X, point.Y);
                                    p.Segments.Add(new LineSegment(lastPoint, true));
                                }
                            }
                            continue;
                        }
                        if (element is PathShape.CurveTo)
                        {
                            PathShape.CurveTo c = element as PathShape.CurveTo;
                            Point startPoint = lastPoint;
                            BezierSegment s = new BezierSegment();
                            if (isRelative)
                            {
                                s.Point1 = lastPoint + (Vector)c.CtrlPoint1;

                                if (c.Command == 's')
                                {
                                    // first control point is a mirrored point of last end control point
                                    //s.Point1 = lastPoint + new Vector(lastc.Point.X - dx, lastc.Point.Y - dy);
                                    //s.Point1 = new Point(lastctrlpoint.X+2, lastctrlpoint.Y+2);

                                    double dx = lastc.CtrlPoint2.X - lastc.Point.X;
                                    double dy = lastc.CtrlPoint2.Y - lastc.Point.Y;
                                    s.Point1 = new Point(lastcirPoint.X - dx, lastcirPoint.Y - dy);
                                    //s.Point1 = lastctrlpoint;
                                }

                                s.Point2 = lastPoint + (Vector)c.CtrlPoint2;
                                s.Point3 = lastPoint + (Vector)c.Point;
                            }
                            else
                            {
                                if (c.Command == 'S')
                                {
                                    // first control point is a mirrored point of last end control point
                                    //s.Point1 = lastPoint + new Vector(lastc.Point.X - dx, lastc.Point.Y - dy);
                                    //s.Point1 = new Point(lastctrlpoint.X+2, lastctrlpoint.Y+2);

                                    double dx = lastc.CtrlPoint2.X - lastc.Point.X;
                                    double dy = lastc.CtrlPoint2.Y - lastc.Point.Y;
                                    s.Point1 = new Point(lastcirPoint.X - dx, lastcirPoint.Y - dy);
                                }
                                else s.Point1 = c.CtrlPoint1;
                                s.Point2 = c.CtrlPoint2;
                                s.Point3 = c.Point;
                            }
                            lastPoint = s.Point3;
                            p.Segments.Add(s);

                            lastc = c;
                            lastcirPoint = s.Point3;

                            //debugPoints.Add(new ControlLine(startPoint, s.Point1));
                            //debugPoints.Add(new ControlLine(s.Point3, s.Point2));
                            continue;
                        }
                        if (element is PathShape.EllipticalArcTo)
                        {
                            PathShape.EllipticalArcTo c = element as PathShape.EllipticalArcTo;
                            ArcSegment s = new ArcSegment();
                            if (isRelative) s.Point = lastPoint + new Vector(c.X, c.Y);
                            else s.Point = new Point(c.X, c.Y);

                            s.Size = new Size(c.RX, c.RY);
                            s.RotationAngle = c.AxisRotation;
                            s.SweepDirection = SweepDirection.Counterclockwise;
                            if (c.Clockwise) s.SweepDirection = SweepDirection.Clockwise;
                            s.IsLargeArc = c.LargeArc;
                            lastPoint = s.Point;
                            p.Segments.Add(s);
                            continue;
                        }
                    }
                    /*
                    if (r.Transform != null)
                        path.Transform = r.Transform;
                    */
                    var di = this.NewDrawingItem(shape, path);
                    AddDrawingToGroup(grp, shape, di);
                    //}
                }
            }


            if (debugPoints != null)
            {
                foreach (ControlLine line in debugPoints)
                {
                    grp.Children.Add(line.Draw());
                }
            }
            return grp;
        }

        private void AddDrawingToGroup(DrawingGroup grp, Shape shape, Drawing drawing)
        {
            if (shape.Clip != null)
            {
                var subgrp = new DrawingGroup();
                subgrp.ClipGeometry = shape.Clip.ClipGeometry;
                subgrp.Children.Add(drawing);
                grp.Children.Add(subgrp);
            }
            else
                grp.Children.Add(drawing);
        }
    }
}
