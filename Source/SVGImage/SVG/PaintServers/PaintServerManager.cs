using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media;
using System.Xml;

namespace SVGImage.SVG.PaintServers
{
    using Utils;

    public sealed class PaintServerManager
    {
        internal SVG Svg { get; set; }
        private static Dictionary<string, Color> m_knownColors = null;

        private Dictionary<string, PaintServer> m_servers = new Dictionary<string, PaintServer>();

        public string Create(SVG svg, XmlNode node)
        {
            if (node.Name == SVGTags.sLinearGradient)
            {
                string id = XmlUtil.AttrValue(node, "id");
                if (this.m_servers.ContainsKey(id) == false) this.m_servers[id] = new LinearGradientColorPaintServer(this, node);
                return id;
            }
            if (node.Name == SVGTags.sRadialGradient)
            {
                string id = XmlUtil.AttrValue(node, "id");
                if (this.m_servers.ContainsKey(id) == false) this.m_servers[id] = new RadialGradientColorPaintServer(this, node);
                return id;
            }
            if (node.Name == SVGTags.sPattern)
            {
                string id = XmlUtil.AttrValue(node, "id");
                if (this.m_servers.ContainsKey(id) == false) this.m_servers[id] = new PatternPaintServer(this, svg, node);
                return id;
            }
            return null;
        }

        public bool ContainsServer(string serverKey)
        {
            if (string.IsNullOrWhiteSpace(serverKey))
            {
                return false;
            }

            return m_servers.ContainsKey(serverKey);
        }

        public void AddServer(string key, PaintServer server)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            m_servers[key] = server;
        }

        public PaintServer GetServer(string serverKey)
        {
            if (string.IsNullOrWhiteSpace(serverKey))
            {
                return null;
            }

            PaintServer result;
            if (serverKey != null && m_servers.TryGetValue(serverKey, out result)) return result;
            else return null;
        }

        public Dictionary<string, PaintServer> GetServers()
        {
            return m_servers;
        }

        public void CreateServerFromBrush(string key, Brush customBrush)
        {
            if (customBrush is SolidColorBrush)
            {
                m_servers[key] = new SolidColorPaintServer(this, customBrush);
            }
            else if (customBrush is LinearGradientBrush)
            {
                m_servers[key] = new LinearGradientColorPaintServer(this, customBrush);
            }
            else if(customBrush is RadialGradientBrush)
            {
                m_servers[key] = new RadialGradientColorPaintServer(this, customBrush);
            }
            else if(customBrush is DrawingBrush)
            {
                m_servers[key] = new PatternPaintServer(this, customBrush);
            }
            else if(customBrush != null)
            {
                m_servers[key] = new PaintServer(this, customBrush);
            }
        }

        public string Parse(string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value)) return null;
                if (value == SVGTags.sNone) return null;
                PaintServer result = null;
                if (this.m_servers.TryGetValue(value, out result)) return value;
                if (value == SVGTags.sInherit)
                {
                    m_servers[value] = new InheritPaintServer(this);
                    return value;
                }
                if (value == SVGTags.sCurrentColor)
                {
                    m_servers[value] = new CurrentColorPaintServer(this);
                    return value;
                }

                if (value.StartsWith("url"))
                {
                    string id = ShapeUtil.ExtractBetween(value, '(', ')').Trim().Trim('\'', '"');
                    if (id.Length > 0 && id[0] == '#') id = id.Substring(1);
                    this.m_servers.TryGetValue(id, out result);
                    return id;
                }

                Color color;
                if (CssColor.TryParse(value,out color))
                {
                    string id="_css"+value;
                    m_servers[id]=new SolidColorPaintServer(this,color);
                    return id;
                }
                return null;
            }
            catch (Exception) { }

            return null;
        }

        public static Color ParseHexColor(string value)
        {
            // format is #xxFF00FF where xx is optional (the a value)
            // if format ix #rgb then the values are replicated #rrggbb
            int start = 0;
            if (value[start] == '#') start++;

            ulong u;
            if (value.Length <= 4)
            {
                uint @int = Convert.ToUInt32(value.Substring(start), 16);
                uint newval = 0;
                newval |= (@int & 0x000f00) << 12;
                newval |= (@int & 0x000f00) << 8;
                newval |= (@int & 0x0000f0) << 8;
                newval |= (@int & 0x0000f0) << 4;
                newval |= (@int & 0x00000f) << 4;
                newval |= (@int & 0x00000f);
                u = newval;
            }
            else
            {
                u = Convert.ToUInt64(value.Substring(start), 16);
            }

            byte a = (byte)((u & 0xff000000) >> 24);
            byte r = (byte)((u & 0x00ff0000) >> 16);
            byte g = (byte)((u & 0x0000ff00) >> 8);
            byte b = (byte)(u & 0x000000ff);
            if (a == 0) a = 255;
            return Color.FromArgb(a, r, g, b);
        }

        public static Color KnownColor(string value)
        {
            LoadKnownColors();
            if (m_knownColors.ContainsKey(value)) return m_knownColors[value];
            return Colors.Black;
        }

        private static void LoadKnownColors()
        {
            if (m_knownColors == null) 
                m_knownColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            if (m_knownColors.Count == 0)
            {
                PropertyInfo[] propinfos = typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static);
                foreach (PropertyInfo info in propinfos)
                {
                    if (info.PropertyType == typeof(Color)) 
                        m_knownColors[info.Name.ToLower()] = (Color)info.GetValue(typeof(Color), null);
                }
                // SVG/CSS accepts the British spellings that WPF omits.
                foreach (string name in new[] { "grey", "darkgrey", "darkslategrey", "dimgrey", "lightgrey", "lightslategrey", "slategrey" })
                    m_knownColors[name] = m_knownColors[name.Replace("grey", "gray")];
                m_knownColors["rebeccapurple"] = Color.FromRgb(102, 51, 153);
            }
        }
    }
}
