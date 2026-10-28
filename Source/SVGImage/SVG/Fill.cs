using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace SVGImage.SVG
{
    using PaintServers;
    using Shapes;

    public sealed class Fill
    {
        public enum eFillRule
        {
            nonzero,
            evenodd
        }

        private SVG _svg;
        private readonly Shape owner;
        private bool hasPaint;
        private bool hasOpacity;
        private bool hasFillRule;

        private bool _isDefault;

        private eFillRule _fillRule;

        private string _paintServerKey;

        private double _opacity;

        public Fill(SVG svg)
        {
            _fillRule = eFillRule.nonzero;
            _opacity = 100;
            _isDefault = false;
            _svg = svg;
        }

        internal Fill(SVG svg, Shape owner) : this(svg)
        {
            this.owner = owner;
        }

        public static Fill CreateDefault(SVG svg, string fillColor)
        {
            var fill = new Fill(svg);
            fill.PaintServerKey = svg.PaintServers.Parse(fillColor);

            fill._isDefault = true;

            return fill;
        }

        public SVG get => _svg;

        public bool IsDefault
        {
            get => _isDefault;
            set => _isDefault = value;
        }

        public eFillRule FillRule { 
            get => !hasFillRule && owner != null ? owner.InheritedFill?.FillRule ?? eFillRule.nonzero : _fillRule;
            set {
                Debug.Assert(_isDefault == false);
                hasFillRule = true;
                _fillRule = value;
            }
        }

        public string PaintServerKey 
        {
            get
            {
                if (hasPaint || owner == null) return _paintServerKey;
                // Fill properties inherit independently: fill-rule alone must not
                // reset an inherited white mask fill (or an explicit fill="none").
                var inherited = owner.InheritedFill;
                return inherited != null ? inherited.PaintServerKey : _svg.PaintServers.Parse("black");
            }
            set {
                Debug.Assert(_isDefault == false);
                hasPaint = true;
                _paintServerKey = value;
            }
        }

        public double Opacity 
        {
            get => !hasOpacity && owner != null ? owner.InheritedFill?.Opacity ?? 100 : _opacity;
            set {
                Debug.Assert(_isDefault == false);
                hasOpacity = true;
                _opacity = value;
            }
        }

        public bool IsEmpty(SVG svg)
        {
            if (svg == null) return true;

            if (!svg.PaintServers.ContainsServer(this.PaintServerKey))
            {
                return true;
            }
            return svg.PaintServers.GetServer(this.PaintServerKey) == null;
        }

        public Brush FillBrush(SVG svg, SVGRender svgRender, Shape shape, double elementOpacity, Rect bounds)
        {
            var paintServer = svg.PaintServers.GetServer(this.PaintServerKey);
            if(paintServer != null)
            {
                if(paintServer is CurrentColorPaintServer)
                {
                    var shapePaintServer = svg.PaintServers.GetServer(shape.PaintServerKey);
                    if(shapePaintServer != null)
                    {
                        return shapePaintServer.GetBrush(this.Opacity * elementOpacity, svg, svgRender, bounds);

                    }
                }
                if (paintServer is InheritPaintServer)
                {
                    var p = shape.RealParent ?? shape.Parent;
                    while (p != null)
                    {
                        if(p.Fill != null)
                        {
                            var checkPaintServer = svg.PaintServers.GetServer(p.Fill.PaintServerKey);
                            if(checkPaintServer != null && !(checkPaintServer is InheritPaintServer))
                            {
                                return checkPaintServer.GetBrush(this.Opacity * elementOpacity, svg, svgRender, bounds);
                            }
                        }
                        p = p.RealParent ?? p.Parent;
                    }
                    return null;
                }
                return paintServer.GetBrush(this.Opacity * elementOpacity, svg, svgRender, bounds);
            }
            return null;
        }
    }
}
