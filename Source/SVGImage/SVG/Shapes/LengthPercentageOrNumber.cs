using System;

namespace SVGImage.SVG.Shapes
{
    using System.Text.RegularExpressions;

    public struct LengthPercentageOrNumber
    {
        private static readonly Regex _lengthRegex = new Regex(@"(?<Value>\d+(?:\.\d+)?)\s*(?<Unit>%|\w+)?", RegexOptions.Compiled | RegexOptions.Singleline);
        private readonly LengthContext _context;
        private readonly double _value;
        public double Value => ResolveValue();

        
        private static double ResolveAbsoluteValue(double value, LengthContext context)
        {
            switch (context.Unit)
            {
                case LengthUnit.cm:
                    return value * 35.43;
                case LengthUnit.mm:
                    return value * 3.54;
                case LengthUnit.Q:
                    return value * 3.54 / 4d;
                case LengthUnit.Inches:
                    return value * 90d;
                case LengthUnit.pc:
                    return value * 15d;
                case LengthUnit.pt:
                    return value * 1.25;
                case LengthUnit.px:
                    return value * 90d / 96d;
                case LengthUnit.Unknown:
                case LengthUnit.Number:
                default:
                    return value;
            }
        }
        private static double ResolveViewboxValue(double value, LengthContext context)
        {
            double height;
            double width;
            if (context.Owner.Svg.ViewBox.HasValue)
            {
                height = context.Owner.Svg.ViewBox.Value.Height;
                width = context.Owner.Svg.ViewBox.Value.Width;
            }
            else
            {
                height = context.Owner.Svg.Size.Height;
                width = context.Owner.Svg.Size.Width;
            }
            switch (context.Unit)
            {
                case LengthUnit.Percent:
                    throw new NotSupportedException($"Percent without specific orientation is not supported. Use ${LengthUnit.PercentWidth}, ${LengthUnit.PercentHeight}, or ${LengthUnit.PercentDiagonal} instead.");
                case LengthUnit.PercentDiagonal:
                    return (value / 100d) * Math.Sqrt(Math.Pow(width, 2d) + Math.Pow(height, 2d));
                case LengthUnit.vw:
                case LengthUnit.PercentWidth:
                    return (value / 100d) * width;
                case LengthUnit.vh:
                case LengthUnit.PercentHeight:
                    return (value / 100d) * height;
                case LengthUnit.vmin:
                    return (value / 100d) * Math.Min(width, height);
                case LengthUnit.vmax:
                    return (value / 100d) * Math.Max(width, height);
                case LengthUnit.Unknown:
                case LengthUnit.Number:
                default:
                    return value;
            }
        }
        private static double ResolveRelativeValue(double value, LengthContext context)
        {
            switch (context.Unit)
            {
                case LengthUnit.em:
                    return value * context.Owner.TextStyle.FontSize;
                case LengthUnit.ex:
                    return value * context.Owner.TextStyle.GetTypeface().XHeight;
                case LengthUnit.ch:
                    var glyphTypeface = context.Owner.TextStyle.GetGlyphTypeface();
                    return value * glyphTypeface.AdvanceWidths[glyphTypeface.CharacterToGlyphMap['0']];
                case LengthUnit.rem:
                    return value * context.Owner.GetRoot().TextStyle.FontSize;
                case LengthUnit.Unknown:
                case LengthUnit.Number:
                default:
                    return value;
            }
        }
        private double ResolveValue()
        {
            if (_context == null)
            {
                return _value; // No context, return raw value
            
            }

            switch (_context.Unit)
            {
                case LengthUnit.Percent:
                case LengthUnit.PercentWidth:
                case LengthUnit.PercentHeight:
                case LengthUnit.PercentDiagonal:
                case LengthUnit.vw:
                case LengthUnit.vh:
                case LengthUnit.vmin:
                case LengthUnit.vmax:
                    return ResolveViewboxValue(_value, _context);
                case LengthUnit.em:
                case LengthUnit.ex:
                case LengthUnit.ch:
                case LengthUnit.rem:
                    return ResolveRelativeValue(_value, _context);
                case LengthUnit.cm:
                case LengthUnit.mm:
                case LengthUnit.Q:
                case LengthUnit.Inches:
                case LengthUnit.pc:
                case LengthUnit.pt:
                case LengthUnit.px:
                    return ResolveAbsoluteValue(_value, _context);
                case LengthUnit.Unknown:
                case LengthUnit.Number:
                default:
                    return _value;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context">If null, units will be ignored</param>
        public LengthPercentageOrNumber(double value, LengthContext context)
        {
            _context = context;
            _value = value;
        }
        public static LengthPercentageOrNumber Parse(Shape owner, string value, LengthOrientation orientation = LengthOrientation.None)
        {
            var lengthMatch = _lengthRegex.Match(value.Trim());
            if(!lengthMatch.Success || !Double.TryParse(lengthMatch.Groups["Value"].Value, out double d))
            {
                throw new ArgumentException($"Invalid length/percentage/number value: {value}");
            }
            LengthContext context;
            if (lengthMatch.Groups["Unit"].Success)
            {
                string unitStr = lengthMatch.Groups["Unit"].Value;
                LengthUnit unit = LengthContext.Parse(unitStr, orientation);
                if (unit == LengthUnit.Unknown)
                {
                    throw new ArgumentException($"Unknown length unit: {unitStr}");
                }
                context = new LengthContext(owner, unit);
            }
            else
            {
                // Default to pixels if no unit is specified
                context = new LengthContext(owner, LengthUnit.px);
            }
            return new LengthPercentageOrNumber(d, context);
        }
        
    }




}
