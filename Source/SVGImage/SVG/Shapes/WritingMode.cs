namespace SVGImage.SVG.Shapes
{
    /// <summary>
    /// The ‘writing-mode’ property specifies whether the initial inline-progression-direction for a ‘text’ element shall be left-to-right, right-to-left, or top-to-bottom.
    /// The ‘writing-mode’ property applies only to ‘text’ elements; the property is ignored for ‘tspan’, ‘tref’, ‘altGlyph’ and ‘textPath’ sub-elements.
    /// (Note that the inline-progression-direction can change within a ‘text’ element due to the Unicode bidirectional algorithm and properties ‘direction’ and ‘unicode-bidi’.
    /// For more on bidirectional text, see Relationship with bidirectionality.)
    /// </summary>
    public enum WritingMode
    {
        /// <summary>
        /// Inherits the writing mode from the parent element.
        /// </summary>
        None = 0,
        /// <summary>
        /// This value defines a top-to-bottom block flow direction. Both the writing mode and the typographic mode are horizontal.
        /// </summary>
        /// <remarks>
        /// Set for atrributes with values 'horizontal-tb', 'lr', 'lr-tb', 'rl', and 'rl-tb'.
        /// </remarks>
        HorizontalTopToBottom,
        /// <summary>
        /// This value defines a right-to-left block flow direction. Both the writing mode and the typographic mode are vertical.
        /// </summary>
        /// <remarks>
        /// Set for atrributes with values 'vertical-rl', 'tb-rl', 'tb'.
        /// </remarks>
        VerticalRightToLeft,
        /// <summary>
        /// This value defines a left-to-right block flow direction. Both the writing mode and the typographic mode are vertical.
        /// </summary>
        /// <remarks>
        /// Set for atrributes with value 'vertical-lr'.
        /// </remarks>
        VerticalLeftToRight,
    }




}
