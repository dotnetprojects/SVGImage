# SVG geometry and raster filters

Clipping accepts paths, basic shapes, text and local `use` references. Child silhouettes are united, `clip-rule` is inherited, transforms are applied, and `clipPathUnits="objectBoundingBox"` maps normalized content to the target's geometric bounds. Empty clips hide their targets. Definitions and circular references do not paint.

Nested `svg` elements establish independent percentage-length contexts, viewport position/dimensions, viewBoxes, meet/slice/none aspect ratios and overflow clipping. Symbol viewBoxes also establish the percentage context for their content. Local `use` references preserve their target's parent after rendering and circular references are ignored.

`textPath` lays out glyph outlines along a referenced path, rotates them to the path tangent, and supports `startOffset` (including percentages), text anchoring and nested styled spans. Glyphs whose midpoints are outside the path are omitted. This does not implement text stretching/automatic spacing, vertical text-on-path or pathLength calibration.

Patterns support independent `patternUnits` and `patternContentUnits`, tile origin/dimensions, viewBox/aspect ratio, transforms, opacity and local href templates (including forward references). Brushes are constructed per target bounds, so sharing a pattern between differently sized objects is safe. Circular pattern painting returns transparent content. Pattern overflow is clipped to the tile.

`feTurbulence` and `feDisplacementMap` use a bounded CPU raster pipeline. Turbulence provides four seeded channels, turbulence/fractalNoise, one or two base frequencies, up to nine octaves and tile stitching, using the SVG 1.1 noise generator. Displacement uses unpremultiplied R/G/B/A selectors, signed scale and bilinear sampling with transparent pixels outside the input.

These chains support SourceGraphic/SourceAlpha, named results, primitive subregions, filter/primitive coordinate units, sRGB/linearRGB conversion, and companion flood, offset, merge, composite and matrix/saturation/luminance-to-alpha operations. Other primitive types are currently passed through; this is not a complete SVG filter graph implementation. Standalone vector color matrices retain the existing vector path.

Raster filters sample the source at load time; animation of the filtered source or filter parameters does not update their bitmap. Rasterization uses up to four pixels per user unit and a maximum dimension of 2048 pixels. This bounds memory use but limits magnification quality. The geometry and animation features remain vector-based outside these raster filter chains.

Run the regression runner with `RESVG_TEST_SUITE` pointing to a local checkout of linebender/resvg-test-suite to additionally compare the supported turbulence cases against its reference PNGs. Reference SVGs/PNGs and downloaded animation examples are kept outside this repository.
