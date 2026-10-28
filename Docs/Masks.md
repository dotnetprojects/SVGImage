# SVG masks

SVGImage supports local mask references on shapes, groups, `use` elements and the root SVG:

```xml
<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100">
  <defs>
    <mask id="cutout">
      <rect width="100" height="100" fill="white" />
      <circle cx="50" cy="50" r="20" fill="black" />
    </mask>
  </defs>
  <rect width="100" height="100" fill="red" mask="url(#cutout)" />
</svg>
```

White reveals the artwork, black hides it, and gray or translucent content partially reveals it. Mask content is composited before luminance is multiplied by alpha, so a black shape painted over white cuts a hole. `color-interpolation="linearRGB"` selects linear RGB luminance; the default is sRGB. `mask-type="alpha"` uses alpha alone.

Both `maskUnits` and `maskContentUnits` support `userSpaceOnUse` and `objectBoundingBox`. Defaults follow SVG: the region uses the object's geometry bounding box, excluding stroke, with `x="-10%"`, `y="-10%"`, `width="120%"` and `height="120%"`; content uses the target's user coordinates. Bounding-box content accepts normalized numbers and percentages. Region clipping, child transforms, inherited fills, forward references and nested masks are supported. Definitions do not paint directly. Empty masks, zero/negative regions and cyclic mask references produce no visible content.

White-only luminance masks and alpha masks remain vector drawings. Colored luminance masks are composited into an offscreen bitmap, with the longest side between 1024 and 2048 pixels, then used as a WPF opacity mask. At very large zoom levels these colored masks can show raster sampling. Mask contents are evaluated when the drawing is created; animated masks and external-document mask references are not implemented. Masking uses the graphics features already supported by SVGImage, so unsupported SVG filters and other unsupported child elements retain their existing limitations.

`OverrideColor`, `OverrideFillColor`, `OverrideStrokeColor` and `OverrideStrokeWidth` affect the visible artwork without changing mask colors or geometry.

Reference: [SVG 1.1 masking](https://www.w3.org/TR/SVG11/masking.html#Masking).
