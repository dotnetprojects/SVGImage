using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG.Animation
{
    // Resolve forward syncbase references independently of drawing traversal.
    internal sealed class SmilTimeline
    {
        private readonly Dictionary<string, XmlNode> ids = new Dictionary<string, XmlNode>();
        private readonly Dictionary<XmlNode, AnimationTrack> parsed = new Dictionary<XmlNode, AnimationTrack>();
        private readonly Dictionary<AnimationTrack, double[]> clockBegins = new Dictionary<AnimationTrack, double[]>();
        private readonly Dictionary<string, List<double>> events = new Dictionary<string, List<double>>();
        internal SmilTimeline(SVG svg)
        {
            foreach (XmlNode node in svg.RootGroup.SourceNode.SelectNodes(".//*[@id]"))
                if (node.LocalName == "animate" || node.LocalName == "animateTransform" || node.LocalName == "animateMotion" || node.LocalName == "set")
                {
                    string id = XmlUtil.AttrValue(node, "id");
                    if (id.Length > 0 && !ids.ContainsKey(id)) ids[id] = node;
                }
        }
        internal AnimationTrack Track(XmlNode node)
        {
            AnimationTrack track;
            if (!parsed.TryGetValue(node, out track))
            {
                track = AnimationTrack.Parse(node);
                parsed[node] = track;
                var starts = track.Begin.Split(';').Select(AnimationTrack.Clock).ToArray();
                if (starts.All(value => !double.IsNaN(value)))
                {
                    clockBegins[track] = starts;
                    track.StartTime = t => ClockBegin(starts, t);
                }
                else track.StartTime = t => Begin(track, t, new HashSet<AnimationTrack>());
            }
            return track;
        }
        internal void Trigger(string name, double time)
        {
            List<double> times;
            if (!events.TryGetValue(name, out times)) events[name] = times = new List<double>();
            times.Add(time);
        }
        private double Begin(AnimationTrack track, double limit, HashSet<AnimationTrack> active)
        {
            double[] starts;
            if (clockBegins.TryGetValue(track, out starts))
            {
                return ClockBegin(starts, limit);
            }
            if (double.IsNaN(limit) || active.Count > 128 || !active.Add(track)) return double.NegativeInfinity;
            double best = double.NegativeInfinity;
            var periods = new List<double>();
            try
            {
                foreach (string token in track.Begin.Split(';'))
                {
                    string value = token.Trim();
                    double start = AnimationTrack.Clock(value);
                    if (double.IsNaN(start))
                    {
                        var sync = Regex.Match(value, @"^(.+)\.(begin|end)\s*(?:([+-])\s*(.+))?$");
                        var click = Regex.Match(value, @"^(.+)(\.click)\s*(?:([+-])\s*(.+))?$");
                        if (sync.Success)
                        {
                            XmlNode node;
                            if (!ids.TryGetValue(sync.Groups[1].Value, out node)) continue;
                            AnimationTrack parent;
                            try { parent = Track(node); } catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is OverflowException) { continue; }
                            double offset = Offset(sync), duration = sync.Groups[2].Value == "end" ? Math.Min(parent.Duration * parent.Repeat, parent.RepeatDuration) : 0;
                            if (double.IsNaN(offset) || double.IsInfinity(duration)) continue;
                            if (ReferenceEquals(parent, track))
                            {
                                if (offset + duration > 0) periods.Add(offset + duration);
                                continue;
                            }
                            start = Begin(parent, limit - offset - duration, active) + offset + duration;
                        }
                        else if (click.Success)
                        {
                            List<double> times;
                            if (!events.TryGetValue(click.Groups[1].Value + click.Groups[2].Value, out times)) continue;
                            double offset = Offset(click);
                            start = double.NegativeInfinity;
                            foreach (double time in times) if (time + offset <= limit) start = Math.Max(start, time + offset);
                        }
                        else continue;
                    }
                    if (start <= limit) best = Math.Max(best, start);
                }
                if (!double.IsInfinity(best))
                    foreach (double period in periods) best += Math.Floor((limit - best) / period) * period;
                return best;
            }
            finally { active.Remove(track); }
        }
        private static double ClockBegin(double[] starts, double limit)
        {
            double latest = double.NegativeInfinity;
            foreach (double start in starts) if (start <= limit) latest = Math.Max(latest, start);
            return latest;
        }
        private static double Offset(Match match)
        {
            return match.Groups[3].Success ? AnimationTrack.Clock(match.Groups[4].Value) * (match.Groups[3].Value == "-" ? -1 : 1) : 0;
        }
    }
}
