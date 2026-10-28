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
