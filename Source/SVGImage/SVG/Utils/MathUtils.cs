using SVGImage.SVG.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace SVGImage.SVG.Utils
{
    internal static class MathUtil
    {
        public static bool IsNearlyZero(this double value, double epsilon = Double.Epsilon)
        {
            return Math.Abs(value) < epsilon;
        }
        public static bool IsNearlyEqual(this double a, double b, double epsilon = Double.Epsilon)
        {
            return Math.Abs(a - b) < epsilon;
        }
        public static int Clamp(int value, int min, int max)
        {
            if (min > max)
            {
                throw new ArgumentException("min must be less than or equal to max", nameof(min));
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
