using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG.Animation
{
    internal sealed class AnimationTrack
    {
        internal string Attribute, Type, Begin, Mode;
        internal bool Additive, Freeze;
        internal double Duration, Repeat = 1, RepeatDuration = double.PositiveInfinity;
        internal string[] Values;
        internal double[] Times;
        internal KeySpline[] Splines;
        internal bool IsCss, Backwards, Paused;
        internal double Delay;
        internal string Direction;
        internal string[] CssTiming;
        internal Matrix UnderlyingMatrix = System.Windows.Media.Matrix.Identity;
        internal string UnderlyingTransform;
        internal PathGeometry MotionPath;
        internal string MotionRotate;
        private double[][] numericValues;
        private CssTransforms[] cssValues;

        internal static AnimationTrack Parse(XmlNode node)
        {
            var track = new AnimationTrack
            {
                Attribute = XmlUtil.AttrValue(node, "attributeName"),
                Type = node.LocalName == "animateTransform" ? XmlUtil.AttrValue(node, "type", "translate") : null,
                Begin = XmlUtil.AttrValue(node, "begin", "0s"),
                Duration = Clock(XmlUtil.AttrValue(node, "dur", "indefinite")),
                Additive = XmlUtil.AttrValue(node, "additive") == "sum",
                Freeze = XmlUtil.AttrValue(node, "fill") == "freeze",
                Mode = XmlUtil.AttrValue(node, "calcMode", "linear")
            };
            if(node.LocalName=="animateMotion")
            {
                track.Type="motion";
                track.Attribute="transform";
                track.Additive=true;
                track.MotionRotate=XmlUtil.AttrValue(node,"rotate","0");
                string path=XmlUtil.AttrValue(node,"path");
                if(path.Length>0) track.MotionPath=SvgPathData.Parse(path);
            }
            string repeat = XmlUtil.AttrValue(node, "repeatCount", "1");
            track.Repeat = repeat == "indefinite" ? double.PositiveInfinity : Number(repeat);
            string repeatDuration = XmlUtil.AttrValue(node, "repeatDur");
            if (repeatDuration.Length > 0)
            {
                track.RepeatDuration = Clock(repeatDuration);
                if (node.Attributes["repeatCount"] == null) track.Repeat = double.PositiveInfinity;
            }
            string values = XmlUtil.AttrValue(node, "values");
            if (node.LocalName == "set")
                track.Values = new[] { XmlUtil.AttrValue(node, "to") };
            else if (values.Length > 0) track.Values = values.Split(';').Select(v => v.Trim()).ToArray();
            else
            {
                string from = XmlUtil.AttrValue(node, "from", track.Type == "scale" ? "1" : "0");
                track.Values = new[] { from, XmlUtil.AttrValue(node, "to", from) };
            }
            if(track.Type=="motion" && (track.MotionPath!=null || node.ChildNodes.Cast<XmlNode>().Any(child => child.LocalName=="mpath")))
            {
                string points=XmlUtil.AttrValue(node,"keyPoints");
                track.Values=points.Length>0 ? points.Split(';') : new[] { "0", "1" };
            }
            string times = XmlUtil.AttrValue(node, "keyTimes");
            track.Times = times.Length > 0 ? times.Split(';').Select(Number).ToArray()
                : Enumerable.Range(0, track.Values.Length).Select(i => track.Values.Length == 1 ? 0 : (double)i / (track.Values.Length - 1)).ToArray();
            if (track.Times.Length != track.Values.Length || track.Times.Any(t => t < 0 || t > 1) ||
                track.Times.Where((t, i) => i > 0 && t < track.Times[i - 1]).Any())
                throw new FormatException("Invalid animation keyTimes");
            string splines = XmlUtil.AttrValue(node, "keySplines");
            if (splines.Length > 0)
                track.Splines = splines.Split(';').Select(s =>
                {
                    var n = Numbers(s);
                    if (n.Length == 0) return new KeySpline(0, 0, 1, 1); // Flash2Svg can leave a final linear segment empty.
                    if (n.Length != 4) throw new FormatException("Invalid keySpline");
                    return new KeySpline(n[0], n[1], n[2], n[3]);
                }).ToArray();
            return track;
        }

        internal bool Sample(double seconds, IDictionary<string, double> events, out int index, out double fraction)
        {
            index = 0; fraction = 0;
            if (IsCss) return CssSample(seconds, out index, out fraction);
            double start = double.NegativeInfinity;
            foreach (string begin in Begin.Split(';'))
            {
                double candidate = Clock(begin.Trim());
                if (double.IsNaN(candidate))
                {
                    var eventMatch = Regex.Match(begin.Trim(), @"^(.+\.click)([+-].+)?$");
                    if (eventMatch.Success && events.TryGetValue(eventMatch.Groups[1].Value, out candidate))
                        candidate += eventMatch.Groups[2].Success ? Clock(eventMatch.Groups[2].Value) : 0;
                    else continue;
                }
                if (candidate <= seconds) start = Math.Max(start, candidate);
            }
            if (double.IsInfinity(start) || double.IsNaN(Duration) || Duration <= 0 || Repeat <= 0) return false;
            double elapsed = seconds - start;
            double total = Math.Min(Duration * Repeat, RepeatDuration);
            if (total <= 0 || double.IsNaN(total)) return false;
            if (elapsed >= total && !Freeze) return false;
            double progress = double.IsInfinity(Duration) ? 0 : elapsed >= total ?
                (total % Duration == 0 ? 1 : (total % Duration) / Duration) : (elapsed % Duration) / Duration;
            while (index < Values.Length - 1 && progress >= Times[index + 1]) index++;
            if (index < Values.Length - 1 && Times[index + 1] > Times[index])
                fraction = (progress - Times[index]) / (Times[index + 1] - Times[index]);
            if (Mode == "discrete" || Attribute == "display" || Attribute == "visibility") fraction = 0;
            else if (Mode == "spline" && Splines != null && index < Splines.Length)
                fraction = Splines[index].GetSplineProgress(Math.Max(0, Math.Min(1, fraction)));
            return true;
        }

        private bool CssSample(double seconds, out int index, out double fraction)
        {
            index = 0; fraction = 0;
            if (double.IsNaN(Duration) || double.IsNaN(Delay) || Duration < 0 || Repeat <= 0) return false;
            double elapsed = (Paused ? 0 : seconds) - Delay;
            bool before = elapsed < 0;
            double total = Duration * Repeat;
            if (before && !Backwards || !before && elapsed >= total && !Freeze) return false;
            double iteration, progress;
            if (before) { iteration = 0; progress = 0; }
            else if (Duration == 0 || elapsed >= total)
            {
                iteration = Math.Max(0, Math.Ceiling(Repeat) - 1);
                progress = Repeat % 1 == 0 ? 1 : Repeat % 1;
            }
            else { iteration = Math.Floor(elapsed / Duration); progress = elapsed / Duration - iteration; }
            bool reversed = Direction == "reverse" || Direction == "alternate-reverse";
            if ((Direction == "alternate" || Direction == "alternate-reverse") && iteration % 2 == 1) reversed = !reversed;
            if (reversed) progress = 1 - progress;
            while (index < Values.Length - 1 && progress >= Times[index + 1]) index++;
            if (index < Values.Length - 1 && Times[index + 1] > Times[index])
                fraction = (progress - Times[index]) / (Times[index + 1] - Times[index]);
            if (index < Values.Length - 1)
                fraction = CssAnimations.Ease(CssTiming[index], fraction, before);
            return true;
        }

        internal double[] Components(int index, double fraction)
        {
            if (numericValues == null) numericValues = Values.Select(Numbers).ToArray();
            var first = numericValues[index];
            var second = numericValues[Math.Min(index + 1, Values.Length - 1)];
            int length = Math.Max(first.Length, second.Length);
            var result = new double[length];
            for (int i = 0; i < length; i++)
            {
                double a = i < first.Length ? first[i] : Type == "scale" && first.Length > 0 ? first[0] : 0;
                double b = i < second.Length ? second[i] : Type == "scale" && second.Length > 0 ? second[0] : 0;
                result[i] = a + (b - a) * fraction;
            }
            return result;
        }

        internal Matrix Matrix(int index, double fraction)
        {
            if(Type=="motion")
            {
                var values=Components(index,fraction);
                Point position; Point tangent;
                if(MotionPath!=null) MotionPath.GetPointAtFractionLength(Math.Max(0,Math.Min(1,values[0])),out position,out tangent);
                else {
                    position=new Point(values.Length>0?values[0]:0,values.Length>1?values[1]:0);
                    var first=Numbers(Values[index]); var last=Numbers(Values[Math.Min(index+1,Values.Length-1)]);
                    tangent=new Point((last.Length>0?last[0]:0)-(first.Length>0?first[0]:0),(last.Length>1?last[1]:0)-(first.Length>1?first[1]:0));
                }
                var motion=System.Windows.Media.Matrix.Identity;
                double angle=MotionRotate=="auto" || MotionRotate=="auto-reverse" ? Math.Atan2(tangent.Y,tangent.X)*180/Math.PI+(MotionRotate=="auto-reverse"?180:0) : Number(MotionRotate);
                motion.Rotate(angle); motion.Translate(position.X,position.Y); return motion;
            }
            if (Type == "css")
            {
                if (cssValues == null) cssValues = Values.Select(valueToken => valueToken == "__underlying__" ?
                    UnderlyingTransform != null ? CssTransforms.Parse(UnderlyingTransform) : UnderlyingMatrix.IsIdentity ? CssTransforms.Parse("none") : CssTransforms.FromMatrix(UnderlyingMatrix)
                    : CssTransforms.Parse(valueToken)).ToArray();
                var a = cssValues[index];
                var b = cssValues[Math.Min(index + 1, Values.Length - 1)];
                return a.Interpolate(b, fraction);
            }
            var v = Components(index, fraction);
            double x = v.Length > 0 ? v[0] : 0, y = v.Length > 1 ? v[1] : 0;
            var matrix = System.Windows.Media.Matrix.Identity;
            switch (Type)
            {
                case "translate": matrix.Translate(x, y); break;
                case "scale": matrix.Scale(x, v.Length > 1 ? y : x); break;
                case "rotate": matrix.RotateAt(x, y, v.Length > 2 ? v[2] : 0); break;
                case "skewX": matrix.Skew(x, 0); break;
                case "skewY": matrix.Skew(0, x); break;
            }
            return matrix;
        }

        internal static Matrix CssMatrix(string value)
        {
            var result = System.Windows.Media.Matrix.Identity;
            foreach (Match match in Regex.Matches(value, @"([a-zA-Z]+)\(([^)]*)\)"))
            {
                var n = Numbers(match.Groups[2].Value);
                if (n.Length == 0) continue;
                var m = System.Windows.Media.Matrix.Identity;
                switch (match.Groups[1].Value)
                {
                    case "translateY": m.Translate(0, n[0]); break;
                    case "translateX": m.Translate(n[0], 0); break;
                    case "translate": m.Translate(n[0], n.Length > 1 ? n[1] : 0); break;
                    case "scale": m.Scale(n[0], n.Length > 1 ? n[1] : n[0]); break;
                    case "rotate": m.Rotate(n[0]); break;
                    case "matrix": if (n.Length == 6) m = new Matrix(n[0], n[1], n[2], n[3], n[4], n[5]); break;
                }
                result.Prepend(m);
            }
            return result;
        }

        internal static double Clock(string text)
        {
            text = text.Trim();
            if (text == "indefinite") return double.PositiveInfinity;
            double multiplier = text.EndsWith("ms") ? .001 : text.EndsWith("min") ? 60 : text.EndsWith("h") ? 3600 : 1;
            text = Regex.Replace(text, @"(?:ms|min|s|h)$", "");
            double number;
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number) ? number * multiplier : double.NaN;
        }

        internal static double Number(string text) => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        internal static double[] Numbers(string text) => Regex.Matches(text, @"[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?").Cast<Match>().Select(m => Number(m.Value)).ToArray();
    }
}
