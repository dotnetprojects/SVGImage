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

Supported timing includes numeric `begin` offsets, finite or indefinite `repeatCount`, `fill="freeze"`, explicit `keyTimes`, linear/discrete interpolation and `keySplines`. Motion animation supports inline paths, local `mpath` references, path tangents (`auto`/`auto-reverse`), numeric rotation and `keyPoints` with `keyTimes`. Supported SMIL properties are `animateTransform` translation, scale, rotation and skew, timed `display`/`visibility`, opacity, circle/ellipse radii and center coordinates, and root `viewBox` camera movement. Additive transforms compose in SVG order. Animations can target their parent or a local `href`/`xlink:href` reference.

CSS `@keyframes` supports transform and opacity animations, grouped offsets, multiple animations, shorthand and longhand declarations, positive/negative delays, fractional/indefinite iterations, reverse/alternate directions, fill modes and an initially paused state. Timing functions include the ease presets, cubic-bezier curves and the steps jump modes. Matching transform functions interpolate their parameters, preserving full rotations. Static/animated transforms support `transform-origin` with view-box or fill-box reference bounds. CSS must be included in a `<style>` element in the SVG; an HTML page's external styles are not automatically loaded. Styles may occur after their targets. Percentage translations, general transform-matrix decomposition and interactive play-state changes are not implemented.

`symbol`/`use` rendering honors symbol viewBoxes, use viewport dimensions, alignment and aspect ratio. This is needed for Flash2Svg exports that assemble facial parts from symbols. Static matrix/saturation color filters used by these exports are applied to vector paint colors. Raster turbulence/displacement filter chains are described in [Rendering.md](Rendering.md); animated filter parameters are not implemented.

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

`SVGRender.AnimationWarnings` reports unsupported animated attributes and malformed SMIL tracks. Flash2Svg's empty final spline segment is treated as linear. JavaScript, HTML audio/foreignObject playback, path morphing, syncbase timing, animated masks and CSS layout/background animations are not implemented. In particular, the LoveDota example's visual camera/character timeline renders, while its JavaScript-triggered embedded audio is not played.

The regression suite includes synthetic timing, sprite-sheet, live-clock, symbol viewport, camera and color-filter checks. Downloaded examples are deliberately kept outside this repository. To additionally validate local copies of the Flash2Svg examples and extracted CodePen horse, set `SVG_ANIMATION_EXAMPLES` to a folder containing `Flash2Svg.wiki/img/*.svg` and `horse.svg`, then run the regression runner. The extra checks load every file, trigger click-start timelines where present and require a changed rendered frame.
