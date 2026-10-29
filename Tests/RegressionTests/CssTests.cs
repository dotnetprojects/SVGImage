using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using SVGImage.SVG;

internal static class CssTests
{
    internal static void Run()
    {
        ColorAt("<defs><g id='eye'><circle cx='10' cy='10' r='10' fill='inherit'/></g></defs><use href='#eye' fill='white'/>", Colors.White, "explicit fill inherit follows the use instance");
        ColorAt("<style>rect {fill:red}</style><rect fill='blue' width='20' height='20'/>", Colors.Red, "CSS overrides presentation attributes");
        ColorAt("<style>#target {fill:red}.a {fill:blue} rect {fill:green}</style><rect id='target' class='a' width='20' height='20'/>", Colors.Red, "specificity");
        ColorAt("<style>.b {fill:blue}.a {fill:red}</style><rect class='a b' width='20' height='20'/>", Colors.Red, "class order does not set precedence");
        ColorAt("<style>.a {fill:blue}.a {fill:red}</style><rect class='a' width='20' height='20'/>", Colors.Red, "source order breaks ties");
        ColorAt("<style>#target {fill:blue}</style><rect id='target' style='fill:red' width='20' height='20'/>", Colors.Red, "inline specificity");
        ColorAt("<style>rect {fill:red !important}</style><rect style='fill:blue' width='20' height='20'/>", Colors.Red, "important beats normal inline");
        ColorAt("<style>#target {fill:blue !important}</style><rect id='target' style='fill:red!important' width='20' height='20'/>", Colors.Red, "important inline wins");
        ColorAt("<style>g.theme > rect.a.b[data-role='main'] {fill:red}</style><g class='theme'><rect class='b a' data-role='main' width='20' height='20'/></g>", Colors.Red, "compound, child and attribute selectors");
        ColorAt("<style>g.theme rect {fill:red}</style><g class='theme'><g><rect width='20' height='20'/></g></g>", Colors.Red, "descendant selector");
        ColorAt("<style>circle + rect {fill:red}</style><circle r='0'/><!--comment--><rect width='20' height='20'/>", Colors.Red, "adjacent sibling skips comments");
        ColorAt("<style>circle ~ rect {fill:red}</style><circle r='0'/><g/><rect width='20' height='20'/>", Colors.Red, "general sibling");
        ColorAt("<style>circle, rect {fill:red}</style><rect width='20' height='20'/>", Colors.Red, "selector list whitespace");
        ColorAt("<style>@media print {rect {fill:blue}} rect {fill:red}</style><rect width='20' height='20'/>", Colors.Red, "unsupported conditional rules do not leak");
        ColorAt("<style>rect:hover {fill:blue}</style><rect width='20' height='20' fill='red'/>", Colors.Red, "unsupported pseudo selector ignored");
        ColorAt("<style>rect {fill:red;display:inline}</style><rect display='none' width='20' height='20'/>", Colors.Red, "cascade can restore display");
        ColorAt("<style>:root {--ink:red} rect {fill:var(--ink)}</style><rect width='20' height='20'/>", Colors.Red, "inherited CSS variable");
        ColorAt("<g style='--ink:blue'><rect style='--ink:red;fill:var(--ink)' width='20' height='20'/></g>", Colors.Red, "local variable override");
        ColorAt("<rect style='fill:var(--missing,var(--other,red))' width='20' height='20'/>", Colors.Red, "nested fallback");
        ColorAt("<g fill='red'><rect style='--a:var(--b);--b:var(--a);fill:var(--a)' fill='blue' width='20' height='20'/></g>", Colors.Red, "invalid variable becomes unset");
        ColorAt("<rect style='--a:var(--b);--b:var(--a);--c:var(--a,red);fill:var(--c)' width='20' height='20'/>", Colors.Red, "cycle fallback outside the cycle");
        ColorAt("<g style='--a:red;--b:var(--a)'><rect style='--a:blue;fill:var(--b)' width='20' height='20'/></g>", Colors.Red, "variables inherit their computed value");
        ColorAt("<rect style='--unused:url(&quot;data:image/png;base64,a:b;c&quot;);fill:red' width='20' height='20'/>", Colors.Red, "declaration tokenizer preserves quoted separators");
        ColorAt("<g color='red'><g><rect fill='currentColor' width='20' height='20'/></g></g>", Colors.Red, "currentColor inherits through groups");
        ColorAt("<style>:root {--ink:red} stop {stop-color:var(--ink) !important}</style>"+
            "<defs><linearGradient id='g'><stop stop-color='blue' offset='0'/><stop stop-color='blue' offset='1'/></linearGradient></defs>"+
            "<rect fill='url(#g)' width='20' height='20'/>", Colors.Red, "gradient stops use the CSS cascade and variables");
        ColorAt("<rect width='20' height='20' fill='rgba(255,0,0,.5)'/>",Color.FromArgb(128,255,0,0),"CSS fractional alpha");
        ColorAt("<rect width='20' height='20' fill='#f008'/>",Color.FromArgb(136,255,0,0),"CSS short alpha hex");
        ColorAt("<rect width='20' height='20' fill='#ff000080'/>",Color.FromArgb(128,255,0,0),"CSS RGBA hex");
        ColorAt("<rect width='20' height='20' fill='hsl(120,100%,50%)'/>",Colors.Lime,"CSS HSL color");
        ColorAt("<rect width='20' height='20' fill='rgb(-10%,120%,0%)'/>",Colors.Lime,"CSS channel clamping");
        ColorAt("<rect width='20' height='20' fill='rgb(-5% 128 0%)'/>", Colors.Green, "modern RGB mixes percentage and numeric channels");
        ColorAt("<rect width='20' height='20' fill='rgb(64 50% 100%)'/>", Color.FromRgb(64,128,255), "modern RGB mixed units in other channel positions");
        ColorAt("<rect width='20' height='20' style='fill:rgb(\n -5%\t128\n 0%\n)'/>", Colors.Green, "modern RGB accepts CSS whitespace and newlines");
        ColorAt("<rect width='20' height='20' fill='rgba(25% 128 100% / 50%)'/>", Color.FromArgb(128,64,128,255), "modern RGBA mixed units and percentage alpha");
        ColorAt("<rect width='20' height='20' fill='rgb(0% 128 0% / .5)'/>", Color.FromArgb(128,0,128,0), "modern RGB numeric alpha after slash");
        foreach (string invalid in new[] { "rgb(-5%,128,0%)", "rgb(0 128 0 .5)", "rgb(0,128,0 / .5)", "rgb(0 128 0 / .5 / .5)" })
            ColorAt("<defs><filter id='invalid'><feFlood flood-color='" + invalid + "'/></filter></defs><rect width='20' height='20' filter='url(#invalid)'/>", Colors.Black, "invalid RGB separators/legacy mixed units use flood default: " + invalid);
        AnimationTiming();
        Console.WriteLine("CSS cascade, selector and custom-property checks passed.");
    }

    private static void AnimationTiming()
    {
        const string frames = "@keyframes move {from{transform:translateX(20px)}to{transform:translateX(40px)}}";
        Func<string, double, double> x = (style, seconds) => At(frames + "rect{" + style + "}", "<rect width='5' height='5' transform='translate(10)'/>", seconds).Bounds.X;
        Near(10, x("animation:move 1s linear 1s", .5), "positive delay retains underlying transform");
        Near(20, x("animation:move 1s linear 1s backwards", .5), "backwards fill");
        Near(30, x("animation:move 1s linear 1s both", 1.5), "delayed interpolation");
        Near(40, x("animation:move 1s linear 1s both", 3), "forwards fill");
        Near(25, x("animation:move 1s linear -.25s", 0), "negative delay");
        Near(35, x("animation:move 1s linear reverse", .25), "reverse");
        Near(35, x("animation:move 1s linear 3 alternate", 1.25), "alternate");
        Near(25, x("animation:move 1s linear 3 alternate-reverse", 1.25), "alternate reverse");
        Near(30, x("animation:move 1s linear 2.5 forwards", 3), "fractional iteration count");
        Near(20, x("animation:move 1s linear paused", .5), "paused starts at zero");
        Near(30, x("animation:1s linear move", .5), "unordered shorthand");
        Near(25, x("animation-name:move;animation-duration:2s;animation-timing-function:linear", .5), "animation longhands");
        Near(25, x("animation:move 1s linear;animation-duration:2s", .5), "longhand overrides shorthand");
        Near(30, x("animation-duration:2s;animation:move 1s linear", .5), "shorthand resets longhands");
        Near(30, x("--motion:move 1s linear;animation:var(--motion)", .5), "variable in shorthand");
        Near(26.307, x("animation:move 1s ease-in", .5), "ease-in", .02);
        Near(36.048, x("animation:move 1s ease", .5), "default ease", .02);
        Near(25, x("animation:move 1s cubic-bezier(0,0,1,1)", .25), "cubic bezier");
        Near(25, x("animation:move 1s steps(4,start)", .125), "step start");
        Near(20, x("animation:move 1s steps(4,end)", .125), "step end");
        Near(24, x("animation:move 1s steps(4,jump-both)", .125), "steps jump both");
        Near(20 + 20.0 / 3, x("animation:move 1s steps(4,jump-none)", .375), "steps jump none");

        var drawing = At("@keyframes spin {from{transform:rotate(0deg)}to{transform:rotate(1turn)}}" +
            "rect{animation:spin 1s linear;transform-origin:50px 50px}", "<rect x='60' y='50' width='10' height='5'/>", .25);
        Near(45, drawing.Bounds.X, "full turn rotation retains angle");
        Near(60, drawing.Bounds.Y, "animated transform origin");
        drawing = At("@keyframes spin {to{transform:rotate(360deg)}}rect{animation:spin 1s linear;transform-origin:50px 50px}",
            "<rect x='60' y='50' width='10' height='5'/>", .25);
        Near(45,drawing.Bounds.X,"to-only full rotation uses identity transform functions");
        drawing = At("rect{transform:rotate(.25turn);transform-origin:center;transform-box:fill-box}", "<rect x='20' y='20' width='20' height='10'/>", 0);
        Near(25, drawing.Bounds.X, "static CSS fill-box origin X");
        Near(15, drawing.Bounds.Y, "static CSS fill-box origin Y");

        drawing = At("@keyframes fade {0%,100%{opacity:0}50%{opacity:1}}rect{animation:fade 2s linear}", "<rect width='20' height='20' fill='red'/>", .5);
        var pixels = AnimationTests.Image(drawing,100,100,new Rect(0,0,100,100));
        Near(128, pixels[(5*100+5)*4+3], "opacity keyframes and grouped offsets", 1);
        drawing = At("@keyframes m {to{transform:translateX(80px)}}@keyframes f {from{opacity:0}to{opacity:1}}" +
            "rect{animation-name:m,f;animation-duration:2s;animation-timing-function:linear}", "<rect width='20' height='20' fill='red'/>", .5);
        Near(20, drawing.Bounds.X, "multiple CSS animations");
        pixels=AnimationTests.Image(drawing,100,100,new Rect(0,0,100,100));
        Near(64,pixels[(5*100+25)*4+3],"animation list repeats shorter longhand lists",1);
    }
    private static DrawingGroup At(string css, string content, double seconds)
    {
        var renderer = new SVGRender { UseAnimations = true };
        var drawing = renderer.LoadXmlDrawing("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'>" + content + "<style>"+css+"</style></svg>");
        renderer.SeekAnimations(TimeSpan.FromSeconds(seconds));
        return drawing;
    }
    private static void Near(double expected, double actual, string name, double tolerance=.01)
    {
        if(Math.Abs(expected-actual)>tolerance) throw new Exception(name+": expected "+expected+", got "+actual);
    }

    private static void ColorAt(string source, Color expected, string name)
    {
        var drawing = new SVGRender().LoadXmlDrawing("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'>" + source + "</svg>");
        var pixels = AnimationTests.Image(drawing, 100, 100, new Rect(0, 0, 100, 100));
        int offset = (5 * 100 + 5) * 4;
        if (pixels[offset] != expected.B * expected.A / 255 || pixels[offset + 1] != expected.G * expected.A / 255 || pixels[offset + 2] != expected.R * expected.A / 255 || pixels[offset + 3] != expected.A)
            throw new Exception(name + ": wrong rendered color " + string.Join(",", pixels.Skip(offset).Take(4)));
    }
}
