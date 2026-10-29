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

Standalone filters and chains support named results, `SourceGraphic` / `SourceAlpha`, primitive subregions, coordinate units and sRGB / linearRGB conversion.

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

Element opacity is applied **after** filtering. Fill/stroke opacity is part of `SourceGraphic`.

Raster filters sample inputs **at load time**. Animating their source/parameters does not refresh the bitmap. Single matrix/saturation filters can retain vector paints with animation enabled. Rasterization uses up to four pixels per user unit, capped at 2048 pixels on the longest dimension.

Lighting, general `feImage` inputs and backdrop sources are not yet implemented. Unsupported primitives currently pass their input through. [Rendering details](Rendering.md).

## CSS

The author cascade handles presentation attributes, stylesheet rules, inline declarations, specificity/source order, `!important`, supported CSS-wide keywords and custom-property inheritance. `var()` supports nested fallbacks and cycles.

Selectors include type, universal, ID, class, compound, attribute presence/equality, `:root`, descendant/child and adjacent/general sibling relationships. Unsupported selectors and conditional at-rules are ignored as whole rules.

Styles belong in the SVG's `<style>` element or inline/presentation attributes. External HTML-page styles, CSS layout/background behavior and arbitrary browser CSS are outside this implementation. [CSS guide](CSS.md).

## Animation

| Mechanism | Current scope |
| --- | --- |
| Native playback | WPF animation clocks. The control's `UseAnimations` defaults to true. |
| SMIL transforms | Translation, scale, rotation, skew and additive composition. |
| SMIL properties | Timed display/visibility, opacity, circle/ellipse radii/centers, root viewBox cameras. |
| SMIL motion | `animateMotion`, inline/local `mpath`, tangent/numeric rotation and key points/times. |
| SMIL timing | Numeric starts, repeat counts/durations, freeze, key times, linear/discrete/spline and explicit click starts. |
| Animated clips | Native circle/ellipse centers and radii, plus clip transforms; unions, static parent transforms and bounding-box mapping retain the clocks. |
| CSS keyframes | Transform/opacity, multiple animations, shorthand/longhands, grouped offsets, delays, iterations, directions, fill and an initial paused state. |
| CSS easing | Ease presets, cubic-bezier and steps; matching transform parameters preserve full rotations. |
| Host controls | `SeekAnimations` snapshots and `TriggerAnimationEvent` forwarding. |

Seeking stops playback on the existing drawing; reload/recreate it to restart. The host forwards click events explicitly; the control does not infer SVG element hit targets.

JavaScript/audio/HTML `foreignObject` playback, path morphing, syncbase timing, animated masks, general matrix decomposition, percentage translations and interactive CSS play-state changes remain unsupported. [Animation guide](Animations.md).

## Verification

Nine library targets build: .NET Framework 4.0, 4.5, 4.6, 4.7, 4.8; .NET Core 3.1; and .NET 6, 7, 8 for Windows.

Coverage includes the [#118 mask](https://github.com/dotnetprojects/SVGImage/issues/118) (93,767 flat interior pixels against Chromium), synthetic checks, a real advancing clock and 14 external animation examples.

A local resvg checkout enables **223 selected external SVG checks**: 219 reference-image comparisons and four render-only cases with nonportable/undefined expectations. Tolerances allow WPF antialiasing, tiled edges, fractional morphology radii and resolution-dependent convolution kernels. Eleven portable convolution images and all eight drop-shadow images are included. Malformed/UB convolution images are excluded; malformed kernel counts use the current Filter Effects pass-through behavior instead of historical SVG 1.1 error rendering. The full resvg suite remains a work in progress.

```powershell
$env:RESVG_TEST_SUITE = 'C:\path\to\resvg-test-suite'
dotnet run --project Tests/RegressionTests -p:TargetFrameworks=net8.0-windows -p:GeneratePackageOnBuild=false
```

Rendering checks need a working Windows graphics session. A plain-WPF preflight reports blank rendering before SVG comparisons run. Downloaded references and animation examples remain outside the repository.

## Example application

The **Flash2Svg examples** tab lists all 13 animations from [Tom Byrne's examples page](https://github.com/TomByrne/Flash2Svg/wiki/Examples), with visible attribution and a link to the project's MIT license. Select an example to download it into `%LOCALAPPDATA%\SVGImage\Flash2Svg\Examples`; subsequent runs use the cached copy. Only the selected preview runs. Play/restart forwards the Viber examples' SMIL click events. LoveDota's embedded JavaScript/audio is not executed. Downloaded artwork stays outside the repository.
