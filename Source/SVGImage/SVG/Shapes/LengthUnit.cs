namespace SVGImage.SVG.Shapes
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Child elements do not inherit the relative values as specified for their parent; they inherit the computed values.
    /// Values from <see cref="Unknown"/> to <see cref="pc"/> are 1 less than as defined in the SVG 1.1 SVG_LENGTHTYPE specification. This is so <see cref="Number"/> can be used as the default.
    /// </remarks>
    public enum LengthUnit
    {
        /// <summary>
        /// A unit was detected but not recgonized.
        /// </summary>
        /// 
        Unknown = -1,
        /// <summary>
        /// No unit specified, interpreted as a value in pixels.
        /// </summary>
        /// 
        Number,

        /// <summary>
        /// Percent is relative to least distant viewbox dimensions.
        /// If the length is inherently horizontal, like "dx", then the percentage is relative to the least distant viewbox width.
        /// If the length is inherently vertical, like "dy", then the percentage is relative to the least distant viewbox height.
        /// Otherwise the percentage is relative to the least distant viewbox diagonal. 
        /// </summary>
        /// <remarks>
        /// Setting a unit of <see cref="Percent"/> may be converted into <see cref="PercentWidth"/>, <see cref="PercentHeight"/>, or <see cref="PercentDiagonal"/>
        /// </remarks>
        Percent,

        /// <summary>
        /// Relative to font size of the element
        /// </summary>
        em,

        /// <summary>
        /// Relative to x-height of the element’s font
        /// </summary>
        ex,

        /// <summary>
        /// pixels	1px = 1/96th of 1in
        /// </summary>
        px,
        /// <summary>
        /// centimeters	1cm = 96px/2.54
        /// </summary>
        cm,

        /// <summary>
        /// millimeters	1mm = 1/10th of 1cm
        /// </summary>
        mm,

        /// <summary>
        /// inches	1in = 2.54cm = 96px
        /// </summary>
        Inches,

        /// <summary>
        /// points	1pt = 1/72nd of 1in
        /// </summary>
        pt,

        /// <summary>
        /// picas	1pc = 1/6th of 1in
        /// </summary>
        pc,

        /// <summary>
        /// quarter-millimeters	1Q = 1/40th of 1cm
        /// </summary>
        Q,

        /// <summary>
        /// Relative to character advance of the “0” (ZERO, U+0030) glyph in the element’s font
        /// </summary>
        ch,

        /// <summary>
        /// Relative to font size of the root element
        /// </summary>
        rem,

        /// <summary>
        /// Relative to 1% of viewport’s width
        /// </summary>
        vw,

        /// <summary>
        /// Relative to 1% of viewport’s height
        /// </summary>
        vh,

        /// <summary>
        /// Relative to 1% of viewport’s smaller dimension
        /// </summary>
        vmin,

        /// <summary>
        /// Relative to 1% of viewport’s larger dimension
        /// </summary>
        vmax,

        /// <summary>
        /// Percentage is relative to the least distant viewbox width.
        /// </summary>
        PercentWidth,

        /// <summary>
        /// Percentage is relative to the least distant viewbox height.
        /// </summary>
        PercentHeight,

        /// <summary>
        /// Percentage is relative to the least distant viewbox diagonal
        /// </summary>
        PercentDiagonal,
    }
}
