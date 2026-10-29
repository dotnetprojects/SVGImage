using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG.Shapes
{
    public sealed class Clip : Group
    {
        private readonly bool boundingBox;
        public Clip(SVG svg, XmlNode node, Shape parent) : base(svg,node,parent)
        {
            boundingBox = XmlUtil.AttrValue(node,"clipPathUnits") == "objectBoundingBox";
        }
        public Geometry ClipGeometry => GetGeometry(new Rect(0,0,1,1));
        internal Geometry GetGeometry(Rect bounds, HashSet<Shape> active = null, Func<Shape, Geometry, Geometry> animate = null)
        {
            Geometry geometry = ShapeGeometry.Create(this,active,animate);
            if (boundingBox)
            {
                if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0) return Geometry.Empty;
                geometry = new GeometryGroup { Children = { geometry } };
                var matrix = geometry.Transform?.Value ?? Matrix.Identity;
                matrix.Append(new Matrix(bounds.Width,0,0,bounds.Height,bounds.X,bounds.Y));
                geometry.Transform = new MatrixTransform(matrix);
            }
            return geometry;
        }
    }
}
