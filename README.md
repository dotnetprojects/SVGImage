# SVGImage
This is an SVG Image View Control for WPF applications.

Initially forked from: [SVGImage Control - CodeProject Article](https://www.codeproject.com/Articles/92434/SVGImage-Control)

Besides the bug fixes, new features are added including the following:
  - Mask/Clip support
  - Support of styles.
  - Simple Animation support.

## Downloads
The SVGImage is available as a single DLL in [NuGet Package](https://www.nuget.org/packages/DotNetProjects.SVGImage).
The respository includes
* **Tests:** For test tools, and
* **Samples:** For sample applications
* **Docs:** For the documentations (in markdown format).

The command lines installation options are: For the version `5.0.118`
* **.NET CLI:** dotnet add package DotNetProjects.SVGImage --version 5.0.118
* **Package Manager:** NuGet\Install-Package DotNetProjects.SVGImage -Version 5.0.118

For reference in projects use: For the version `5.0.118`
```xml
<PackageReference Include="DotNetProjects.SVGImage" Version="5.0.118" />
```

## How to build
The SVGImage build targets .NET6.0-Windows (and up), .Net Framework 4.x TFMs and .NET Core 3.1. 
* Use `Source/SVGImage.sln` in requires Visual Studio 2022 for complete build, or 
* Use `Source/SVGImage.VS2019.sln` in Visual Studio 2019 for .Net Frameworks and .NET Core 3.1.

## Documentations
For the User Manual and API References see [Documentation](http://dotnetprojects.github.io/SVGImage/).

## Framework support
The SVGImage control library targets the following frameworks
* .NET Framework, Version 4.0
* .NET Framework, Version 4.5
* .NET Framework, Version 4.6
* .NET Framework, Version 4.7
* .NET Framework, Version 4.8
* .NET Core, Version 3.1
* .NET 6 ~ 8

## License
The SVGImage control library is relicensed under [MIT License](https://github.com/dotnetprojects/SVGImage/blob/master/LICENSE),
with permission from the original author.

## Sample Application

![](Docs/images/sample.png)


