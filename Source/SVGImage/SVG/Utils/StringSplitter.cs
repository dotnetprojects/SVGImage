using System;
using System.Linq;
using System.Xml;
using System.Windows;

namespace SVGImage.SVG.Utils
{
    internal sealed class StringSplitter
    {
        private string m_value;
        private int m_curPos = -1;
        //private char[] NumberChars = "0123456789.-".ToCharArray();
        
        public StringSplitter(string value)
        {
            this.m_value = value;
            this.m_curPos = 0;
        }
        
        public void SetString(string value, int startpos)
        {
            this.m_value = value;
            this.m_curPos = startpos;
        }
        
        public bool More
        {
            get
            {
                return this.m_curPos >= 0 && this.m_curPos < this.m_value.Length;
            }
        }

        public double ReadNextValue()
        {
            int startpos = this.m_curPos;
            if (startpos < 0)
                startpos = 0;
            if (startpos >= this.m_value.Length)
                return float.NaN;
            string numbers = "0123456789-.eE";
            // find start of a number
            while (startpos < this.m_value.Length && numbers.Contains(this.m_value[startpos]) == false)
                startpos++;
            // end of number
            int endpos = startpos;
            while (endpos < this.m_value.Length && numbers.Contains(this.m_value[endpos]))
            {
                // '-' if number is followed by '-' then it indicates the end of the value
                if (endpos != startpos && this.m_value[endpos] == '-' &&
                    this.m_value[endpos - 1] != 'e' && this.m_value[endpos - 1] != 'E')
                    break;
                endpos++;
            }
            int len = endpos - startpos;
            if (len <= 0)
                return float.NaN;
            this.m_curPos = endpos;
            string s = this.m_value.Substring(startpos, len);

            // find start of a next number
            startpos = endpos;
            while (startpos < this.m_value.Length && numbers.Contains(this.m_value[startpos]) == false)
                startpos++;
            if (startpos >= this.m_value.Length)
                endpos = startpos;

            this.m_curPos = endpos;
            return XmlConvert.ToDouble(s);
        }

        public Point ReadNextPoint()
        {
            double x = this.ReadNextValue();
            double y = this.ReadNextValue();
            return new Point(x, y);
        }
    }
}
