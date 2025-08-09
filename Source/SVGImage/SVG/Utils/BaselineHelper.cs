using SVGImage.SVG.Shapes;
using System;
using System.Globalization;

namespace SVGImage.SVG.Utils
{
    internal static class BaselineHelper
    {
        public static LengthPercentageOrNumber EstimateBaselineShift(Shape shape)
        {
            return EstimateBaselineShift(shape.TextStyle, shape);
        }
        /// <summary>
        /// The purpose of this method is to allow TextStrings which are not shapes themselves to use the same logic as TextShapes to estimate the baseline shift.
        /// They can use this method to estimate the baseline shift based on the TextStyle of the TextString's parent Shape.
        /// </summary>
        /// <param name="textStyle"></param>
        /// <param name="shape"></param>
        /// <returns></returns>
        public static LengthPercentageOrNumber EstimateBaselineShift(TextStyle textStyle, Shape shape)
        {
            if (String.IsNullOrEmpty( textStyle.BaseLineShift) || textStyle.BaseLineShift == "baseline")
            {
                return new LengthPercentageOrNumber(0d, new LengthContext(shape, LengthUnit.Number));
            }
            else if (textStyle.BaseLineShift == "sub")
            {
                //Based on previous estimation
                return new LengthPercentageOrNumber( textStyle.FontSize * 0.5, new LengthContext(shape, LengthUnit.Number));
            }
            else if (textStyle.BaseLineShift == "super")
            {
                //Based on previous estimation
                return new LengthPercentageOrNumber((-1) * (textStyle.FontSize + (textStyle.FontSize * 0.25)), new LengthContext(shape, LengthUnit.Number));
            }
            else if(textStyle.BaseLineShift.EndsWith("%") && Double.TryParse(textStyle.BaseLineShift.Substring(0, textStyle.BaseLineShift.Length - 1), NumberStyles.Number, CultureInfo.InvariantCulture, out double d))
            {
                try
                {
                    //The computed value of the property is this percentage multiplied by the computed "line-height" of the ‘text’ element.
                    //for the purposes of processing the ‘font’ property in SVG, 'line-height' is assumed to be equal the value for property ‘font-size’
                    return new LengthPercentageOrNumber(d, new LengthContext(shape, LengthUnit.rem));
                }
                catch
                {
                    //Continue
                }
            }
            try
            {
                return LengthPercentageOrNumber.Parse(shape, textStyle.BaseLineShift, LengthOrientation.Vertical);
            }
            catch
            {
                return new LengthPercentageOrNumber(0d, new LengthContext(shape, LengthUnit.Number));
            }
        }
        public static double EstimateBaselineShiftValue(Shape shape)
        {
            return EstimateBaselineShift(shape).Value;
        }
        public static double EstimateBaselineShiftValue(TextStyle textStyle, Shape shape)
        {
            return EstimateBaselineShift(textStyle, shape).Value;
        }
    }
}
