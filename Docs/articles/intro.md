## Introduction

SVG (Scalable Vector Graphic) is an XML based graphic format.

This little project started out as a simple `SVG` to `XAML` converter. I looked at some simple SVG files and noticed 
the similarity to `XAML` defined graphic, and decided to write a converter. However, I soon realized that SVG is a 
very complex format, and at the same time, I found no good reason for converting to `XAML` just to show the image 
on the screen, so instead, I started working on the `SVG`, `SVGRender` and the `SVGImage` classes.

* The `SVG` class is the class that reads and parses the XML file.
* `SVGRender` is the class that creates the WPF Drawing object based on the information from the SVG class.
* `SVGImage` is the image control. The image control can either  
    - load the image from a filename `SetImage(filename)` 
    - or by setting the Drawing object through `SetImage(Drawing)`, 
   which allows multiple controls to share the same drawing instance.
   
The control only has a couple of properties, `SizeType` and `ImageSource`.

* `SizeType` - controls how the image is stretched to fill the control
    - `None`: The image is not scaled. The image location is translated so the top left corner of the image bounding box is moved to the top left corner of the image control.
    - `ContentToSizeNoStretch`: The image is scaled to fit the control without any stretching. Either X or Y direction will be scaled to fill the entire width or height.
    - `ContentToSizeStretch`: The image will be stretched to fill the entire width and height.
    - `SizeToContent`: The control will be resized to fit the un-scaled image. If the image is larger than the max size for the control, the control is set to max size and the image is scaled.
    - `ViewBoxToSizeNoStretch`: Not the content of the image but its viewbox is scaled to fit the control without any stretching.Either `X` or `Y` direction will be scaled to fill the entire width or height.

For `None` and `ContentToSizeNoStretch`, the Horizontal/VerticalContentAlignment properties can be used to position the image within the control.

* `ImageSource` - This property is the same as `SetImage(drawing)`, and is exposed to allow for the source to be set through binding.

The colors of an `SVGImage` can be overridden at run-time through some additional properties of the `SVGImage`:

* `OverrideFillColor` - This property overrides all fill colors of an SVG image
* `OverrideStrokeColor` - Overrides all stroke colors of an SVG image
* `OverrideColor` - Overrides all colors of an SVG image. `OverrideFillColor` and `OverrideStrokeColor` take precedence over the `OverrideColor` property.
