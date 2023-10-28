using System;
using System.Xml;

namespace SVGImage.SVG.Utils
{
    internal static class XmlUtil
    {
        static bool GetValueRespectingUnits(string inputstring, out double value, double percentageMaximum)
        {
            value = 0;
            var units = string.Empty;
            int index = inputstring.LastIndexOfAny(new char[] { '.', '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
            if (index >= 0)
            {
                string svalue = inputstring.Substring(0, index + 1);
                if (index + 1 < inputstring.Length)
                    units = inputstring.Substring(index + 1);
                try
                {
                    value = XmlConvert.ToDouble(svalue);

                    switch (units) // from http://www.selfsvg.info/?section=3.4
                    {
                        case "pt": value = value * 1.25; break;
                        case "mm": value = value * 3.54; break;
                        case "pc": value = value * 15; break;
                        case "cm": value = value * 35.43; break;
                        case "in": value = value * 90; break;
                        case "%": value = value * percentageMaximum / 100; break;
                    }

                    return true;
                }
                catch (FormatException)
                { }
            }
            return false;
        }

        public static double GetDoubleValue(string value, double percentageMaximum = 1)
        {
            double result = 0;
            if (GetValueRespectingUnits(value, out result, percentageMaximum))
            {
                return result;
            }
            return 0;
        }

        public static double AttrValue(StyleItem attr)
        {
            double result = 0;
            GetValueRespectingUnits(attr.Value, out result, 1);
            return result;
        }

        public static double AttrValue(XmlNode node, string id, double defaultvalue, double percentageMaximum = 1)
        {
            XmlAttribute attr = node.Attributes[id];
            if (attr == null)
                return defaultvalue;

            double result = 0;
            if (attr != null && GetValueRespectingUnits(attr.Value, out result, percentageMaximum))
            {
                return result;
            }
            return defaultvalue;
        }

        public static string AttrValue(XmlNode node, string id, string defaultvalue)
        {
            if (node.Attributes == null)
                return defaultvalue;
            XmlAttribute attr = node.Attributes[id];
            if (attr != null)
                return attr.Value;
            return defaultvalue;
        }

        public static string AttrValue(XmlNode node, string id)
        {
            return AttrValue(node, id, string.Empty);
        }

        public static double ParseDouble(SVG svg, string svalue)
        {
            double value = 0d;
            if (GetValueRespectingUnits(svalue, out value, 1))
                return value;
            return 0.1;
        }
    }
}
