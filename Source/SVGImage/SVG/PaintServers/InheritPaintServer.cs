﻿using System.Windows;
using System.Windows.Media;

namespace SVGImage.SVG.PaintServers
{
    public sealed class InheritPaintServer : PaintServer
    {
        public InheritPaintServer(PaintServerManager owner)
            : base(owner)
        {
        }

        public override Brush GetBrush(double opacity, SVG svg, SVGRender svgRender, Rect bounds)
        {
            return null;
        }
    }
}
