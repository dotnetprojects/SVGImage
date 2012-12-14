using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Reflection;
using System.Xml;

namespace SVGImage.SVG
{
	// http://www.w3.org/TR/SVGTiny12/painting.html#PaintServers
	abstract public class PaintServer
	{
		public PaintServerManager Owner { get; private set;}
		public PaintServer(PaintServerManager owner)
		{
			this.Owner = owner;
		}
		public abstract Brush GetBrush(double opacity, SVG svg);
	}
	public class PaintServerManager
	{
		static Dictionary<string, Color> m_knownColors = null;
		Dictionary<string, PaintServer> m_servers = new Dictionary<string,PaintServer>();
		public PaintServer Create(XmlNode node)
		{
			if (node.Name == SVGTags.sLinearGradient)
			{
				string id = XmlUtil.AttrValue(node, "id");
				if (this.m_servers.ContainsKey(id) == false)
					this.m_servers[id] = new LinearGradientColor(this, node);
				return this.m_servers[id];
			}
			if (node.Name == SVGTags.sRadialGradient)
			{
				string id = XmlUtil.AttrValue(node, "id");
				if (this.m_servers.ContainsKey(id) == false)
					this.m_servers[id] = new RadialGradientColor(this, node);
				return this.m_servers[id];
			}
			return null;
		}
		public PaintServer Parse(string value)
		{
		    if (string.IsNullOrEmpty(value))
                return null;
			if (value == "none")
				return null;
			if (value[0] == '#')
				return this.ParseSolidColor(value);
			PaintServer result = null;;
			if (this.m_servers.TryGetValue(value, out result))
				return result;
			if (value.StartsWith("url"))
			{
				string id = ShapeUtil.ExtractBetween(value, '(', ')');
				if (id.Length > 0 && id[0] == '#')
					id = id.Substring(1);
				this.m_servers.TryGetValue(id, out result);
				return result;
			}
			return this.ParseKnownColor(value);
		}
		public static Color ParseHexColor(string value)
		{
			// format is #xxFF00FF where xx is optional (the a value)
			// if format ix #rgb then the values are replicated #rrggbb
			int start = 0;
			if (value[start] == '#')
				start++;

			uint u = Convert.ToUInt32(value.Substring(start), 16);
			if (value.Length <= 4)
			{
				uint newval = 0;
				newval |= (u & 0x000f00) << 12;
				newval |= (u & 0x000f00) << 8;
				newval |= (u & 0x0000f0) << 8;
				newval |= (u & 0x0000f0) << 4;
				newval |= (u & 0x00000f) << 4;
				newval |= (u & 0x00000f);
				u = newval;
			}
			byte a = (byte)((u & 0xff000000) >> 24);
			byte r = (byte)((u & 0x00ff0000) >> 16);
			byte g = (byte)((u & 0x0000ff00) >> 8);
			byte b = (byte)(u & 0x000000ff);
			if (a == 0)
				a = 255;
			return Color.FromArgb(a, r, g, b);
		}
		public static Color KnownColor(string value)
		{
			LoadKnownColors();
			if (m_knownColors.ContainsKey(value))
				return m_knownColors[value];
			return Colors.Black;
		}
		SolidColor ParseSolidColor(string value)
		{
			string id = "_solid" + value;
			PaintServer result;
			if (this.m_servers.TryGetValue(id, out result))
				return result as SolidColor;
			result = new SolidColor(this, ParseHexColor(value));
			this.m_servers[id] = result;
			return result as SolidColor;
		}
		SolidColor ParseKnownColor(string value)
		{
			LoadKnownColors();
			PaintServer result;
			if (this.m_servers.TryGetValue(value, out result))
				return result as SolidColor;
			Color c;
			if (m_knownColors.TryGetValue(value, out c))
			{
				result = new SolidColor(this, c);
				this.m_servers[value] = result;
				return result as SolidColor;
			}
			return null;
		}
		static void LoadKnownColors()
		{
			if (m_knownColors == null)
				m_knownColors = new Dictionary<string,Color>();
			if (m_knownColors.Count == 0)
			{
				PropertyInfo[] propinfos = typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static);
				foreach (PropertyInfo info in propinfos)
				{
					if (info.PropertyType == typeof(Color))
						m_knownColors[info.Name.ToLower()] = (Color)info.GetValue(typeof(Color), null);
				}
			}
		}
	}

	class SolidColor : PaintServer
	{
		public Color Color { get; set; }
		public SolidColor(PaintServerManager owner, Color c) : base(owner)
		{
			this.Color = c;
		}
		public override Brush GetBrush(double opacity, SVG svg)
		{
			byte a = (byte)(255 * opacity / 100);
			Color c = this.Color;
			Color newcol = System.Windows.Media.Color.FromArgb(a, c.R, c.G, c.B);
			return new SolidColorBrush(newcol);
		}
	}
	abstract class GradientColor : PaintServer
	{
		// http://www.w3.org/TR/SVG11/pservers.html#LinearGradients
		List<GradientStop> m_stops = new List<GradientStop>();
		public IList<GradientStop> Stops
		{
			get { return this.m_stops.AsReadOnly(); }
		}
		public Transform Transform { get; protected set;}
		public string GradientUnits {get; private set;}
		
		public GradientColor(PaintServerManager owner, XmlNode node) : base(owner)
		{
			this.GradientUnits = XmlUtil.AttrValue(node, "gradientUnits", string.Empty);
			string transform = XmlUtil.AttrValue(node, "gradientTransform", string.Empty);
			if (transform.Length > 0)
			{
				this.Transform = ShapeUtil.ParseTransform(transform.ToLower());
			}

			if (node.ChildNodes.Count == 0 && XmlUtil.AttrValue(node, "xlink:href", string.Empty).Length > 0)
			{
				string refid = XmlUtil.AttrValue(node, "xlink:href", string.Empty);
				GradientColor refcol = owner.Parse(refid.Substring(1)) as GradientColor;
				if (refcol == null)
					return;
				this.m_stops = new List<GradientStop>(refcol.m_stops);
			}
			foreach (XmlNode childnode in node.ChildNodes)
			{
				if (childnode.Name == "stop")
				{
					List<XmlAttribute> styleattr = new List<XmlAttribute>();
					string fullstyle = XmlUtil.AttrValue(childnode, SVGTags.sStyle, string.Empty);
					if (fullstyle.Length > 0)
					{
						foreach (ShapeUtil.Attribute styleitem in XmlUtil.SplitStyle(null, fullstyle))
							styleattr.Add(new XmlUtil.StyleItem(childnode, styleitem.Name, styleitem.Value));
					}
					foreach (XmlAttribute attr1 in styleattr)
						childnode.Attributes.Append(attr1);


					double offset = XmlUtil.AttrValue(childnode, "offset", (double)0);
					string s = XmlUtil.AttrValue(childnode, "stop-color", "#0");

					double stopopacity = XmlUtil.AttrValue(childnode, "stop-opacity", (double)1);

					Color color;
					if (s.StartsWith("#"))
						color = PaintServerManager.ParseHexColor(s);
					else
						color = PaintServerManager.KnownColor(s);

					if (stopopacity != 1)
						color = Color.FromArgb((byte)(stopopacity*255), color.R, color.G, color.B);

					if (offset > 1)
						offset = offset / 100;
					this.m_stops.Add(new GradientStop(color, offset));
				}
			}
		}
	}
	
	class LinearGradientColor : GradientColor
	{
		public double X1 {get; private set;}
		public double Y1 {get; private set;}
		public double X2 {get; private set;}
		public double Y2 {get; private set;}
		public string Id {get; private set;}

		public LinearGradientColor(PaintServerManager owner, XmlNode node) : base(owner, node)
		{
			System.Diagnostics.Debug.Assert(node.Name == SVGTags.sLinearGradient);
			this.Id = XmlUtil.AttrValue(node, "id");
			this.X1 = XmlUtil.AttrValue(node, "x1", double.NaN);
			this.Y1 = XmlUtil.AttrValue(node, "y1", double.NaN);
			this.X2 = XmlUtil.AttrValue(node, "x2", double.NaN);
			this.Y2 = XmlUtil.AttrValue(node, "y2", double.NaN);
		}
		public override Brush GetBrush(double opacity, SVG svg)
		{
			LinearGradientBrush b = new LinearGradientBrush();
			foreach (GradientStop stop in this.Stops)
				b.GradientStops.Add(stop);

			b.MappingMode = BrushMappingMode.RelativeToBoundingBox;
			b.StartPoint = new System.Windows.Point(0, 0);
			b.EndPoint = new System.Windows.Point(1, 0);
			
			if (this.GradientUnits == SVGTags.sGradientUserSpace)
			{
				Transform tr = this.Transform as Transform;
				if (tr != null)
				{
					b.StartPoint = tr.Transform(new System.Windows.Point(this.X1, this.Y1));
					b.EndPoint = tr.Transform(new System.Windows.Point(this.X2, this.Y2));
				}
				else
				{
					b.StartPoint = new System.Windows.Point(this.X1, this.Y1);
					b.EndPoint = new System.Windows.Point(this.X2, this.Y2);
				}
				this.Transform = null;
				b.MappingMode = BrushMappingMode.Absolute;
			}
			else
			{
				this.Normalize();
				if (double.IsNaN(this.X1) == false)
					b.StartPoint = new System.Windows.Point(this.X1, this.Y1);
				if (double.IsNaN(this.X2) == false)
					b.EndPoint = new System.Windows.Point(this.X2, this.Y2);
			}
			return b;
		}
		void Normalize()
		{
			// This is until proper 'userspace' is supported.
			// crude normalization of the transition points.
			// gradient transition line is alwaysfrom 0 to 1
			if (double.IsNaN(this.X1) == false && double.IsNaN(this.X2) == false)
			{
				double min = this.X1;
				if (this.X2 < this.X1)
					min = this.X2;
				this.X1 -= min;
				this.X2 -= min;
				double scale = this.X1;
				if (this.X2 > this.X1)
					scale = this.X2;
				if (scale != 0)
				{
					this.X1 /= scale;
					this.X2 /= scale;
				}
			}
			if (double.IsNaN(this.Y1) == false && double.IsNaN(this.Y2) == false)
			{
				double min = this.Y1;
				if (this.Y2 < this.Y1)
					min = this.Y2;
				this.Y1 -= min;
				this.Y2 -= min;
				double scale = this.Y1;
				if (this.Y2 > this.Y1)
					scale = this.Y2;
				if (scale != 0)
				{
					this.Y1 /= scale;
					this.Y2 /= scale;
				}
			}
		}
	}

	class RadialGradientColor : GradientColor
	{
		public double CX {get; private set;}
		public double CY {get; private set;}
		public double FX {get; private set;}
		public double FY {get; private set;}
		public double R {get; private set;}
		public string Id {get; private set;}
		
		public RadialGradientColor(PaintServerManager owner, XmlNode node) : base(owner, node)
		{
			System.Diagnostics.Debug.Assert(node.Name == SVGTags.sRadialGradient);
			this.Id = XmlUtil.AttrValue(node, "id");

			this.CX = XmlUtil.AttrValue(node, "cx", double.NaN);
			this.CY = XmlUtil.AttrValue(node, "cy", double.NaN);
			this.FX = XmlUtil.AttrValue(node, "fx", double.NaN);
			this.FY = XmlUtil.AttrValue(node, "fy", double.NaN);
			this.R = XmlUtil.AttrValue(node, "r", double.NaN);
			this.Normalize();
		}
		public override Brush GetBrush(double opacity, SVG svg)
		{
			RadialGradientBrush b = new RadialGradientBrush();
			foreach (GradientStop stop in this.Stops)
				b.GradientStops.Add(stop);

			b.GradientOrigin = new System.Windows.Point(0.5, 0.5);
            b.Center = new System.Windows.Point(0.5, 0.5);
            b.RadiusX = 0.5; 
            b.RadiusY = 0.5;

			if (this.GradientUnits == SVGTags.sGradientUserSpace)
			{
				b.Center = new System.Windows.Point(this.CX, this.CY);
				b.GradientOrigin = new System.Windows.Point(this.FX, this.FY);
				b.RadiusX = this.R;
				b.RadiusY = this.R;
				b.MappingMode = BrushMappingMode.Absolute;
			}
			else
			{
				double scale = 1d/100d;
				if (double.IsNaN(this.CX) == false && double.IsNaN(this.CY) == false)
				{
					b.GradientOrigin = new System.Windows.Point(this.CX*scale, this.CY*scale);
					b.Center = new System.Windows.Point(this.CX*scale, this.CY*scale);
				}
				if (double.IsNaN(this.FX) == false && double.IsNaN(this.FY) == false)
				{
					b.GradientOrigin = new System.Windows.Point(this.FX*scale, this.FY*scale);
				}
				if (double.IsNaN(this.R) == false)
				{
					b.RadiusX = this.R*scale;
					b.RadiusY = this.R*scale;
				}
				b.MappingMode = BrushMappingMode.RelativeToBoundingBox;
			}
			if (this.Transform != null)
				b.Transform = this.Transform;
			return b;
		}
		void Normalize()
		{
		}
	}
}
