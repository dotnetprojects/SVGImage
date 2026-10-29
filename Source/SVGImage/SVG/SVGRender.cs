using SVGImage.SVG.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml;

namespace SVGImage.SVG
{
    using Animation;
    using FileLoaders;
    using Shapes;
    using System.Globalization;

    /// <summary>
    /// This is the class that creates the WPF Drawing object based on the information from the <see cref="SVG"/> class.
    /// </summary>
    /// <seealso href="http://www.w3.org/TR/SVGTiny12/"/>
    /// <seealso href="http://commons.oreilly.com/wiki/index.php/SVG_Essentials"/>
    public partial class SVGRender
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

        /// <summary>Applies SVG filter effects. Disable for a fast preview, then enable and recreate the drawing for full quality.</summary>
        public bool UseFilters { get; set; } = true;

        private double rasterizationScale = 4;
        /// <summary>Filter pixels per SVG user unit, capped at 2048 pixels per side.
        /// Lighting without kernelUnitLength depends on this sampling resolution.</summary>
        public double RasterizationScale
        {
            get { return rasterizationScale; }
            set
            {
                if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value))
                    throw new ArgumentOutOfRangeException(nameof(value));
                rasterizationScale = value;
            }
        }
        private HashSet<Mask> activeMasks = new HashSet<Mask>();
        private readonly HashSet<Shape> activeUses = new HashSet<Shape>();
        private SvgAnimationScene animationScene;

        /// <summary>Stops playback and evaluates the current scene at an exact time.</summary>
        public void SeekAnimations(TimeSpan position) => animationScene?.Seek(position);

        /// <summary>Forwards a named SVG event, for example an element's click, to SMIL timing.</summary>
        public void TriggerAnimationEvent(string elementId, string eventName = "click") => animationScene?.Trigger(elementId, eventName);

        public IList<string> AnimationWarnings => animationScene != null ? animationScene.Warnings.AsReadOnly() : new List<string>().AsReadOnly();

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

        internal void LoadDrawingWithoutRender(string filename)
        {
            this.SVG = new SVG(filename, ExternalFileLoader);
        }
        internal void LoadDrawingWithoutRender(Stream stream)
        {
            this.SVG = new SVG(stream, ExternalFileLoader);
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
            this.SVG = svg;
            animationScene = UseAnimations ? new SvgAnimationScene(svg) : null;
            bool camera = animationScene != null && animationScene.Tracks(svg.RootGroup).Any(t => t.Attribute == "viewBox");
            needsBackground=UseFilters && (svg.RootGroup?.SourceNode?.SelectNodes(".//*[@in='BackgroundImage' or @in='BackgroundAlpha' or @in2='BackgroundImage' or @in2='BackgroundAlpha']").Count ?? 0)>0;
            BackdropContext rootBackdrop=null;
            if(needsBackground) NewBackdrop(svg.RootGroup,out rootBackdrop);
            var drawingGroup = this.LoadGroup(svg.Elements, camera ? null : svg.ViewBox, false, rootBackdrop);
            if (svg.RootGroup != null && (svg.RootGroup.Mask != null || (UseFilters && svg.RootGroup.Filter != null) || svg.RootGroup.Transform != null || svg.RootGroup.Clip != null || svg.RootGroup.Opacity != 1 || (animationScene != null && animationScene.Tracks(svg.RootGroup).Count > 0)))
            {
                var masked = new DrawingGroup();
                AddDrawingToGroup(masked, svg.RootGroup, drawingGroup);
                if (camera && svg.ViewBox.HasValue) masked.ClipGeometry = new RectangleGeometry(svg.ViewBox.Value);
                return masked;
            }
            return drawingGroup;
        }

        public DrawingGroup CreateDrawing(Shape shape)
        {
            return this.LoadGroup(new Shape[] { shape }, null, false);
        }

        private GeometryDrawing NewDrawingItem(Shape shape, Geometry geometry)
        {
            double shapeOpacity = UseFilters && shape.Filter != null || animationScene != null && animationScene.Tracks(shape).Any(t => t.Attribute == "opacity") ? 1 : shape.Opacity;
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
                var brush = stroke.StrokeBrush(this.SVG, this, shape, shapeOpacity, geometry.Bounds);
                if (OverrideColor != null)
                    brush = new SolidColorBrush(Color.FromArgb((byte)(255 * shapeOpacity),
                        OverrideColor.Value.R, OverrideColor.Value.G, OverrideColor.Value.B));
                if (OverrideStrokeColor != null)
                    brush = new SolidColorBrush(Color.FromArgb((byte)(255 * shapeOpacity),
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
                    item.Brush = new SolidColorBrush(Color.FromArgb((byte)(255 * shapeOpacity),
                        OverrideColor.Value.R, OverrideColor.Value.G, OverrideColor.Value.B));
                if (OverrideFillColor != null)
                    item.Brush = new SolidColorBrush(Color.FromArgb((byte)(255 * shapeOpacity),
                        OverrideFillColor.Value.R, OverrideFillColor.Value.G, OverrideFillColor.Value.B));
                GeometryGroup g = new GeometryGroup();
                g.FillRule = FillRule.Nonzero;
                g.Children.Add(geometry);
                geometry = g;
            }
            else if (shape.Fill != null)
            {
                item.Brush = shape.Fill.FillBrush(this.SVG, this, shape, shapeOpacity, geometry.Bounds);
                if (item.Brush != null)
                {
                    if (OverrideColor != null)
                        item.Brush = new SolidColorBrush(Color.FromArgb((byte)(255 * shapeOpacity),
                            OverrideColor.Value.R, OverrideColor.Value.G, OverrideColor.Value.B));
                    if (OverrideFillColor != null)
                        item.Brush = new SolidColorBrush(Color.FromArgb((byte)(255 * shapeOpacity),
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

        internal DrawingGroup LoadGroup(IList<Shape> elements, Rect? viewBox, bool isSwitch, BackdropContext backdrop = null)
        {
            DrawingGroup grp = new DrawingGroup();
            if (viewBox.HasValue) grp.ClipGeometry = new RectangleGeometry(viewBox.Value);

            foreach (Shape shape in elements)
            {
                shape.RealParent = null;
                if (!shape.Display && !(animationScene != null && animationScene.Tracks(shape).Any(t => t.Attribute == "display")))
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
                        if (!shape.RequiredFeatures.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).All(SVGFeatures.Features.Contains))
                        {
                            continue;
                        }
                    }
                    if (!string.IsNullOrEmpty(shape.RequiredExtensions)) continue;
                }

                if (shape is AnimationBase) continue;

                if (shape is UseShape useshape)
                {
                    Shape currentUsedShape = this.SVG.GetShape(useshape.hRef);
                    bool recursive = false;
                    for (var ancestor = shape; ancestor != null; ancestor = ancestor.Parent)
                        if (ancestor == currentUsedShape) { recursive = true; break; }
                    if (currentUsedShape != null && !recursive && activeUses.Add(currentUsedShape))
                    {
                        Shape oldparent = currentUsedShape.Parent;
                        //currentUsedShape.RealParent = useshape;
                        currentUsedShape.Parent = useshape;
                        try
                        {
                            if(currentUsedShape is Symbol invalid && invalid.ViewBox.HasValue && (useshape.Width<=0 || useshape.Height<=0)) continue;
                            var placement=currentUsedShape is Symbol symbolBox && symbolBox.ViewBox.HasValue ? symbolBox.Viewport(useshape.X,useshape.Y,useshape.Width,useshape.Height) : new Matrix(1,0,0,1,useshape.X,useshape.Y);
                            var subgroup = this.LoadGroup(new[] { currentUsedShape }, null, false, ChildBackdrop(shape,backdrop,grp,placement));
                            if (currentUsedShape is Symbol symbol && symbol.ViewBox.HasValue)
                            {
                                if (useshape.Width <= 0 || useshape.Height <= 0) continue;
                                subgroup.Transform = new MatrixTransform(symbol.Viewport(useshape.X, useshape.Y, useshape.Width, useshape.Height));
                                if (!symbol.OverflowVisible)
                                {
                                    var viewport = new DrawingGroup { ClipGeometry = new RectangleGeometry(new Rect(useshape.X, useshape.Y, useshape.Width, useshape.Height)) };
                                    viewport.Children.Add(subgroup);
                                    subgroup = viewport;
                                }
                            }
                            else subgroup.Transform = new TranslateTransform(useshape.X, useshape.Y);
                            AddDrawingToGroup(grp, shape, subgroup, backdrop);
                        }
                        finally
                        {
                            currentUsedShape.Parent = oldparent;
                            activeUses.Remove(currentUsedShape);
                        }
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
                if (shape is SvgViewport viewportShape)
                {
                    if (viewportShape.Frame.Width <= 0 || viewportShape.Frame.Height <= 0) continue;
                    var content = LoadGroup(viewportShape.Elements,null,false,ChildBackdrop(shape,backdrop,grp,viewportShape.ViewportTransform()));
                    content.Transform = new MatrixTransform(viewportShape.ViewportTransform());
                    var viewport = new DrawingGroup();
                    if (!viewportShape.OverflowVisible) viewport.ClipGeometry = new RectangleGeometry(viewportShape.Frame);
                    viewport.Children.Add(content);
                    AddDrawingToGroup(grp, shape, viewport, backdrop);
                    continue;
                }
                if (shape is Group groupShape)
                {
                    var subgroup = this.LoadGroup((shape as Group).Elements, null, groupShape.IsSwitch, ChildBackdrop(shape,backdrop,grp));
                    AddDrawingToGroup(grp, shape, subgroup, backdrop);
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
                    AddDrawingToGroup(grp, shape, di, backdrop);
                    continue;
                }
                if (shape is LineShape lineShape)
                {
                    LineGeometry line = new LineGeometry(lineShape.P1, lineShape.P2);
                    var di = this.NewDrawingItem(shape, line);
                    AddDrawingToGroup(grp, shape, di, backdrop);
                    continue;
                }
                if (shape is PolylineShape polyline)
                {
                    if (polyline.Points.Length == 0) continue;
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
                    AddDrawingToGroup(grp, shape, di, backdrop);
                    continue;
                }
                if (shape is PolygonShape polygon)
                {
                    if (polygon.Points.Length == 0) continue;
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
                    AddDrawingToGroup(grp, shape, di, backdrop);
                    continue;
                }
                if (shape is CircleShape circle)
                {
                    EllipseGeometry c = new EllipseGeometry(new Point(circle.CX, circle.CY), circle.R, circle.R);
                    var di = this.NewDrawingItem(shape, c);
                    AddDrawingToGroup(grp, shape, di, backdrop);
                    continue;
                }
                if (shape is EllipseShape ellipse)
                {
                    EllipseGeometry c = new EllipseGeometry(new Point(ellipse.CX, ellipse.CY), ellipse.RX, ellipse.RY);
                    var di = this.NewDrawingItem(shape, c);
                    AddDrawingToGroup(grp, shape, di, backdrop);
                    continue;
                }
                if (shape is ImageShape image)
                {
                    var i = new ImageDrawing(image.ImageSource, new Rect(image.X, image.Y, image.Width, image.Height));
                    AddDrawingToGroup(grp, shape, i, backdrop);
                    continue;
                }
                if (shape is TextShape textShape)
                {
                    TextRender textRender2 = new TextRender();
                    GeometryGroup gp = textRender2.BuildTextGeometry(textShape);
                    if (gp != null)
                    {
                        foreach (Geometry gm in GetStyledSpans(gp))
                        {
                            if (TextRender.GetElement(gm) is TextShapeBase tspan)
                            {
                                var di = this.NewDrawingItem(tspan, gm);
                                AddDrawingToGroup(grp, shape, di, backdrop);
                            }
                            else
                            {
                                var di = this.NewDrawingItem(shape, gm);
                                AddDrawingToGroup(grp, shape, di, backdrop);
                            }
                        }
                    }
                    continue;
                }
                if (shape is PathShape pathShape)
                {
                    var path = SvgPathData.Parse(pathShape.Data);
                    var di = this.NewDrawingItem(shape, path);
                    AddDrawingToGroup(grp, shape, di, backdrop);
                }
            }

            return grp;
        }

        private static IEnumerable<Geometry> GetStyledSpans(Geometry geometry)
        {
            if (geometry is GeometryGroup gg)
            {
                if (!(TextRender.GetElement(gg) is null))
                {
                    yield return geometry;
                }
                else
                {
                    foreach (var g in gg.Children)
                    {
                        foreach (var subg in GetStyledSpans(g))
                        {
                            yield return subg;
                        }
                    }
                }
            }
            else
            {
                yield return geometry;
            }
        }

        private void AddDrawingToGroup(DrawingGroup grp, Shape shape, Drawing drawing, BackdropContext backdrop = null)
        {
            var mask = shape.Mask;
            bool filtered = UseFilters && shape.Filter != null;
            bool animated = animationScene != null && animationScene.Tracks(shape).Count > 0;
            if (shape.Clip != null || shape.Transform != null || filtered || mask != null || animated || shape is Group)
            {
                var subgrp = new DrawingGroup();
                if (mask != null)
                    subgrp.OpacityMask = CreateMask(mask, MaskDrawing.ObjectBounds(drawing));
                if (shape is Group || filtered || (animated && animationScene.Tracks(shape).Any(t => t.Attribute == "opacity")))
                    subgrp.Opacity = shape.Opacity;
                if (shape.Clip != null)
                    subgrp.ClipGeometry = shape.Clip.GetGeometry(MaskDrawing.ObjectBounds(drawing), null, animationScene == null ? (Func<Shape, Geometry, Geometry>)null : animationScene.AttachClip);
                if (shape.Transform != null)
                {
                    subgrp.Transform = shape.Transform;
                    if (!animated || !animationScene.Tracks(shape).Any(t => t.Type != null))
                        subgrp.Transform = new MatrixTransform(CssTransforms.Around(CssTransforms.Resolve(shape, MaskDrawing.ObjectBounds(drawing)), CssTransforms.Origin(shape, MaskDrawing.ObjectBounds(drawing))));
                }
                if (filtered)
                {
                    drawing = shape.Filter.Render(drawing, animationScene != null, this, shape.Filter.UsesBackground ? FilterBackdrop(shape,drawing,grp,backdrop) : null);
                }
                subgrp.Children.Add(drawing);
                if (animated) animationScene.Attach(shape, subgrp);
                grp.Children.Add(subgrp);
            }
            else
                grp.Children.Add(drawing);
        }

        private Brush CreateMask(Mask mask, Rect bounds)
        {
            if (bounds.IsEmpty || !activeMasks.Add(mask)) return Brushes.Transparent;
            try
            {
                // Paint overrides apply to the visible artwork, not mask luminance.
                var renderer = new SVGRender(ExternalFileLoader)
                {
                    SVG = SVG, activeMasks = activeMasks, UseFilters = UseFilters, RasterizationScale = RasterizationScale, animationScene = animationScene, activeFilterImages = activeFilterImages, activeImageResources = activeImageResources, needsBackground = needsBackground
                };
                int before = animationScene?.ClockCount ?? 0;
                var drawing = renderer.LoadGroup(mask.Elements, null, false);
                if (mask.ContentUsesBoundingBox)
                    drawing.Transform = new MatrixTransform(bounds.Width, 0, 0, bounds.Height, bounds.X, bounds.Y);
                var maskGroup = new DrawingGroup();
                maskGroup.Children.Add(drawing);
                var region = mask.GetRegion(bounds);
                if (region.IsEmpty) return Brushes.Transparent;
                bool linear = mask.ColorInterpolation == "linearRGB";
                if (animationScene != null && animationScene.ClockCount > before && !mask.UsesAlpha && !MaskDrawing.WhiteOnly(maskGroup))
                    return animationScene.AnimatedMask(maskGroup, region, false, linear);
                return MaskDrawing.Create(maskGroup, region, mask.UsesAlpha, linear);
            }
            finally
            {
                activeMasks.Remove(mask);
            }
        }
    }
}
