using System.Windows.Media;
using System.Windows;

namespace SVGImage.SVG.Shapes
{
    public abstract class TextRenderBase
    {
        public abstract GeometryGroup BuildTextGeometry(TextShape text);
        public static DependencyProperty TSpanElementProperty = DependencyProperty.RegisterAttached(
            "TSpanElement", typeof(TextShapeBase), typeof(DependencyObject));
        public static void SetElement(DependencyObject obj, TextShapeBase value)
        {
            obj.SetValue(TSpanElementProperty, value);
        }
        public static TextShapeBase GetElement(DependencyObject obj)
        {
            return (TextShapeBase)obj.GetValue(TSpanElementProperty);
        }
    }




}
