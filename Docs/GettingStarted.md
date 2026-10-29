---
title: Get started
description: Install SVGImage, add a WPF control and work with native Drawing objects.
---
# Get started

SVGImage is a **Windows WPF** control and renderer. The source targets .NET Framework 4.0–4.8, .NET Core 3.1 and .NET 6–8 for Windows.

## Install a published package

```powershell
dotnet add package DotNetProjects.SVGImage
```

See [NuGet](https://www.nuget.org/packages/DotNetProjects.SVGImage) for versions. The [support reference](Support.md) describes the current source; [unreleased changes](Changelog.md) may not yet be packaged.

## Put an SVG in your window

Add the SVG to your application, set its build action to **Content** and copy it to the output directory. Add the namespace and control:

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:svg="https://github.com/dotnetprojects/SVGImage">
    <svg:SVGImage UriSource="Assets/illustration.svg"
                  Width="320" Height="240"
                  UseAnimations="True"
                  SizeType="ViewBoxToSizeNoStretch" />
</Window>
```

`UseAnimations` defaults to true. Set it to false for static rendering. Color overrides include `OverrideColor`, `OverrideFillColor` and `OverrideStrokeColor`. Set `IsGrayscale="True"` for grayscale display.

## Work with a Drawing

For direct drawing access, opt in to animation before loading:

```csharp
var renderer = new SVGImage.SVG.SVGRender { UseAnimations = true };
System.Windows.Media.DrawingGroup drawing = renderer.LoadDrawing("Assets/illustration.svg");

// Stop the existing drawing at an exact time for a snapshot.
renderer.SeekAnimations(TimeSpan.FromSeconds(1.5));
```

Reload/recreate the drawing to restart playback after seeking. See [animation controls](Animations.md) and the [API reference](api/index.md).

## Build the current source

Use `Source/SVGImage.sln` with Visual Studio 2022, or build on Windows:

```powershell
dotnet build Source/SVGImage/DotNetProjects.SVGImage.csproj -p:GeneratePackageOnBuild=false
```

To use unreleased changes, reference the library project or a matching locally built DLL instead of an older package.

Explore the [samples](https://github.com/dotnetprojects/SVGImage/tree/master/Samples), [support reference](Support.md) and [changelog](Changelog.md).
