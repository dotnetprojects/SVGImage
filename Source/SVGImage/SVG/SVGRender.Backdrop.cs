using System;
using System.Windows;
using System.Windows.Media;
using SVGImage.SVG.Shapes;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG
{
    public partial class SVGRender
    {
        private bool needsBackground;
        internal sealed class BackdropContext
        {
            internal Drawing Base;
            internal Rect? Region;
            internal Drawing Capture(DrawingGroup prefix)
            {
                var result=new DrawingGroup();
                if(Region.HasValue) result.ClipGeometry=new RectangleGeometry(Region.Value);
                if(Base!=null) result.Children.Add(Base);
                // Copy only preceding children: later siblings must never leak
                // into an earlier filter's snapshot.
                foreach(var drawing in prefix.Children) result.Children.Add(drawing);
                return result;
            }
        }
        private static bool NewBackdrop(Shape shape, out BackdropContext context)
        {
            context=null;
            string value;
            if(!(shape is Group) || !shape.CssProperties.TryGetValue("enable-background",out value) || !value.TrimStart().StartsWith("new")) return false;
            value=value.Trim();
            if(value=="new") { context=new BackdropContext(); return true; }
            var region=SvgNumberReader.List(value.Substring(3));
            if(region.Length==4 && region[2]>0 && region[3]>0)
                context=new BackdropContext { Region=new Rect(region[0],region[1],region[2],region[3]) };
            return true;
        }
        private BackdropContext ChildBackdrop(Shape shape, BackdropContext parent, DrawingGroup preceding, Matrix? placement=null)
        {
            if(!needsBackground || !UseFilters) return null;
            BackdropContext fresh;
            if(NewBackdrop(shape,out fresh)) return fresh;
            if(parent==null) return null;
            var matrix=placement ?? Matrix.Identity;
            if(shape.Transform!=null)
            {
                Rect localBounds=Rect.Empty;
                if(shape.TransformText!=null && shape.TransformText.Contains("%") && shape is Group group)
                    foreach(var child in group.Elements) localBounds.Union(ShapeGeometry.Create(child).Bounds);
                matrix.Append(CssTransforms.Around(CssTransforms.Resolve(shape,localBounds),CssTransforms.Origin(shape,localBounds)));
            }
            return new BackdropContext { Base=BackdropToLocal(parent.Capture(preceding),matrix) };
        }
        private static Drawing BackdropToLocal(Drawing drawing, Matrix transform)
        {
            if(drawing==null || !transform.HasInverse) return null;
            transform.Invert();
            return new DrawingGroup { Children={drawing}, Transform=new MatrixTransform(transform) };
        }
        private Drawing FilterBackdrop(Shape shape, Drawing source, DrawingGroup preceding, BackdropContext context)
        {
            BackdropContext fresh;
            if(context==null || NewBackdrop(shape,out fresh)) return null;
            var bounds=MaskDrawing.ObjectBounds(source);
            var transform=CssTransforms.Around(CssTransforms.Resolve(shape,bounds),CssTransforms.Origin(shape,bounds));
            return BackdropToLocal(context.Capture(preceding),transform);
        }
    }
}
