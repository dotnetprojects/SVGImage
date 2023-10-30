using System;

namespace SVGImage.SVG
{
    public class StyleItem
    {
        private string _name;
        private string _value;

        public StyleItem(string name, string value)
        {
            _name = name;
            _value = value;
        }

        public string Name { get => _name; set => _name = value; }
        public string Value { get => _value; set => _value = value; }

        public override string ToString()
        {
            return string.Format("{0}:{1}", _name, _value);
        }

        public static StyleItem ReadNextAttr(string inputstring, ref int startpos)
        {
            if (inputstring[startpos] != ' ')
                throw new Exception("inputstring[startpos] must be a whitepace character");
            while (inputstring[startpos] == ' ')
                startpos++;

            int namestart = startpos;
            int nameend = inputstring.IndexOf('=', startpos);
            if (nameend < namestart)
                throw new Exception("did not find xml attribute name");

            int valuestart = inputstring.IndexOf('"', nameend);
            valuestart++;
            int valueend = inputstring.IndexOf('"', valuestart);
            if (valueend < 0 || valueend < valuestart)
                throw new Exception("did not find xml attribute value");

            // search for first occurence of x="yy"
            string attrName = inputstring.Substring(namestart, nameend - namestart).Trim();
            string attrValue = inputstring.Substring(valuestart, valueend - valuestart).Trim();
            startpos = valueend + 1;

            return new StyleItem(attrName, attrValue);
        }

    }
}