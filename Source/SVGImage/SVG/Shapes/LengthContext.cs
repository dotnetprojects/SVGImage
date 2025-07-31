using System;
using System.Collections.Generic;

namespace SVGImage.SVG.Shapes
{
    public class LengthContext
    {
        public Shape Owner { get; set; }
        public LengthUnit Unit { get; set; }
        private static readonly Dictionary<string, LengthUnit> _unitMap = new Dictionary<string, LengthUnit>(StringComparer.OrdinalIgnoreCase)
        {
            {"em", LengthUnit.em},
            {"ex", LengthUnit.ex},
            {"ch", LengthUnit.ch},
            {"rem", LengthUnit.rem},
            {"vw", LengthUnit.vw},
            {"vh", LengthUnit.vh},
            {"vmin", LengthUnit.vmin},
            {"vmax", LengthUnit.vmax},
            {"cm", LengthUnit.cm},
            {"mm", LengthUnit.mm},
            {"Q", LengthUnit.Q},
            {"in", LengthUnit.Inches},
            {"pc", LengthUnit.pc},
            {"pt", LengthUnit.pt},
            {"px", LengthUnit.px},
        };

        public LengthContext(Shape owner, LengthUnit unit)
        {
            Owner = owner;
            Unit = unit;
        }

        public static LengthUnit Parse(string text, LengthOrientation orientation = LengthOrientation.None)
        {
            if (String.IsNullOrEmpty(text))
            {
                return LengthUnit.Number;
            }
            string trimmed = text.Trim();
            if(trimmed == "%")
            {
                switch (orientation)
                {
                    case LengthOrientation.Horizontal:
                        return LengthUnit.PercentWidth;
                    case LengthOrientation.Vertical:
                        return LengthUnit.PercentHeight;
                    default:
                        return LengthUnit.PercentDiagonal;
                }
            }
            if(_unitMap.TryGetValue(trimmed, out LengthUnit unit))
            {
                return unit;
            }
            return LengthUnit.Unknown;
        }
    }




}
