Run the automated WPF regression checks on Windows:

```powershell
dotnet run --project Tests/RegressionTests -p:TargetFrameworks=net8.0-windows -p:GeneratePackageOnBuild=false
```

The executable runs on an STA thread and returns a nonzero exit code if a check fails.

Mask checks include the verbatim SVG from [issue #118](https://github.com/dotnetprojects/SVGImage/issues/118), saved as `Fixtures/Issue118.svg`. At 320 by 320 pixels, the test compares every flat interior pixel against `Fixtures/Issue118.chromium.png` (93,767 pixels). It excludes edge neighborhoods because WPF and Chromium use different antialiasing. Explicit checks also verify the transparent check-mark hole, folder fill and inner border. The WPF rendering is written to `bin/Debug/net8.0-windows/Issue118.actual.png` for inspection.

The browser reference was rendered with Chrome 156.0.8078.48, with a transparent background. To regenerate it, create an HTML file containing the unchanged fixture and this style:

```html
<style>html,body{margin:0;background:transparent}svg{width:320px;height:320px;display:block}</style>
```

Run Chrome headlessly against that HTML file with `--hide-scrollbars --default-background-color=00000000 --window-size=320,320 --screenshot=<absolute-output-path>`. Review the result before replacing the reference.

Additional pixel checks cover black-over-white cutouts, grayscale and linear-RGB luminance, alpha masks, gradients, embedded images, transparent colors, independent shared-paint opacity, mask child group opacity, coordinate units, percentage content, stroke-independent bounding boxes, transforms, forward/style references, nested/cyclic/empty masks, root SVG masks, `use` elements and paint overrides.

Animation checks use small synthetic SVG strings and verify SMIL interpolation, transform composition, timed frame visibility, repeats, click starts, CSS stepped sprite sheets, a real advancing WPF clock, symbol viewBox mapping, camera animation, color filters and disabling animation. Optional external sample validation is documented in [Animations](../../Docs/Animations.md); downloaded examples are not included in this repository.

The original `clipping_css.svg` sample is linked into the test output alongside its JPEG. Checks verify its visible image, changing aperture, stable static clip and indefinite repeat duration; a synthetic clip also advances on real WPF clocks. Transformed bounding-box clips can animate from a zero radius. An embedded `acid1.svg` links its companion CSS to verify pack-relative styles and `<use>` paint inheritance. Initial and changed paint overrides are checked on a real control.

Set `RESVG_TEST_SUITE` to an external checkout for 313 selected path/point/filter fixtures, plus eight turbulence reference comparisons. These include eleven portable convolution cases and all eight drop-shadow cases. Nine historical/undefined/nonportable cases are render-only; three saturated specular references overflow, an inherited-background reference retains a buffer instead of resetting it, and the masked-background reference is marked undefined. Analytic tests enforce clamping and explicit inheritance. Lighting comparisons use the reference resolution through `RasterizationScale`. Convolution's malformed/UB upstream references are excluded; synthetic checks verify the current specification's invalid-kernel pass-through and zero-divisor defaults. Downloaded corpus files remain outside the repository.

Release version validation runs separately with `./Tests/ReleaseWorkflowTests.ps1`. Packaging is verified across all nine framework assemblies and symbol sets with `.github/scripts/Test-ReleasePackages.ps1`.

Filter preview checks verify that bypassing filters preserves clipping and element/group opacity, the setting propagates into masks, a frozen preview remains unchanged, and restoring filters on the same parsed SVG matches a fresh full render.

Advanced animation checks cover compatible and to-only path morphs, incompatible-path warnings, morphed clip paths, advancing native path/mask clocks, syncbase chains/offsets/repeats/click history/self loops/cycle guards, alpha and composited luminance masks, exact seeking within a raster-mask frame bucket, matrix decomposition/common prefixes, and static/animated/nested percentage references. All five original Animation features sample SVGs must render distinct frames without animation warnings.


Lighting/image/backdrop checks exercise diffuse/specular alpha and saturation, all three light sources, CSS color, invalid parameters, raster/SVG data URIs, local elements, relative external SVG/raster chains, released file handles, missing/cyclic images, disabled/reset/clipped background buffers, sibling order, transformed targets and ancestor opacity. All three Filter features demos render offline, and the actual WPF control loads their embedded relative SVG resource.
