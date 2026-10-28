# Grayscale rendering

Set `IsGrayscale` to display an SVG in grayscale without replacing every color with the same value:

```xml
<svg:SVGImage UriSource="/Images/icon.svg" IsGrayscale="True" />
```

The conversion preserves alpha, gradient stops, strokes, and vector geometry. Embedded bitmap images are converted to grayscale with their alpha preserved. Color overrides are applied before conversion. Set `IsGrayscale` back to `False` to display the original colors.

To obtain and modify a rendered drawing yourself, use `SVGRender`:

```csharp
var renderer = new SVGImage.SVG.SVGRender();
Drawing original = renderer.LoadDrawing("icon.svg");
Drawing gray = SVGImage.SVG.Utils.DrawingColorConverter.ToGrayscale(original);
svgControl.SetImage(gray);
// Or use new DrawingImage(gray) as a WPF ImageSource.
```

`ToGrayscale` returns a copy, so the original drawing can be shared with other controls. It uses the weighted RGB luminance `0.299 R + 0.587 G + 0.114 B`. Drawings with active animations are converted at their current values.
