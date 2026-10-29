# Changelog

Changes to SVGImage are recorded here, newest first. Dates use the project's local calendar (Europe/Berlin).

## Unreleased

These entries describe the current repository source. They have not been assigned a NuGet release version. Install a published package for its released feature set, or build the current source to use these additions.

### 2026-10-29

#### Added

- **Documentation:** redesigned homepage and repository overview, original WPF-rendered gallery, support reference, getting-started guide and this permanent changelog.
- **SVG filter graphs:** standalone raster rendering, named `in` / `in2` / `result` connections, `SourceGraphic` / `SourceAlpha`, filter regions, primitive subregions, coordinate units and sRGB / linearRGB processing.
- **Procedural effects:** four-channel seeded `feTurbulence` with turbulence/fractal noise, one/two base frequencies, octaves and stitching; `feDisplacementMap` with unpremultiplied channel selection and bilinear sampling.
- **More filter primitives:** `feFlood`, `feOffset`, `feMerge`, `feComposite`, `feColorMatrix` (matrix, saturation, hue rotation, luminance-to-alpha), `feComponentTransfer` (identity, table, discrete, linear, gamma), `feBlend`, `feGaussianBlur`, `feMorphology` and `feTile`.
- **Text on a path:** local `textPath` references, glyph placement and tangent rotation, percentage `startOffset`, text anchoring and styled spans.
- **SMIL motion:** `animateMotion` with inline paths or local `mpath` references, automatic/numeric rotation and `keyPoints` / `keyTimes`.
- **Nested SVG viewports:** independent percentage contexts, placement and sizing, viewBox mapping, meet/slice/none and overflow clipping. Symbol viewBoxes establish their content's percentage context too.
- **Pattern painting:** independent tile/content units, tile positions/dimensions, viewBoxes, transforms, opacity and forward `href` templates. Patterns are generated per target bounds.
- **CSS cascade:** specificity/source order, presentation/inline declarations, `!important`, CSS-wide keywords, inherited custom properties, nested `var()` fallbacks and cycle handling.
- **CSS selectors:** type, universal, ID, class, compound and attribute presence/equality selectors, `:root`, descendant/child and adjacent/general sibling relationships.
- **CSS animation controls:** multiple animations, shorthand/longhands, positive/negative delays, fractional repeats, directions, fill modes, initial paused state, grouped offsets and per-keyframe easing. Matching transform functions preserve full rotations.
- **CSS colors:** RGB/RGBA and HSL/HSLA functions, percentage channels, short/long RGBA hex colors, grey aliases and `rebeccapurple`.
- **Regression coverage:** an optional local resvg test-suite runner for 196 path/point/filter cases and eight turbulence references. Downloaded fixtures remain outside the repository.

#### Fixed

- Middle/end text anchoring now shifts text before its anchor using advances; `textPath` respects its own anchor style. The native documentation gallery exposed this existing alignment issue.
- Clip paths unite child silhouettes and honor paths, basic shapes, text, `use`, inherited `clip-rule`, transforms, bounding-box units and forward references. Empty clips safely hide their targets.
- Recursive `use` references stop instead of overflowing the stack. Rendering restores the referenced shape's original parent.
- Paths preserve completed segments before invalid data, accept adjacent single-token arc flags and reflect curve controls. Point lists preserve valid pairs, ignore unmatched final coordinates and safely handle missing data.
- Element opacity is applied after filtering; fill/stroke opacity remains part of `SourceGraphic`. User-space generators can paint empty groups; tiling preserves input subregions and their transparent pixels.
- Radial focal points use the center's coordinate space. Gradient/pattern opacity is independent per use. Gradient spreading and monotonically clamped stop offsets are honored.
- RGB alpha/channel clamping, quoted local paint/filter references and explicit inheritance of non-inherited CSS properties are handled correctly.
- `currentColor`, gradient-stop styling, transform origin, variables and animation declarations participate in improved CSS handling.

#### Verification and scope

- All nine library targets build without warnings/errors. WPF regressions, live clocks and 14 external animation examples pass.
- Selected checks use resvg-test-suite revision `d8e0643`: 200 reference-image comparisons and four render-only cases with nonportable/undefined expectations. Tolerances account for WPF antialiasing, tiled edges and fractional morphology radii.
- Blur uses separable box convolutions; morphology uses sliding extrema. Raster filters are capped at 2048 pixels on their longest dimension and sample sources at load time. Single matrix/saturation filters can retain vector paints to preserve native clocks.
- A plain-WPF rendering preflight reports an unavailable Windows graphics session before SVG pixel checks run.

Implementation: [CSS](https://github.com/dotnetprojects/SVGImage/commit/49bb532), [geometry, patterns, motion and procedural filters](https://github.com/dotnetprojects/SVGImage/commit/2dd3429), [filter graphs, paths and colors](https://github.com/dotnetprojects/SVGImage/commit/8d35028).

### 2026-10-28

#### Added

- **Native animation:** WPF clocks for SMIL transforms, timed visibility/display, opacity, circle/ellipse radii/centers and root viewBox cameras. Numeric starts, repeats, freeze, key times, splines and explicit click starts are supported.
- **CSS keyframes:** transform/opacity and stepped sprite-sheet playback, including the extracted CodePen horse example.
- **Host controls:** `UseAnimations`, deterministic `SeekAnimations` snapshots and `TriggerAnimationEvent`. Changing the source after initialization respects the animation setting.

#### Fixed

- Symbol/use viewBoxes, dimensions, alignment and aspect ratios now render correctly, fixing facial-part placement in MattHead and PeteyHead.
- Static color-matrix/saturation paint mappings support Flash2Svg exports while preserving native drawing animation.

Implementation: [native animation](https://github.com/dotnetprojects/SVGImage/commit/070e54d).

## [5.2.16](https://github.com/dotnetprojects/SVGImage/releases/tag/5.2.16) — 2026-10-28

### Added

- Luminance/alpha masks, mask/content units, region clipping, percentages, transforms, styled/forward references, nesting and safe empty/cyclic references. White/alpha masks keep vector drawing where possible; colored luminance masks use composited raster conversion.

### Fixed

- The SVG from [#118](https://github.com/dotnetprojects/SVGImage/issues/118) renders with its intended mask. Its fixture checks 93,767 flat interior pixels against Chromium.

Implementation: [SVG masks](https://github.com/dotnetprojects/SVGImage/commit/6494ad8).

## [5.2.15](https://github.com/dotnetprojects/SVGImage/releases/tag/5.2.15) — 2026-10-28

### Added

- `IsGrayscale` and `DrawingColorConverter.ToGrayscale` cover solid/gradient brushes, strokes, groups and bitmaps. Frozen drawings remain shareable. Addresses [#99](https://github.com/dotnetprojects/SVGImage/issues/99).

### Fixed

- RGB/percentage RGB gradient stops are parsed correctly. Addresses [#116](https://github.com/dotnetprojects/SVGImage/issues/116).
- SVG button content has an intrinsic desired size without explicit dimensions. Addresses [#106](https://github.com/dotnetprojects/SVGImage/issues/106).

Implementation: [RGB gradients](https://github.com/dotnetprojects/SVGImage/commit/0844bf9), [button sizing](https://github.com/dotnetprojects/SVGImage/commit/f31af63), [grayscale](https://github.com/dotnetprojects/SVGImage/commit/434d2fc).

## [5.2.14](https://github.com/dotnetprojects/SVGImage/releases/tag/5.2.14) — 2026-10-28

- Incorporated the upstream duplicate-font-name correction from [#121](https://github.com/dotnetprojects/SVGImage/pull/121), originally authored in March 2026.

## Earlier history

For earlier changes, see the [release tags](https://github.com/dotnetprojects/SVGImage/tags) and [commit history](https://github.com/dotnetprojects/SVGImage/commits/master/). Historical versions are not reconstructed as release notes here.

## Maintaining this changelog

Add user-visible changes under **Unreleased**, with a dated subsection and issue/commit links. Describe resulting behavior, compatibility changes and practical limits. On package publication, move entries to a versioned release heading with its date and start a fresh Unreleased section. This file is the single source; the documentation site includes it directly.
