using System.Windows.Media;
using System.Windows;

namespace SVGImage.SVG.Shapes
{
    public abstract class TextRenderBase
    {
        public abstract GeometryGroup BuildTextGeometry(TextShape text);
        public static DependencyProperty TSpanElementProperty = DependencyProperty.RegisterAttached(
            "TSpanElement", typeof(ITextNode), typeof(DependencyObject));
        public static void SetElement(DependencyObject obj, ITextNode value)
        {
            obj.SetValue(TSpanElementProperty, value);
        }
        public static ITextNode GetElement(DependencyObject obj)
        {
            return (ITextNode)obj.GetValue(TSpanElementProperty);
        }
    }




}
