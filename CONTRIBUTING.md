# Contributing

Include a small SVG reproduction for rendering changes, explain the resulting behavior and run the relevant Windows [regression checks](Tests/RegressionTests/README.md).

## Keep the change history current

For user-visible features, fixes or compatibility changes, add a dated entry under **Unreleased** in [CHANGELOG.md](CHANGELOG.md). Link relevant issues/commits and describe practical limits. Assign a version when the package is actually published.

The site includes the root changelog directly. Update it once, rather than copying release notes into a second file. If support changes, also update [Docs/Support.md](Docs/Support.md).

## Documentation

Build the site using [Docs/Readme.md](Docs/Readme.md). Keep gallery examples original and their previews rendered by SVGImage. Do not commit downloaded third-party animation samples or resvg fixtures.

Before publishing, move the appropriate entries under a version/date heading, then start a fresh Unreleased section.

## Publish a NuGet release

1. Move the release's changelog entries under its version/date and commit the source.
2. Create and publish a GitHub release at that commit using a SemVer tag such as `6.0.0`, `v6.0.0` or `v6.0.0-preview.1`. Draft releases do not publish packages.
3. The release workflow runs regressions, builds/packs all nine targets, validates both packages, attaches the NuGet/symbol assets to that same release, then publishes to NuGet using the repository's `NUGET_API_KEY` secret. The tag supplies package/informational versions; numeric components supply assembly/file versions. NuGet normalizes build metadata out of filenames.

A push, PR or AppVeyor build only builds/tests. Rerunning a release job replaces its assets and skips already-published package versions. A published NuGet version cannot be replaced; corrections need a new release version. Test version parsing with `./Tests/ReleaseWorkflowTests.ps1`. Do not create a release just to test packaging: pack locally into a directory outside the repository and validate with `.github/scripts/Test-ReleasePackages.ps1`.
