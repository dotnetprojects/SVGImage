---
title: Rendering performance
description: Measured rendering improvements, reproducible benchmarks and filter quality tradeoffs.
---
# Rendering performance

The current source reduces temporary allocations in the raster pipeline and redundant paint-inheritance traversal. These changes preserve the sampling quality and filter algorithms.

## Work removed

- Fill and stroke inheritance consult the parent once instead of walking ancestors whose getters already traverse that same ancestry. Deep groups with no paint declarations no longer cause repeated exponential searches. Values still resolve against the current `use` parent; no global paint cache is introduced.
- Color matrices use scalar arithmetic and blends use a small RGB value type, avoiding temporary arrays and LINQ work per pixel.
- Gaussian blur alternates two private scratch buffers across its box passes. Shared source/named-result frames are preserved; scratch buffers are local to that blur operation.
- Alpha-only inputs are materialized only when requested. Blur, morphology and image primitives supply their own output frames. Single-input primitives skip unused second-input color conversions; generators skip unused source color conversions.
- Flood colors are converted once per primitive. Full-region primitives skip redundant per-pixel clipping tests.

## Measured comparison

Baseline: commit `5b28407`. Measured on 2026-11-03. All twelve pixel hashes matched exactly.

| Workload | Before → after (ms) | Managed allocation before → after (MB) |
| --- | ---: | ---: |
| `nested groups 16` | 10.76 → 0.59 | 0.10 → 0.10 |
| `nested groups 20` | 63.19 → 1.21 | 0.11 → 0.11 |
| `color matrix` | 105.18 → 66.94 | 169.04 → 38.48 |
| `multiply blend` | 95.73 → 90.67 | 161.36 → 53.84 |
| `hue blend` | 205.36 → 157.53 | 530.00 → 53.84 |
| `linear filter chain` | 615.64 → 423.54 | 338.10 → 115.38 |
| `tiger.svg load` | 30.75 → 24.46 | 6.85 → 6.85 |
| `tiger.svg redraw` | 27.71 → 24.85 | 5.24 → 5.24 |
| `acid1.svg load` | 9.64 → 10.75 | 2.32 → 2.33 |
| `acid1.svg redraw` | 4.22 → 3.60 | 0.88 → 0.88 |
| `171.svg load` | 5824.77 → 4691.42 | 3111.01 → 1384.67 |
| `171.svg redraw` | 5646.23 → 4656.40 | 3096.64 → 1370.28 |

[Raw results, including pixel hashes](https://github.com/dotnetprojects/SVGImage/blob/master/Tests/PerformanceBenchmarks/results/2026-11-03-net8.json). Small vector cases vary between runs; these measurements do not imply a uniform speedup for every SVG.

The benchmark uses `SVGRender` with its default animation-disabled setting, so the standalone color-matrix case exercises raster rendering rather than the animation-preserving vector shortcut.

The .NET 8 Release benchmark compares the committed baseline with the optimized source on the same Windows host. Each case has one untimed warmup and five timed iterations; values are medians. Managed allocations are cumulative bytes allocated by the rendering thread, not peak resident or native/GPU memory. Final image rasterization/hash computation and explicit GC are outside the timed section. Results are specific to these workloads and this machine.

## Reproduce

```powershell
dotnet run --project Tests/PerformanceBenchmarks --configuration Release -p:TargetFrameworks=net8.0-windows -p:GeneratePackageOnBuild=false -- 5
```

The benchmark emits JSON Lines for twelve cases, including load and redraw paths for tiger, Acid1 and the car. Run baseline and candidate serially, without concurrent builds/tests. The pixel hash verifies that a speedup did not simply remove effects or reduce quality. [Benchmark methodology](https://github.com/dotnetprojects/SVGImage/tree/master/Tests/PerformanceBenchmarks).

## Host-side controls

Reuse a finished drawing when displaying the same static artwork. Recreating a drawing rebuilds its filters, even when the parsed SVG is reused. Static frozen drawings can be published from a background STA, as demonstrated by the example car.

For large filtered artwork, `UseFilters=false` provides a quick preview; turn filters on and recreate the drawing for the complete image. The example car performs these passes on its background STA.

`SVGRender.RasterizationScale` controls filter pixels per user unit, with a 2048-pixel cap on the longest dimension. Lower sampling scales reduce work when the artwork is displayed small, but can change edge precision and lighting. The optimizations above keep the default scale and compare identical pixels. Set the control's `UseAnimations=false` for artwork that does not need clocks.

Raster filters still take load-time snapshots. The new scratch buffers are not a persistent/shared bitmap cache; changing the SVG, paint overrides or animation state continues to require the appropriate redraw.
