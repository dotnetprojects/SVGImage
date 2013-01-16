using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Xml;

namespace SVGImage.SVG.PaintServer
{
    internal class RadialGradientColorPaintServerPaintServer : GradientColorPaintServer
    {
        public double CX { get; private set; }

        public double CY { get; private set; }

        public double FX { get; private set; }

        public double FY { get; private set; }

        public double R { get; private set; }

        public string Id { get; private set; }

        private Brush brush = null;

        public RadialGradientColorPaintServerPaintServer(PaintServerManager owner, XmlNode node)
            : base(owner, node)
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
            if (this.brush != null) return this.brush;

            RadialGradientBrush b = new RadialGradientBrush();
            foreach (GradientStop stop in this.Stops) b.GradientStops.Add(stop);

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
                double scale = 1d / 100d;
                if (double.IsNaN(this.CX) == false && double.IsNaN(this.CY) == false)
                {
                    //b.GradientOrigin = new System.Windows.Point(this.CX*scale, this.CY*scale);
                    b.Center = new System.Windows.Point(this.CX /* *scale */, this.CY /* *scale */);
                }
                if (double.IsNaN(this.FX) == false && double.IsNaN(this.FY) == false)
                {
                    b.GradientOrigin = new System.Windows.Point(this.FX * scale, this.FY * scale);
                }
                if (double.IsNaN(this.R) == false)
                {
                    b.RadiusX = this.R /* *scale*/;
                    b.RadiusY = this.R /* *scale*/;
                }
                b.MappingMode = BrushMappingMode.RelativeToBoundingBox;
            }
            if (this.Transform != null) b.Transform = this.Transform;

            this.brush = b;

            return b;
        }

        private void Normalize()
        {
        }
    }
}
