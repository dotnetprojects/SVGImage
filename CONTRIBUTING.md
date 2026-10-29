# Contributing

Include a small SVG reproduction for rendering changes, explain the resulting behavior and run the relevant Windows [regression checks](Tests/RegressionTests/README.md).

## Keep the change history current

For user-visible features, fixes or compatibility changes, add a dated entry under **Unreleased** in [CHANGELOG.md](CHANGELOG.md). Link relevant issues/commits and describe practical limits. Assign a version when the package is actually published.

The site includes the root changelog directly. Update it once, rather than copying release notes into a second file. If support changes, also update [Docs/Support.md](Docs/Support.md).

## Documentation

Build the site using [Docs/Readme.md](Docs/Readme.md). Keep gallery examples original and their previews rendered by SVGImage. Do not commit downloaded third-party animation samples or resvg fixtures.

Before publishing, move the appropriate entries under a version/date heading, then start a fresh Unreleased section.
