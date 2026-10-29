using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml;
using SVGImage.SVG.Shapes;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG.Animation
{
    internal sealed class SvgAnimationScene
    {
        private readonly SVG svg;
        private readonly SmilTimeline timeline;
        private readonly Dictionary<Shape, List<AnimationTrack>> tracks = new Dictionary<Shape, List<AnimationTrack>>();
        private readonly List<AnimationClock> clocks = new List<AnimationClock>();
        private readonly List<Action<double>> snapshots = new List<Action<double>>();
        private readonly Dictionary<string, double> events = new Dictionary<string, double>();
        internal readonly List<string> Warnings = new List<string>();
        private double time;

        internal SvgAnimationScene(SVG svg) { this.svg = svg; timeline = new SmilTimeline(svg); }

        internal List<AnimationTrack> Tracks(Shape shape)
        {
            if (shape == null) return new List<AnimationTrack>();
            List<AnimationTrack> result;
            if (tracks.TryGetValue(shape, out result)) return result;
            result = new List<AnimationTrack>();
            tracks[shape] = result;
            var nodes = new List<XmlNode>(shape.AnimationNodes);
            List<XmlNode> referenced;
            if (svg.ReferencedAnimations.TryGetValue(shape.Id, out referenced)) nodes.AddRange(referenced);
            foreach (var node in nodes)
            {
                try
                {
                    var track = timeline.Track(node);
                    if(track.Type=="motion" && track.MotionPath==null)
                    {
                        foreach(XmlNode child in node.ChildNodes) if(child.LocalName=="mpath")
                        {
                            string href=XmlUtil.AttrValue(child,"href",XmlUtil.AttrValue(child,"xlink:href"));
                            if(href.StartsWith("#") && svg.GetShape(href.Substring(1)) is PathShape path)
                            {
                                track.MotionPath=SvgPathData.Parse(path.Data);

                            }
                        }
                    }
                    if (track.Type != null || new[] { "display", "visibility", "opacity", "r", "rx", "ry", "cx", "cy", "viewBox", "d" }.Contains(track.Attribute))
                        result.Add(track);
                    else Warnings.Add("Unsupported animated attribute: " + track.Attribute);
                }
                catch (Exception e) when (e is FormatException || e is ArgumentException || e is OverflowException)
                { Warnings.Add(shape.Id + ": " + e.Message); }
            }
            result.AddRange(CssAnimations.Build(svg, shape));
            return result;
        }

        internal Geometry AttachClip(Shape shape, Geometry geometry)
        {
            if (Tracks(shape).Count == 0) return geometry;
            if (geometry.IsFrozen) geometry = geometry.Clone();
            var group = new DrawingGroup { Transform = geometry.Transform };
            geometry.Transform = null;
            group.Children.Add(new GeometryDrawing(Brushes.Black, null, geometry));
            Geometry animated = geometry;
            if (shape is CircleShape || shape is EllipseShape || shape is PathShape)
                while (animated is GeometryGroup wrapper && wrapper.Children.Count == 1) animated = wrapper.Children[0];
            Attach(shape, group, animated);
            return new GeometryGroup { Children = { geometry }, Transform = group.Transform };
        }

        internal void Attach(Shape shape, DrawingGroup group, Geometry animatedGeometry = null)
        {
            var items = Tracks(shape);
            if (items.Count == 0) return;
            var transforms = items.Where(t => t.Type != null).ToArray();
            var cameras = items.Where(t => t.Attribute == "viewBox").ToArray();
            if (transforms.Length > 0 || cameras.Length > 0)
            {
                var reference = CssTransforms.ReferenceBox(shape, MaskDrawing.ObjectBounds(group.Children[0]));
                Matrix original = shape.Transform == null ? group.Transform?.Value ?? Matrix.Identity : CssTransforms.Resolve(shape, MaskDrawing.ObjectBounds(group.Children[0]));
                var origin = CssTransforms.Origin(shape, MaskDrawing.ObjectBounds(group.Children[0]));
                foreach (var track in transforms) { track.UnderlyingMatrix = original; track.UnderlyingTransform = shape.TransformText; track.TransformReference = reference; }
                Func<double, Matrix> sample = seconds =>
                {
                    var matrix = original;
                    foreach (var track in transforms)
                    {
                        int index; double fraction;
                        if (!track.Sample(seconds, events, out index, out fraction)) continue;
                        var value = track.Matrix(index, fraction);
                        if (track.Additive) matrix.Prepend(value);
                        else matrix = value;
                    }
                    foreach (var track in cameras)
                    {
                        int index; double fraction;
                        if (!track.Sample(seconds, events, out index, out fraction)) continue;
                        var box = track.Components(index, fraction);
                        if (box.Length != 4 || box[2] <= 0 || box[3] <= 0) continue;
                        var viewport = svg.ViewBox ?? new Rect(svg.Size);
                        double scale = Math.Min(viewport.Width / box[2], viewport.Height / box[3]);
                        matrix = new Matrix(scale, 0, 0, scale, viewport.X + (viewport.Width - box[2] * scale) / 2 - box[0] * scale,
                            viewport.Y + (viewport.Height - box[3] * scale) / 2 - box[1] * scale);
                    }
                    return CssTransforms.Around(matrix, origin);
                };
                var transform = new MatrixTransform(sample(0));
                group.Transform = transform;
                var clock = (AnimationClock)new MatrixSampler { Sample = sample, Duration = Duration.Forever }.CreateClock(true);
                transform.ApplyAnimationClock(MatrixTransform.MatrixProperty, clock);
                clocks.Add(clock);
                snapshots.Add(t => { transform.ApplyAnimationClock(MatrixTransform.MatrixProperty, null); transform.Matrix = sample(t); });
            }
            var opacity = items.Where(t => t.Attribute == "display" || t.Attribute == "visibility" || t.Attribute == "opacity").ToArray();
            if (opacity.Length > 0)
            {
                double original = group.Opacity;
                Func<double, double> sample = seconds =>
                {
                    double value = original;
                    bool visible = shape.Display;
                    foreach (var track in opacity)
                    {
                        int index; double fraction;
                        if (!track.Sample(seconds, events, out index, out fraction)) continue;
                        if (track.Attribute == "display" || track.Attribute == "visibility")
                            visible = track.Values[index] != "none" && track.Values[index] != "hidden";
                        else value = Math.Max(0, Math.Min(1, track.Components(index, fraction)[0]));
                    }
                    return visible ? value : 0;
                };
                AddDouble(group, DrawingGroup.OpacityProperty, sample);
            }
            var path = (animatedGeometry ?? shape.geometryElement) as PathGeometry;
            var morphTrack = items.LastOrDefault(track => track.Attribute == "d");
            if (path != null && shape is PathShape pathShape && morphTrack != null)
            {
                try
                {
                    var morph = new PathMorph(pathShape.Data, morphTrack);
                    var originalFigures = path.Figures.Clone();
                    AddValue(path, PathGeometry.FiguresProperty, typeof(PathFigureCollection), seconds =>
                    {
                        int index; double fraction;
                        return morphTrack.Sample(seconds, events, out index, out fraction) ? morph.Sample(index, fraction) : originalFigures;
                    });
                }
                catch (FormatException ex) { Warnings.Add(shape.Id + ": " + ex.Message); }
            }
            // Keep the previously supported radius/center animations, now including
            // parent targets, explicit keyTimes, repeat counts and spline easing.
            var ellipse = (animatedGeometry ?? shape.geometryElement) as EllipseGeometry;
            if (ellipse != null)
            {
                foreach (string attribute in new[] { "r", "rx", "ry" })
                {
                    var track = items.LastOrDefault(t => t.Attribute == attribute);
                    if (track == null) continue;
                    if (attribute != "ry") AddDouble(ellipse, EllipseGeometry.RadiusXProperty, SampleNumber(track, ellipse.RadiusX));
                    if (attribute != "rx") AddDouble(ellipse, EllipseGeometry.RadiusYProperty, SampleNumber(track, ellipse.RadiusY));
                }
                var cx = items.LastOrDefault(t => t.Attribute == "cx");
                var cy = items.LastOrDefault(t => t.Attribute == "cy");
                if (cx != null || cy != null)
                {
                    var center = ellipse.Center;
                    Func<double, Point> sample = t => new Point(cx == null ? center.X : SampleNumber(cx, center.X)(t), cy == null ? center.Y : SampleNumber(cy, center.Y)(t));
                    var clock = (AnimationClock)new PointSampler { Sample = sample, Duration = Duration.Forever }.CreateClock(true);
                    ellipse.ApplyAnimationClock(EllipseGeometry.CenterProperty, clock);
                    clocks.Add(clock);
                    snapshots.Add(t => { ellipse.ApplyAnimationClock(EllipseGeometry.CenterProperty, null); ellipse.Center = sample(t); });
                }
            }
        }

        internal int ClockCount => clocks.Count;
        internal Brush AnimatedMask(DrawingGroup drawing, Rect region, bool alpha, bool linear)
        {
            Brush cached = null; long frame = long.MinValue;
            Func<double, bool, Brush> render = (t, force) =>
            {
                long next = (long)Math.Floor(t * 30);
                if (force || cached == null || next != frame)
                { cached = MaskDrawing.Create(drawing, region, alpha, linear, 512); frame = next; }
                return cached;
            };
            var paint = new GeometryDrawing(render(0, true), null, new RectangleGeometry(region));
            AddValue(paint, GeometryDrawing.BrushProperty, typeof(Brush), t => render(t, false), t => render(t, true));
            var content = new DrawingGroup(); content.Children.Add(paint);
            return new DrawingBrush(content) { ViewboxUnits = BrushMappingMode.Absolute, Viewbox = region,
                ViewportUnits = BrushMappingMode.Absolute, Viewport = region, Stretch = Stretch.Fill };
        }
        private void AddValue(Animatable target, DependencyProperty property, Type type, Func<double, object> sample, Func<double, object> snapshot = null)
        {
            var clock = (AnimationClock)new ValueSampler { Sample = sample, ValueType = type, Duration = Duration.Forever }.CreateClock(true);
            target.ApplyAnimationClock(property, clock); clocks.Add(clock);
            snapshots.Add(t => { target.ApplyAnimationClock(property, null); target.SetValue(property, (snapshot ?? sample)(t)); });
        }
        private sealed class ValueSampler : AnimationTimeline
        {
            internal Func<double, object> Sample;
            internal Type ValueType;
            public override Type TargetPropertyType => ValueType;
            protected override Freezable CreateInstanceCore() => new ValueSampler { Sample = Sample, ValueType = ValueType };
            public override object GetCurrentValue(object origin, object destination, AnimationClock clock) => Sample(clock.CurrentTime?.TotalSeconds ?? 0);
        }

        private Func<double, double> SampleNumber(AnimationTrack track, double original)
        {
            return t => { int i; double f; return track.Sample(t, events, out i, out f) ? track.Components(i, f)[0] : original; };
        }

        private void AddDouble(Animatable target, DependencyProperty property, Func<double, double> sample)
        {
            var clock = (AnimationClock)new DoubleSampler { Sample = sample, Duration = Duration.Forever }.CreateClock(true);
            target.ApplyAnimationClock(property, clock);
            clocks.Add(clock);
            snapshots.Add(t => { target.ApplyAnimationClock(property, null); target.SetValue(property, sample(t)); });
        }

        internal void Seek(TimeSpan position)
        {
            if (position < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(position));
            time = position.TotalSeconds;
            foreach (var clock in clocks) clock.Controller.Stop();
            foreach (var snapshot in snapshots) snapshot(time);
        }

        internal void Trigger(string id, string name)
        {
            double current = clocks.Count > 0 && clocks[0].CurrentTime.HasValue ? clocks[0].CurrentTime.Value.TotalSeconds : time;
            events[id + "." + name] = current;
            timeline.Trigger(id + "." + name, current);
            // In snapshot mode re-evaluate immediately; running clocks update on the next tick.
            if (clocks.Count > 0 && clocks[0].CurrentState == ClockState.Stopped)
                foreach (var snapshot in snapshots) snapshot(time);
        }

        private sealed class MatrixSampler : MatrixAnimationBase
        {
            internal Func<double, Matrix> Sample;
            protected override Freezable CreateInstanceCore() => new MatrixSampler { Sample = Sample };
            protected override Matrix GetCurrentValueCore(Matrix origin, Matrix destination, AnimationClock clock) => Sample(clock.CurrentTime?.TotalSeconds ?? 0);
        }
        private sealed class DoubleSampler : DoubleAnimationBase
        {
            internal Func<double, double> Sample;
            protected override Freezable CreateInstanceCore() => new DoubleSampler { Sample = Sample };
            protected override double GetCurrentValueCore(double origin, double destination, AnimationClock clock) => Sample(clock.CurrentTime?.TotalSeconds ?? 0);
        }
        private sealed class PointSampler : PointAnimationBase
        {
            internal Func<double, Point> Sample;
            protected override Freezable CreateInstanceCore() => new PointSampler { Sample = Sample };
            protected override Point GetCurrentValueCore(Point origin, Point destination, AnimationClock clock) => Sample(clock.CurrentTime?.TotalSeconds ?? 0);
        }
    }
}
