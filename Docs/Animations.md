# Animated SVGs

`SVGImage` plays supported SVG animations using native WPF animation clocks:

```xml
<svg:SVGImage UriSource="animation.svg"
              UseAnimations="True"
              SizeType="ViewBoxToSizeNoStretch" />
```

`UseAnimations` defaults to true. It also works when the source is changed after initialization. Set it to false to render the static document. With `SVGRender`, opt in before loading:

```csharp
var renderer = new SVGImage.SVG.SVGRender { UseAnimations = true };
DrawingGroup drawing = renderer.LoadDrawing(filename);
```

Supported timing includes numeric `begin` offsets, finite or indefinite `repeatCount`, `fill="freeze"`, explicit `keyTimes`, linear/discrete interpolation and `keySplines`. Supported SMIL properties are `animateTransform` translation, scale, rotation and skew, timed `display`/`visibility`, opacity, circle/ellipse radii and center coordinates, and root `viewBox` camera movement. Additive transforms compose in SVG order. Animations can target their parent or a local `href`/`xlink:href` reference.

CSS `@keyframes` transform animations support a named `animation` shorthand with duration, optional `infinite` repetition, linear interpolation and `steps(N)` / `steps(N, end)`. This covers SVG sprite sheets such as the 16-frame horse example. CSS must be included in a `<style>` element in the SVG; an HTML page's external styles are not automatically loaded. Styles are collected before shapes, so a style element may occur after its targets. The supported transform functions are translate, translateX/Y, scale, rotate and matrix. This is a limited CSS animation subset, not a complete CSS engine.

`symbol`/`use` rendering honors symbol viewBoxes, use viewport dimensions, alignment and aspect ratio. This is needed for Flash2Svg exports that assemble facial parts from symbols. Static matrix/saturation color filters used by these exports are applied to vector paint colors. General SVG filter graphs and animated filter parameters are not implemented.

For deterministic snapshots or exports, stop playback at an exact time:

```csharp
renderer.SeekAnimations(TimeSpan.FromSeconds(25));
// Render drawing into a RenderTargetBitmap, for example.
```

`SeekAnimations` stops playback and evaluates the existing drawing; load/recreate the drawing to restart its native clocks. The same method is available on the control. For SMIL click-start timing, explicitly forward an event:

```csharp
svgControl.TriggerAnimationEvent("playButton", "click");
// Or renderer.TriggerAnimationEvent("playButton", "click").
```

This starts tracks whose `begin` names `playButton.click`, including numeric offsets. The control does not infer SVG element hit targets; the host decides which element/event to forward. Numeric event timing can also be used with snapshots.

`SVGRender.AnimationWarnings` reports unsupported animated attributes and malformed SMIL tracks. Flash2Svg's empty final spline segment is treated as linear. JavaScript, HTML audio/foreignObject playback, path morphing, animateMotion, syncbase timing, animated masks, CSS layout/background animations and the remainder of the CSS/SMIL standards are not implemented. In particular, the LoveDota example's visual camera/character timeline renders, while its JavaScript-triggered embedded audio is not played.

The regression suite includes synthetic timing, sprite-sheet, live-clock, symbol viewport, camera and color-filter checks. Downloaded examples are deliberately kept outside this repository. To additionally validate local copies of the Flash2Svg examples and extracted CodePen horse, set `SVG_ANIMATION_EXAMPLES` to a folder containing `Flash2Svg.wiki/img/*.svg` and `horse.svg`, then run the regression runner. The extra checks load every file, trigger click-start timelines where present and require a changed rendered frame.
