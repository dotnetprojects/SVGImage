using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace SVGImage.SVG
{
    using PaintServers;
    using Shapes;

    public sealed class Stroke
    {
        public enum eLineCap
        {
            butt,
            round,
            square,
        }

        public enum eLineJoin
        {
            miter,
            round,
            bevel,
        }

        private SVG _svg;
        private readonly Shape owner;
        private bool hasPaint, hasOpacity, hasWidth, hasLineCap, hasLineJoin, hasStrokeArray;

        private bool _isDefault;

        private string _paintServerKey;

        private double _opacity;

        private double _width;

        private eLineCap _lineCap;

        private eLineJoin _lineJoin;

        private double[] _strokeArray;

        public Stroke(SVG svg)
        {
            _svg = svg;
            _isDefault = false;
            _width = 1;
            _lineCap = eLineCap.butt;
            _lineJoin = eLineJoin.miter;
            _opacity = 100;
        }

        internal Stroke(SVG svg, Shape owner) : this(svg)
        {
            this.owner = owner;
        }

        public static Stroke CreateDefault(SVG svg, double width)
        {
            var stroke = new Stroke(svg);
            stroke._width = width;

            stroke._isDefault = true;

            return stroke;
        }

        public SVG get => _svg;

        public bool IsDefault
        {
            get => _isDefault;
            set => _isDefault = value;
        }

        public string PaintServerKey
        {
            get => !hasPaint && owner != null ? owner.InheritedStroke?.PaintServerKey : _paintServerKey;
            set {
                Debug.Assert(_isDefault == false);
                hasPaint = true;
                _paintServerKey = value;
            }
        }

        public double Opacity
        {
            get => !hasOpacity && owner != null ? owner.InheritedStroke?.Opacity ?? 100 : _opacity;
            set {
                Debug.Assert(_isDefault == false);
                hasOpacity = true;
                _opacity = value;
            }
        }

        public double Width
        {
            get => !hasWidth && owner != null ? owner.InheritedStroke?.Width ?? 1 : _width;
            set {
                Debug.Assert(_isDefault == false);
                hasWidth = true;
                _width = value;
            }
        }

        public eLineCap LineCap
        {
            get => !hasLineCap && owner != null ? owner.InheritedStroke?.LineCap ?? eLineCap.butt : _lineCap;
            set {
                Debug.Assert(_isDefault == false);
                hasLineCap = true;
                _lineCap = value;
            }
        }

        public eLineJoin LineJoin
        {
            get => !hasLineJoin && owner != null ? owner.InheritedStroke?.LineJoin ?? eLineJoin.miter : _lineJoin;
            set {
                Debug.Assert(_isDefault == false);
                hasLineJoin = true;
                _lineJoin = value;
            }
        }

        public double[] StrokeArray
        {
            get => !hasStrokeArray && owner != null ? owner.InheritedStroke?.StrokeArray : _strokeArray;
            set {
                Debug.Assert(_isDefault == false);
                hasStrokeArray = true;
                _strokeArray = value;
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

        public Brush StrokeBrush(SVG svg, SVGRender svgRender, Shape shape, double elementOpacity, Rect bounds)
        {
            var paintServer = svg.PaintServers.GetServer(PaintServerKey);
            if (paintServer != null)
            {
                if (paintServer is CurrentColorPaintServer)
                {
                    var shapePaintServer = svg.PaintServers.GetServer(shape.PaintServerKey);
                    if (shapePaintServer != null)
                    {
                        return shapePaintServer.GetBrush(this.Opacity * elementOpacity, svg, svgRender, bounds);

                    }
                }
                if (paintServer is InheritPaintServer)
                {
                    var p = shape.RealParent ?? shape.Parent;
                    while (p != null)
                    {
                        if (p.Stroke != null)
                        {
                            var checkPaintServer = svg.PaintServers.GetServer(p.Stroke.PaintServerKey);
                            if (!(checkPaintServer is InheritPaintServer))
                            {
                                return checkPaintServer?.GetBrush(this.Opacity * elementOpacity, svg, svgRender, bounds);
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
