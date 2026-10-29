# SVGImage documentation

The [homepage](index.md) introduces the project with previews rendered by SVGImage itself.

- [Get started](GettingStarted.md)
- [SVG support reference](Support.md)
- [Changelog](Changelog.md) — includes the repository's single canonical `CHANGELOG.md`
- [Masks](Masks.md), [CSS](CSS.md), [animations](Animations.md), [rendering](Rendering.md), [grayscale](Grayscale.md)

Build and preview on Windows:

```powershell
dotnet tool install -g docfx --version 2.78.5
docfx Docs/docfx.json --serve
```

The generated site is written to `Docs/_site`. The existing GitHub Pages workflow deploys it on pushes to `master`.

Gallery SVGs in `images/demos` are original documentation examples. Their PNG/GIF previews are rendered with the WPF library, rather than browser screenshots. Downloaded animation and resvg fixtures are not included.

Regenerate the gallery from the repository root on Windows:

```powershell
dotnet run --project Docs/tools/GalleryRenderer -p:TargetFrameworks=net8.0-windows -p:GeneratePackageOnBuild=false
```

For future updates, add dated entries under Unreleased in the root `CHANGELOG.md`. The website includes that file directly; do not maintain a second copy.
