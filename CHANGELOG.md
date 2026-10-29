# Changelog

Changes to SVGImage are recorded here, newest first. Dates use the project's local calendar (Europe/Berlin).

## Unreleased

These entries describe the current repository source. They have not been assigned a NuGet release version. Install a published package for its released feature set, or build the current source to use these additions.

### 2026-10-29

#### Added

- **Lighting filters:** diffuse and specular shading from alpha height maps, distant/point/spot lights, Sobel edge/corner normals, lighting color/CSS, surface scale, kernel spacing, spot cones and specular exponents. Diffuse alpha is opaque; specular alpha follows the largest output channel, with clamping before byte conversion.
- **Filter image inputs:** `feImage` loads local SVG elements, raster/SVG data URIs and host-resolved external resources; aspect-ratio placement, primitive regions, nested relative SVG resources and cycle guards are supported. Raster image decoding now releases its streams and accepts SVG2 `href` and namespace-qualified XLink references.
- **Backdrops:** `BackgroundImage` / `BackgroundAlpha` capture preceding siblings inside `enable-background:new`, with region clipping, nested resets and transformed coordinate spaces. Ancestor group opacity is applied once after composition. Normal documents avoid background capture work.
- **Filter examples:** an offline Filter features tab demonstrates lighting, three image-input forms and backdrop glass, with inspectable source. `SVGRender.RasterizationScale` controls filter sampling (default four pixels per user unit, retaining the 2048-pixel cap).
- **Filter verification:** 98 additional resvg lighting/image/background fixtures, plus analytic pixel tests for saturation, alpha, relative resources, recursion, buffer resets and sibling order. Of 321 selected corpus checks, 312 compare reference pixels and nine are render-only; five newly included historical/UB references are covered by explicit current-rule tests where applicable.
- **Path morphing:** native SMIL `d` interpolation for compatible line/quadratic/cubic/arc path structures, to-only underlying paths, clipping/masking integration and discrete switching for incompatible topology.
- **Syncbase timing:** forward animation-ID begin/end references, signed offsets, acyclic timing chains, repeated active durations, click-start propagation/history and seeded self-repeating sequences. Unseeded cycles stay inactive; the full SMIL end/restart and multi-animation cycle model remains incomplete.
- **Animated masks:** retained vector clocks for alpha/white masks and live composited luminance masks with correct black-over-white occlusion; bounded 30 Hz/512-pixel raster updates and immediate exact seeking.
- **CSS transforms:** percentage translations using viewport/fill-box bounds; general 2D matrix interpolation through signed scales, rotation and residual skew, preserving compatible prefixes and full rotations. Singular endpoints switch discretely; explicit transform edits made through the rendering API are preserved.
- **Original animation examples:** a dedicated Animation features tab with five offline SVG demos, source inspection, restart and exact-frame controls.

- **Progressive SVG preview:** `SVGRender.UseFilters` can bypass SVG filters while keeping clipping, masks and opacity. The car sample publishes a frozen preview immediately, then rerenders the same parsed SVG with filters on its background STA and replaces the preview with the complete result.

- **Documentation:** redesigned homepage and repository overview, original WPF-rendered gallery, support reference, getting-started guide and this permanent changelog.
- **SVG filter graphs:** standalone raster rendering, named `in` / `in2` / `result` connections, `SourceGraphic` / `SourceAlpha`, filter regions, primitive subregions, coordinate units and sRGB / linearRGB processing.
- **Procedural effects:** four-channel seeded `feTurbulence` with turbulence/fractal noise, one/two base frequencies, octaves and stitching; `feDisplacementMap` with unpremultiplied channel selection and bilinear sampling.
- **Convolution and shadows:** `feConvolveMatrix` kernels, order/targets, divisor/bias, edge modes, kernel spacing and preserved alpha; `feDropShadow` blur/offset/color/opacity and source-over composition. Added eleven portable convolution and all eight drop-shadow reference comparisons.
- **Animation gallery:** a dedicated, lazily created Flash2Svg sample tab lists all 13 wiki examples, credits the original page/project license, caches downloads outside the repository and provides Play/restart for SMIL click triggers. JavaScript/audio remain unsupported.
- **Release automation:** publishing a GitHub release packs all nine frameworks with the release tag's semantic version, validates manifests/assembly versions/symbols, attaches `.nupkg`/`.snupkg` assets and publishes them to NuGet. Push/PR/AppVeyor builds no longer publish packages; GitVersion was removed.
- **More filter primitives:** `feFlood`, `feOffset`, `feMerge`, `feComposite`, `feColorMatrix` (matrix, saturation, hue rotation, luminance-to-alpha), `feComponentTransfer` (identity, table, discrete, linear, gamma), `feBlend`, `feGaussianBlur`, `feMorphology` and `feTile`.
- **Text on a path:** local `textPath` references, glyph placement and tangent rotation, percentage `startOffset`, text anchoring and styled spans.
- **SMIL motion:** `animateMotion` with inline paths or local `mpath` references, automatic/numeric rotation and `keyPoints` / `keyTimes`.
- **Nested SVG viewports:** independent percentage contexts, placement and sizing, viewBox mapping, meet/slice/none and overflow clipping. Symbol viewBoxes establish their content's percentage context too.
- **Pattern painting:** independent tile/content units, tile positions/dimensions, viewBoxes, transforms, opacity and forward `href` templates. Patterns are generated per target bounds.
- **CSS cascade:** specificity/source order, presentation/inline declarations, `!important`, CSS-wide keywords, inherited custom properties, nested `var()` fallbacks and cycle handling.
- **CSS selectors:** type, universal, ID, class, compound and attribute presence/equality selectors, `:root`, descendant/child and adjacent/general sibling relationships.
- **CSS animation controls:** multiple animations, shorthand/longhands, positive/negative delays, fractional repeats, directions, fill modes, initial paused state, grouped offsets and per-keyframe easing. Matching transform functions preserve full rotations.
- **CSS colors:** RGB/RGBA and HSL/HSLA functions, percentage channels, short/long RGBA hex colors, grey aliases and `rebeccapurple`.
- **Regression coverage:** an optional local resvg test-suite runner for 313 path/point/filter cases and eight turbulence references. Downloaded fixtures remain outside the repository.

#### Fixed

- Background-buffer discovery and root filter composition safely handle an uninitialized SVG document; empty drawings remain valid with filters enabled or disabled.

- The Acid1 sample's right pupil now uses modern `rgb(-5% 128 0%)` and renders green. RGB/RGBA parsing distinguishes space/slash syntax (mixed numeric/percentage channels allowed) from legacy commas (uniform channel units), accepts multiline CSS whitespace and rejects mixed separators or alpha without a slash. Pixel regressions cover the actual embedded sample and mixed channels/alpha.

- Animated clip geometry retains its native clocks through unions/transforms and bounding-box mapping. The original `clipping_css.svg` now moves/repeats correctly; `repeatDur` supports indefinite and finite durations, including frozen partial iterations.
- Initial control loading publishes its brush palette without rendering the SVG twice. File sources retain their filename for relative resource resolution; embedded SVGs load companion CSS/images beside their resource URI. Initial and later paint overrides remain functional.
- Blur passes process RGBA scanlines together, avoiding repeated edge-mode decoding and strided per-channel reads. The sample opens immediately while its detailed static illustration renders on a background STA; only a frozen drawing crosses back to the UI. Sample asset paths resolve relative to the executable regardless of the working directory.
- Explicit inherited fill/stroke follow `<use>` instance ancestry, fixing the black outer eye in `acid1.svg`. SVG switch feature lists recognize whitespace-separated required features and reject unsupported extensions independently.

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
- Selected checks use resvg-test-suite revision `d8e0643`: 312 reference-image comparisons and nine render-only cases with historical/nonportable/undefined expectations. Tolerances account for WPF antialiasing, tiled edges and fractional morphology radii.
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
