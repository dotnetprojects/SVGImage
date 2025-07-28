namespace SVGImage.SVG.Shapes
{
    public enum LengthAdjustment
    {
        None,
        /// <summary>
        /// Indicates that only the advance values are adjusted. The glyphs themselves are not stretched or compressed.
        /// </summary>
        Spacing,
        /// <summary>
        /// Indicates that the advance values are adjusted and the glyphs themselves stretched or compressed in one axis (i.e., a direction parallel to the inline-base direction).
        /// </summary>
        SpacingAndGlyphs
    }




}
