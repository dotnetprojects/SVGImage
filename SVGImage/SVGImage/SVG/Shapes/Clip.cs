using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml;

namespace SVGImage.SVG.Shapes
{
    internal class Clip : Group
    {
        public Clip(SVG svg, XmlNode node, Shape parent)
            : base(svg, node, parent)
        {
        }

        private Geometry clpGeo = null;

        public Geometry ClipGeometry
        {
            get
            {
                if (clpGeo != null) return clpGeo;

                var retVal = this.getGeoForShape(this.Elements[0]);

                if (this.Elements.Count > 1)
                    foreach (var element in this.Elements)
                    {
                        retVal = new CombinedGeometry(retVal, this.getGeoForShape(element));
                    }

                clpGeo = retVal;
                return clpGeo;
            }
        }

        private Geometry getGeoForShape(Shape shp)
        {
            if (shp is RectangleShape)
            {
                var r = shp as RectangleShape;
                return new RectangleGeometry(new Rect(r.RX, r.RY, r.Width, r.Height));
            }
            if (shp is CircleShape)
            {
                var c = shp as CircleShape;
                return new EllipseGeometry(new Point(c.CX, c.CY), c.R, c.R);
            }
            return null;
        }
    }
}
