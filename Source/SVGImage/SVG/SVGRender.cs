using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SVGImage.SVG
{
    using Animation;
    using Shapes;
    using FileLoaders;

    /// <summary>
    /// This is the class that creates the WPF Drawing object based on the information from the <see cref="SVG"/> class.
    /// </summary>
    /// <seealso href="http://www.w3.org/TR/SVGTiny12/"/>
    /// <seealso href="http://commons.oreilly.com/wiki/index.php/SVG_Essentials"/>
    public class SVGRender
    {
        public SVGRender() : this(FileSystemLoader.Instance)
        {
        }

        public SVGRender(IExternalFileLoader fileLoader)
        {
            ExternalFileLoader = (fileLoader != null) ? fileLoader : FileSystemLoader.Instance;
        }

        public SVG SVG { get; private set; }

        public bool UseAnimations { get; set; }

        public Color? OverrideColor { get; set; }

        public Color? OverrideFillColor { get; set; }

        public Color? OverrideStrokeColor { get; set; }
       
        public double? OverrideStrokeWidth { get; set; }

        private Dictionary<string, Brush> m_customBrushes;

        public Dictionary<string, Brush> CustomBrushes
        {
            get => m_customBrushes;
            set
            {
                m_customBrushes = value;
                if (this.SVG != null)
                {
                    this.SVG.CustomBrushes = value;
                }
            }
        }

        public IExternalFileLoader ExternalFileLoader { get; set; }

        public DrawingGroup LoadDrawing(string filename)
        {
            this.SVG = new SVG(filename, ExternalFileLoader);
            return this.CreateDrawing(this.SVG);
        }

        public DrawingGroup LoadXmlDrawing(string fileXml)
        {
            this.SVG = new SVG(this.ExternalFileLoader);
            this.SVG.LoadXml(fileXml);

            return this.CreateDrawing(this.SVG);
        }

        public DrawingGroup LoadDrawing(Uri fileUri)
        {
            this.SVG = new SVG(this.ExternalFileLoader);
            this.SVG.Load(fileUri);

            return this.CreateDrawing(this.SVG);
        }

        public DrawingGroup LoadDrawing(TextReader txtReader)
        {
            this.SVG = new SVG(this.ExternalFileLoader);
            this.SVG.Load(txtReader);

            return this.CreateDrawing(this.SVG);
        }

        public DrawingGroup LoadDrawing(XmlReader xmlReader)
        {
            this.SVG = new SVG(this.ExternalFileLoader);
            this.SVG.Load(xmlReader);

            return this.CreateDrawing(this.SVG);
        }

        public DrawingGroup LoadDrawing(Stream stream)
        {
            this.SVG = new SVG(stream, ExternalFileLoader);

            return this.CreateDrawing(this.SVG);
        }

        public DrawingGroup CreateDrawing(SVG svg)
        {
            return this.LoadGroup(svg.Elements, svg.ViewBox, false);
        }

        public DrawingGroup CreateDrawing(Shape shape)
        {
            return this.LoadGroup(new Shape[] { shape }, null, false);
        }

        private GeometryDrawing NewDrawingItem(Shape shape, Geometry geometry)
        {
            shape.geometryElement = geometry;
            GeometryDrawing item = new GeometryDrawing();
            Stroke stroke = shape.Stroke;
            if (stroke != null)
            {
                var strokeWidth = stroke.Width;
                if (OverrideStrokeWidth.HasValue)
                {
                    strokeWidth = OverrideStrokeWidth.Value;
                }
                var brush = stroke.StrokeBrush(this.SVG, this, shape, shape.Opacity, geometry.Bounds);
                if (OverrideColor != null)
                    brush = new SolidColorBrush(Color.FromArgb((byte)(255 * shape.Opacity), 
                        OverrideColor.Value.R, OverrideColor.Value.G, OverrideColor.Value.B));
                if (OverrideStrokeColor != null)
                    brush = new SolidColorBrush(Color.FromArgb((byte)(255 * shape.Opacity),
                        OverrideStrokeColor.Value.R, OverrideStrokeColor.Value.G, OverrideStrokeColor.Value.B));
                item.Pen = new Pen(brush, strokeWidth);
                if (stroke.StrokeArray != null)
                {
                    item.Pen.DashCap = PenLineCap.Flat;
                    DashStyle ds = new DashStyle();
                    double scale = 1 / strokeWidth;
                    foreach (var dash in stroke.StrokeArray) ds.Dashes.Add(dash * scale);
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

            if (shape.Fill == null)
            {
                item.Brush = Brushes.Black;
                if (OverrideColor != null)
                    item.Brush = new SolidColorBrush(Color.FromArgb((byte)(255 * shape.Opacity), 
                        OverrideColor.Value.R, OverrideColor.Value.G, OverrideColor.Value.B));
                if (OverrideFillColor != null)
                    item.Brush = new SolidColorBrush(Color.FromArgb((byte)(255 * shape.Opacity),
                        OverrideFillColor.Value.R, OverrideFillColor.Value.G, OverrideFillColor.Value.B));
                GeometryGroup g = new GeometryGroup();
                g.FillRule = FillRule.Nonzero;
                g.Children.Add(geometry);
                geometry = g;
            }
            else if (shape.Fill != null)
            {
                item.Brush = shape.Fill.FillBrush(this.SVG, this, shape, shape.Opacity, geometry.Bounds);
                if (item.Brush != null)
                {
                    if (OverrideColor != null)
                        item.Brush = new SolidColorBrush(Color.FromArgb((byte)(255 * shape.Opacity), 
                            OverrideColor.Value.R, OverrideColor.Value.G, OverrideColor.Value.B));
                    if (OverrideFillColor != null)
                        item.Brush = new SolidColorBrush(Color.FromArgb((byte)(255 * shape.Opacity),
                            OverrideFillColor.Value.R, OverrideFillColor.Value.G, OverrideFillColor.Value.B));
                }
                GeometryGroup g = new GeometryGroup();
                g.FillRule = FillRule.Nonzero;
                if (shape.Fill.FillRule == Fill.eFillRule.evenodd) g.FillRule = FillRule.EvenOdd;
                g.Children.Add(geometry);
                geometry = g;
            }

            item.Geometry = geometry;
            return item;
        }

        internal DrawingGroup LoadGroup(IList<Shape> elements, Rect? viewBox, bool isSwitch)
        {
            DrawingGroup grp = new DrawingGroup();
            if (viewBox.HasValue) grp.ClipGeometry = new RectangleGeometry(viewBox.Value);

            foreach (Shape shape in elements)
            {
                shape.RealParent = null;
                if (!shape.Display)
                {
                    continue;
                }

                if (isSwitch)
                {
                    if (grp.Children.Count > 0)
                    {
                        break;
                    }
                    if (!string.IsNullOrEmpty(shape.RequiredFeatures))
                    {
                        if (!SVGFeatures.Features.Contains(shape.RequiredFeatures))
                        {
                            continue;
                        }
                        if (!string.IsNullOrEmpty(shape.RequiredExtensions))
                        {
                            continue;
                        }
                    }
                }

                if (shape is AnimationBase)
                {
                    if (UseAnimations)
                    {
                        if (shape is AnimateTransform animateTransform)
                        {
                            if (animateTransform.Type == AnimateTransformType.Rotate)
                            {
                                var animation = new DoubleAnimation
                                {
                                    From = double.Parse(animateTransform.From),
                                    To = double.Parse(animateTransform.To),
                                    Duration = animateTransform.Duration
                                };
                                animation.RepeatBehavior = RepeatBehavior.Forever;
                                var r = new RotateTransform();
                                grp.Transform = r;
                                r.BeginAnimation(RotateTransform.AngleProperty, animation);
                            }
                        }
                        else if (shape is Animate animate)
                        {
                            var target = this.SVG.GetShape(animate.hRef);
                            var g = target.geometryElement;
                            //todo : rework this all, generalize it!
                            if (animate.AttributeName == "r")
                            {
                                var animation = new DoubleAnimationUsingKeyFrames() { Duration = animate.Duration };
                                foreach (var d in animate.Values.Split(';').Select(x => new LinearDoubleKeyFrame(double.Parse(x))))
                                {
                                    animation.KeyFrames.Add(d);
                                }
                                animation.RepeatBehavior = RepeatBehavior.Forever;

                                g.BeginAnimation(EllipseGeometry.RadiusXProperty, animation);
                                g.BeginAnimation(EllipseGeometry.RadiusYProperty, animation);
                            }
                            else if (animate.AttributeName == "cx")
                            {
                                var animation = new PointAnimationUsingKeyFrames() { Duration = animate.Duration };
                                foreach (var d in animate.Values.Split(';').Select(_ => new LinearPointKeyFrame(
                                    new Point(double.Parse(_), ((EllipseGeometry)g).Center.Y))))
                                {
                                    animation.KeyFrames.Add(d);
                                }
                                animation.RepeatBehavior = RepeatBehavior.Forever;
                                g.BeginAnimation(EllipseGeometry.CenterProperty, animation);
                            }
                            else if (animate.AttributeName == "cy")
                            {
                                var animation = new PointAnimationUsingKeyFrames() { Duration = animate.Duration };
                                foreach (var d in animate.Values.Split(';').Select(_ => new LinearPointKeyFrame(
                                    new Point(((EllipseGeometry)g).Center.X, double.Parse(_)))))
                                {
                                    animation.KeyFrames.Add(d);
                                }
                                animation.RepeatBehavior = RepeatBehavior.Forever;
                                g.BeginAnimation(EllipseGeometry.CenterProperty, animation);
                            }

                        }
                    }

                    continue;
                }

                if (shape is UseShape useshape)
                {
                    Shape currentUsedShape = this.SVG.GetShape(useshape.hRef);
                    if (currentUsedShape != null)
                    {
                        Shape oldparent = currentUsedShape.Parent;
                        //currentUsedShape.RealParent = useshape;
                        currentUsedShape.Parent = useshape;
                        DrawingGroup subgroup;
                        if (currentUsedShape is Group)
                            subgroup = this.LoadGroup(((Group)currentUsedShape).Elements, null, false);
                        else
                            subgroup = this.LoadGroup(new[]{ currentUsedShape }, null, false);
                        if (currentUsedShape.Clip != null)
                            subgroup.ClipGeometry = currentUsedShape.Clip.ClipGeometry;
                        subgroup.Transform = new TranslateTransform(useshape.X, useshape.Y);
                        if (useshape.Transform != null)
                            subgroup.Transform = new TransformGroup() {
                                Children = new TransformCollection() { subgroup.Transform, useshape.Transform } 
                            };
                        grp.Children.Add(subgroup);
                        currentUsedShape.Parent = oldparent;
                    }
                    continue;
                }
                if (shape is Clip clip)
                {
                    var subgroup = this.LoadGroup(clip.Elements, null, false);
                    if (shape.Transform != null)
                        subgroup.Transform = shape.Transform;
                    grp.Children.Add(subgroup);
                    continue;
                }
                if (shape is Group groupShape)
                {
                    var subgroup = this.LoadGroup((shape as Group).Elements, null, groupShape.IsSwitch);
                    AddDrawingToGroup(grp, shape, subgroup);
                    continue;
                }
                if (shape is RectangleShape rectShape)
                {
                    double dx     = rectShape.X;
                    double dy     = rectShape.Y;
                    double width  = rectShape.Width;
                    double height = rectShape.Height;
                    double rx     = rectShape.RX;
                    double ry     = rectShape.RY;
                    if (width <= 0 || height <= 0)
                    {
                        continue;
                    }
                    if (rx <= 0 && ry > 0)
                    {
                        rx = ry;
                    }
                    else if (rx > 0 && ry <= 0)
                    {
                        ry = rx;
                    }

                    var rect = new RectangleGeometry(new Rect(dx, dy, width, height), rx, ry);
                    var di = this.NewDrawingItem(shape, rect);
                    AddDrawingToGroup(grp, shape, di);
                    continue;
                }
                if (shape is LineShape lineShape)
                {
                    LineGeometry line = new LineGeometry(lineShape.P1, lineShape.P2);
                    var di = this.NewDrawingItem(shape, line);
                    AddDrawingToGroup(grp, shape, di);
                    continue;
                }
                if (shape is PolylineShape polyline)
                {
                    PathGeometry path = new PathGeometry();
                    PathFigure p = new PathFigure();
                    path.Figures.Add(p);
                    p.IsClosed = false;
                    p.StartPoint = polyline.Points[0];
                    for (int index = 1; index < polyline.Points.Length; index++)
                    {
                        p.Segments.Add(new LineSegment(polyline.Points[index], true));
                    }
                    var di = this.NewDrawingItem(shape, path);
                    AddDrawingToGroup(grp, shape, di);
                    continue;
                }
                if (shape is PolygonShape polygon)
                {
                    PathGeometry path = new PathGeometry();
                    PathFigure p = new PathFigure();
                    path.Figures.Add(p);
                    p.IsClosed = true;
                    p.StartPoint = polygon.Points[0];
                    for (int index = 1; index < polygon.Points.Length; index++)
                    {
                        p.Segments.Add(new LineSegment(polygon.Points[index], true));
                    }
                    var di = this.NewDrawingItem(shape, path);
                    AddDrawingToGroup(grp, shape, di);
                    continue;
                }
                if (shape is CircleShape circle)
                {
                    EllipseGeometry c = new EllipseGeometry(new Point(circle.CX, circle.CY), circle.R, circle.R);
                    var di = this.NewDrawingItem(shape, c);
                    AddDrawingToGroup(grp, shape, di);
                    continue;
                }
                if (shape is EllipseShape ellipse)
                {
                    EllipseGeometry c = new EllipseGeometry(new Point(ellipse.CX, ellipse.CY), ellipse.RX, ellipse.RY);
                    var di = this.NewDrawingItem(shape, c);
                    AddDrawingToGroup(grp, shape, di);
                    continue;
                }
                if (shape is ImageShape image)
                {
                    var i = new ImageDrawing(image.ImageSource, new Rect(image.X, image.Y, image.Width, image.Height));
                    AddDrawingToGroup(grp, shape, i);
                    continue;
                }
                if (shape is Text textShape)
                {
                    TextRender2 textRender2 = new TextRender2();
                    GeometryGroup gp = textRender2.BuildTextGeometry(textShape);
                    if (gp != null)
                    {
                        foreach (Geometry gm in gp.Children)
                        {
                            TextSpan tspan = TextRender2.GetElement(gm);
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
                    continue;
                }
                if (shape is PathShape pathShape)
                {
                    var svg = this.SVG;
                    if (pathShape.Fill == null || pathShape.Fill.IsEmpty(svg))
                    {
                        if (pathShape.Stroke == null || pathShape.Stroke.IsEmpty(svg))
                        {
                            var fill = new Fill(svg);
                            fill.PaintServerKey = this.SVG.PaintServers.Parse("black");
                            pathShape.Fill = fill;
                        }
                    }
                    var path = PathGeometry.CreateFromGeometry(PathGeometry.Parse(pathShape.Data));
                    var di = this.NewDrawingItem(shape, path);
                    AddDrawingToGroup(grp, shape, di);
                }
            }

            return grp;
        }

        private void AddDrawingToGroup(DrawingGroup grp, Shape shape, Drawing drawing)
        {
            if (shape.Clip != null || shape.Transform != null || shape.Filter != null)
            {
                var subgrp = new DrawingGroup();
                if (shape.Clip != null)
                    subgrp.ClipGeometry = shape.Clip.ClipGeometry;
                if (shape.Transform != null)
                    subgrp.Transform = shape.Transform;
                if (shape.Filter != null)
                    subgrp.BitmapEffect = shape.Filter.GetBitmapEffect();
                subgrp.Children.Add(drawing);
                grp.Children.Add(subgrp);
            }
            else
                grp.Children.Add(drawing);
        }
    }
}
