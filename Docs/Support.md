---
title: SVG support reference
description: Supported features, practical limits and regression coverage in the current SVGImage source.
---
# SVG support reference

This page describes the **current repository source**, including [unreleased work](Changelog.md). Published NuGet packages may contain an earlier feature set. SVGImage runs on **Windows / WPF**.

## Geometry and composition

| Feature | Current scope | Details |
| --- | --- | --- |
| Basic geometry | Rectangles, rounded rectangles, circles, ellipses, lines, polygons and polylines | Empty point lists are safe; valid pairs before invalid data are preserved. |
| Paths | Absolute/relative commands, curves, arcs and reflected controls | Adjacent arc flags; parsing stops at the last complete segment on invalid data. |
| Groups / transforms | Group composition and 2D transforms | Rotation, translation, scale, skew and matrices. |
| `clipPath` | Combined silhouettes, `clip-rule`, transforms, text and local `use` | User-space/bounding-box units, forward references, empty clips. |
| `mask` | Luminance and alpha, nesting and references | Region/content units, percentages, transforms and cycle guards. [Mask guide](Masks.md). |
| `symbol` / `use` | Local references and reusable viewports | ViewBoxes, alignment, aspect ratios and recursion guards. |
| Nested `svg` | Independent viewports and percentage contexts | Position, size, viewBox, meet/slice/none and overflow clipping. |
| Images | Raster content, including embedded images | Images can participate in masks and grayscale conversion. |

## Paint and text

| Feature | Current scope | Details |
| --- | --- | --- |
| Fills / strokes | Solid colors, widths, caps, joins and dash arrays | Some CSS length/layout cases remain incomplete. |
| Gradients | Linear/radial paints, stops and spreading | Stop opacity, focal points, pad/repeat/reflect and per-use opacity. General template inheritance is incomplete. |
| `pattern` | Repeated SVG content | Tile/content units, viewBoxes, transforms, opacity and local templates. |
| Colors | Names, RGB/RGBA, HSL/HSLA and hex | Modern RGB/RGBA mixed numbers/percentages, slash alpha, legacy commas, clamping and short/long RGBA hex notation. |
| Text | Glyph outlines, styled spans, anchoring and basic layout | Complete bidirectional/complex-script layout and every font property are not implemented. |
| `textPath` | Local path references and tangent-aligned glyphs | Percentage `startOffset`, anchoring and spans. Stretching, vertical path text and `pathLength` calibration remain unsupported. |
| Grayscale / overrides | `IsGrayscale`, drawing conversion and color/stroke overrides | [Grayscale guide](Grayscale.md). |

## Filters

Standalone filters and chains support named results, `SourceGraphic` / `SourceAlpha`, `BackgroundImage` / `BackgroundAlpha`, primitive subregions, coordinate units and sRGB / linearRGB conversion.

| Primitive | Current scope |
| --- | --- |
| `feTurbulence` | Four seeded channels, turbulence/fractal noise, one/two frequencies, up to nine octaves, stitching. |
| `feDisplacementMap` | Unpremultiplied R/G/B/A selectors, signed scale, bilinear sampling. |
| `feFlood` | CSS color/opacity, including user-space regions on empty groups. |
| `feOffset` / `feMerge` | Translated inputs and compositing of named inputs. |
| `feComposite` | Over, in, out, atop, xor and arithmetic. |
| `feColorMatrix` | Matrix, saturation, hue rotation and luminance-to-alpha. |
| `feComponentTransfer` | Identity, table, discrete, linear and gamma per channel. |
| `feBlend` | Normal, multiply, screen, darken/lighten, overlay, hard/soft light, dodge/burn, difference, exclusion, hue, saturation, color and luminosity. |
| `feGaussianBlur` | Independent deviations; three box convolutions approximate a Gaussian. |
| `feMorphology` | Erosion and dilation using sliding extrema. |
| `feTile` | Input subregion repetition, including transparent pixels. |
| `feConvolveMatrix` | Rotated kernels, rectangular orders, divisor/bias, targets, duplicate/wrap/transparent edges, kernel spacing and preserved alpha. |
| `feDropShadow` | Source-alpha blur, offset, flood color/opacity and source-over composition. |
| `feDiffuseLighting` / `feSpecularLighting` | Alpha height maps, edge/corner normals, surface scale, kernel spacing, CSS lighting color and diffuse/specular constants. |
| `feDistantLight` / `fePointLight` / `feSpotLight` | Azimuth/elevation, positions, target direction, cone limits and spot/specular exponents. |
| `feImage` | Local SVG elements, raster/SVG data URIs, external raster/SVG through the host loader, primitive subregions and aspect-ratio placement; guarded cyclic references. |

`SVGRender.UseFilters` defaults to true. Disable it for a quick preview, then re-enable it and call `CreateDrawing(renderer.SVG)` to render the same parsed document with filters. The sample car uses both passes on a background STA and publishes separate frozen drawings. Clipping, masks and opacity remain active in the preview.

Element opacity is applied **after** filtering. Fill/stroke opacity is part of `SourceGraphic`.

Raster filters sample inputs **at load time**. Animating their source/parameters does not refresh the bitmap. Single matrix/saturation filters can retain vector paints with animation enabled. `SVGRender.RasterizationScale` sets pixels per user unit (default four), capped at 2048 pixels on the longest dimension. Default lighting derivatives are pixel based, so lighting changes with the sampling resolution; set the scale to the intended output resolution when comparing reference images.

Backdrops use the SVG 1.1 `enable-background` model: an ancestor `new` enables capture; `accumulate` preserves the buffer; nested `new` resets it. Optional `new x y width height` clips the captured region. Filters see preceding siblings in their coordinate space, before ancestor group opacity/filter/clip/mask composition. Explicit `inherit` inherits the parent value, including a `new` reset. This does not implement CSS `backdrop-filter` on HTML.

External `feImage` loads go through `IExternalFileLoader` (local files and pack resources by default); network access requires a host loader. Local references use SVG element coordinates rather than fitting their object bounds. External SVG fragments use the document viewport; full SVG image `defer`/intrinsic-sizing semantics remain incomplete. Unsupported primitives currently pass their input through. [Rendering details](Rendering.md).

## CSS

The author cascade handles presentation attributes, stylesheet rules, inline declarations, specificity/source order, `!important`, supported CSS-wide keywords and custom-property inheritance. `var()` supports nested fallbacks and cycles.

Selectors include type, universal, ID, class, compound, attribute presence/equality, `:root`, descendant/child and adjacent/general sibling relationships. Unsupported selectors and conditional at-rules are ignored as whole rules.

Styles belong in the SVG's `<style>` element or inline/presentation attributes. External HTML-page styles, CSS layout/background behavior and arbitrary browser CSS are outside this implementation. [CSS guide](CSS.md).

## Animation

| Mechanism | Current scope |
| --- | --- |
| Native playback | WPF animation clocks. The control's `UseAnimations` defaults to true. |
| SMIL transforms | Translation, scale, rotation, skew and additive composition. |
| Path morphing | Native `d` animation for matching normalized figures/segments and arc flags; discrete switching supports incompatible paths. |
| SMIL properties | Timed display/visibility, opacity, circle/ellipse radii/centers, root viewBox cameras. |
| SMIL motion | `animateMotion`, inline/local `mpath`, tangent/numeric rotation and key points/times. |
| SMIL timing | Numeric/event starts, forward `id.begin` / `id.end` chains with offsets, seeded self loops, repeat counts/durations, freeze, key times and linear/discrete/spline. |
| Animated clips | Native circle/ellipse centers and radii, plus clip transforms; unions, static parent transforms and bounding-box mapping retain the clocks. |
| CSS keyframes | Transform/opacity, multiple animations, shorthand/longhands, grouped offsets, delays, iterations, directions, fill and an initial paused state. |
| Animated masks | Vector alpha/white luminance masks; composited colored luminance refreshes up to 30 Hz at 512 pixels on the longest side. |
| CSS transforms | Percentage translations using viewport/fill-box bounds; 2D matrix decomposition with signed scales, rotation and residual skew, preserving compatible function prefixes. |
| CSS easing | Ease presets, cubic-bezier and steps; matching transform parameters preserve full rotations. |
| Host controls | `SeekAnimations` snapshots and `TriggerAnimationEvent` forwarding. |

Seeking stops playback on the existing drawing; reload/recreate it to restart. The host forwards click events explicitly; the control does not infer SVG element hit targets.

JavaScript/audio/HTML `foreignObject` playback, arbitrary path topology normalization, 3D matrix decomposition and interactive CSS play-state changes remain unsupported. Syncbase timing currently supports acyclic chains and seeded self loops; multi-animation dependency cycles, explicit SMIL end/restart rules and general event names remain incomplete. Mask region attributes and filtered mask content still retain their existing static sampling limits. [Animation guide](Animations.md).

## Verification

Nine library targets build: .NET Framework 4.0, 4.5, 4.6, 4.7, 4.8; .NET Core 3.1; and .NET 6, 7, 8 for Windows.

Coverage includes the [#118 mask](https://github.com/dotnetprojects/SVGImage/issues/118) (93,767 flat interior pixels against Chromium), synthetic checks, a real advancing clock and 14 external animation examples.

A local resvg checkout enables **321 selected external SVG checks**: 312 reference-image comparisons and nine render-only cases with historical/nonportable/undefined expectations. The 98 lighting/image/backdrop additions include three historical specular images whose saturated channels overflow, one inherited-background image that retains an ancestor buffer instead of resetting it, and one explicitly undefined masked-backdrop image. Focused tests enforce channel clamping and explicit inheritance/reset behavior. Tolerances allow WPF antialiasing, tiled edges, fractional morphology radii and resolution-dependent convolution kernels. Eleven portable convolution images and all eight drop-shadow images are included. Malformed/UB convolution images are excluded; malformed kernel counts use the current Filter Effects pass-through behavior instead of historical SVG 1.1 error rendering. The full resvg suite remains a work in progress.

```powershell
$env:RESVG_TEST_SUITE = 'C:\path\to\resvg-test-suite'
dotnet run --project Tests/RegressionTests -p:TargetFrameworks=net8.0-windows -p:GeneratePackageOnBuild=false
```

Rendering checks need a working Windows graphics session. A plain-WPF preflight reports blank rendering before SVG comparisons run. Downloaded references and animation examples remain outside the repository.

## Example application

The **Filter features** tab contains three original offline demonstrations: diffuse/specular lighting with all three light types, local/relative/data-URI image inputs and backdrop glass. Each has inspectable SVG source and Reload.

The **Animation features** tab contains five original, offline examples with inspectable SVG source: path morphing, syncbase sequencing, alpha/luminance masks, matrix decomposition and percentage translations. Play/restart and an exact two-second snapshot are available for each.

The **Flash2Svg examples** tab lists all 13 animations from [Tom Byrne's examples page](https://github.com/TomByrne/Flash2Svg/wiki/Examples), with visible attribution and a link to the project's MIT license. Select an example to download it into `%LOCALAPPDATA%\SVGImage\Flash2Svg\Examples`; subsequent runs use the cached copy. Only the selected preview runs. Play/restart forwards the Viber examples' SMIL click events. LoveDota's embedded JavaScript/audio is not executed. Downloaded artwork stays outside the repository.
