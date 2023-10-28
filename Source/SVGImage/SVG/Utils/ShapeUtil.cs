using System;
using System.Collections.Generic;
using System.Linq;

using System.Windows.Media;

namespace SVGImage.SVG.Utils
{
    internal static class ShapeUtil
    {
        public static Transform ParseTransform(string value)
        {
            //todo, increase perf. and object creation of this code (check with acid after)
            var transforms = value.Split(')');
            if (transforms.Length == 2)
                return ParseTransformInternal(value);

            var tg = new TransformGroup();
            // to check why ordering is needed (see acid.svg)
            foreach (var transform in transforms.OrderBy(x => x.StartsWith(SVGTags.sTranslate)))
            {
                if (!string.IsNullOrEmpty(transform))
                {
                    var transObj = ParseTransformInternal(transform + ")");
                    if (transObj != null)
                    {
                        tg.Children.Add(transObj);
                    }
                }
            }
            return tg;
        }

        private static Transform ParseTransformInternal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }
            value = value.Trim();

            string type = ExtractUntil(value, '(').TrimStart(',');
            string v1 = ExtractBetween(value, '(', ')');

            StringSplitter split = new StringSplitter(v1);
            List<double> values = new List<double>();
            while (split.More)
                values.Add(split.ReadNextValue());
            if (type == SVGTags.sTranslate)
                if (values.Count == 1)
                    return new TranslateTransform(values[0], 0);
                else
                    return new TranslateTransform(values[0], values[1]);
            if (type == SVGTags.sMatrix)
                return Transform.Parse(v1);
            if (type == SVGTags.sScale)
                if (values.Count == 1)
                    return new ScaleTransform(values[0], values[0]);
                else
                    return new ScaleTransform(values[0], values[1]);
            if (type == SVGTags.sSkewX)
                return new SkewTransform(values[0], 0);
            if (type == SVGTags.sSkewY)
                return new SkewTransform(0, values[0]);
            if (type == SVGTags.sRotate)
            {
                if (values.Count == 1)
                    return new RotateTransform(values[0], 0, 0);
                if (values.Count == 2)
                    return new RotateTransform(values[0], values[1], 0);
                return new RotateTransform(values[0], values[1], values[2]);
            }

            return null;
        }

        public static string ExtractUntil(string value, char ch)
        {
            int index = value.IndexOf(ch);
            if (index <= 0)
                return value;
            return value.Substring(0, index);
        }

        public static string ExtractBetween(string value, char startch, char endch)
        {
            int start = value.IndexOf(startch);
            if (startch < 0)
                return value;
            start++;
            int end = value.IndexOf(endch, start);
            if (endch < 0)
                return value.Substring(start);
            return value.Substring(start, end - start);
        }
    }
}
