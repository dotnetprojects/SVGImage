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

CSS `@keyframes` supports transform and opacity animations, grouped offsets, multiple animations, shorthand and longhand declarations, positive/negative delays, fractional/indefinite iterations, reverse/alternate directions, fill modes and an initially paused state. Timing functions include the ease presets, cubic-bezier curves and the steps jump modes. Matching transform functions interpolate their parameters, preserving full rotations. Static/animated transforms support `transform-origin` with view-box or fill-box reference bounds. CSS must be included in a `<style>` element in the SVG; an HTML page's external styles are not automatically loaded. Styles may occur after their targets. Percentage translations resolve against the nearest SVG viewport or the target fill box when `transform-box:fill-box` is selected. Matrix values and incompatible transform-list tails interpolate through 2D decomposition (translation, signed scales, shortest rotation and residual skew); compatible prefixes retain parameter interpolation and full turns. Singular matrix endpoints use discrete fallback. Reference bounds are captured when the drawing is created. Interactive play-state changes and 3D decomposition are not implemented.

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

`SVGRender.AnimationWarnings` reports unsupported animated attributes and malformed SMIL tracks. Flash2Svg's empty final spline segment is treated as linear. JavaScript, HTML audio/foreignObject playback and CSS layout/background animations are not implemented. In particular, the LoveDota example's visual camera/character timeline renders, while its JavaScript-triggered embedded audio is not played.

The regression suite includes synthetic timing, sprite-sheet, live-clock, symbol viewport, camera and color-filter checks. Downloaded examples are deliberately kept outside this repository. To additionally validate local copies of the Flash2Svg examples and extracted CodePen horse, set `SVG_ANIMATION_EXAMPLES` to a folder containing `Flash2Svg.wiki/img/*.svg` and `horse.svg`, then run the regression runner. The extra checks load every file, trigger click-start timelines where present and require a changed rendered frame.

Circle/ellipse clip centers and radii and clip transforms retain native clocks through union geometry and static coordinate wrappers. SMIL `repeatDur` accepts finite clock durations or `indefinite`, and finite frozen durations can stop partway through an iteration. The sample app includes a separate Flash2Svg examples tab with source attribution and cached downloads; it forwards explicit click triggers without executing SVG scripts/audio.

### Path morphing, synchronization and masks

SMIL `animate attributeName="d"` supports `values`, from/to and to-only interpolation on matching normalized figures, segment types and arc flags. Lines, quadratic/cubic curves and arcs interpolate natively; incompatible paths report an animation warning. `calcMode="discrete"` can switch between different path topologies. Morphs work in ordinary drawings, clip paths and masks. Automatic path-topology matching and changing arc flags during interpolation are outside this scope.

`begin="other.begin+0.2s"` and `begin="other.end-0.1s"` resolve forward animation-ID references and chains. End references include repeat counts/repeat durations. Click starts propagate through these chains, earlier click instances remain available for seeking, and a seeded `begin="0s;self.end+1s"` loop can repeat without accumulating frame events. Unseeded cycles remain inactive. Multi-animation dependency cycles and the complete SMIL end/restart/event model remain incomplete.

Alpha and white-only luminance masks retain vector animation clocks. Masks that need luminance compositing rerasterize the animated mask content, including black-over-white occlusion, up to 30 times per second at a maximum dimension of 512 pixels. Seeking refreshes immediately even within the same playback-frame bucket. Mask region attributes are fixed at load time; raster filter inputs inside masks retain the normal filter animation limits.

The sample's **Animation features** tab includes an original example of each feature, its SVG source, native playback and an exact two-second snapshot. These five files are also exercised by the regression runner.
