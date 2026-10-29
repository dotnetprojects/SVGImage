using System;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using SVGImage.SVG;

internal static class AdvancedAnimationTests
{
    private static string Svg(string content) => "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'>" + content + "</svg>";
    private static SVGRender Renderer() => new SVGRender { UseAnimations = true };
    internal static void Run()
    {
        Morph(); Timing(); Masks(); Transforms(); Samples();
        Console.WriteLine("Path morphs, syncbase timing, animated masks, decomposed matrices and percentage translations passed.");
    }
    private static void Morph()
    {
        var r=Renderer();
        var d=r.LoadXmlDrawing(Svg("<path d='M10 10L30 10L30 30L10 30Z' fill='red'><animate attributeName='d' values='M10 10L30 10L30 30L10 30Z;M50 50L90 50L90 90L50 90Z' dur='2s' fill='freeze'/></path>"));
        r.SeekAnimations(TimeSpan.FromSeconds(1));
        Near(30,d.Bounds.X,"morph midpoint x"); Near(30,d.Bounds.Width,"morph midpoint width");
        GeometryTests.Pixel(d,40,40,Colors.Red,"morphed path interior");
        r.SeekAnimations(TimeSpan.FromSeconds(3)); Near(50,d.Bounds.X,"morph frozen endpoint");
        d=r.LoadXmlDrawing(Svg("<path d='M0 0Q10 20 20 0'><animate attributeName='d' to='M40 20Q60 60 80 20' dur='2s'/></path>"));
        r.SeekAnimations(TimeSpan.FromSeconds(1)); Near(20,d.Bounds.X,"to-only morph uses original path");
        d=r.LoadXmlDrawing(Svg("<path d='M0 0L10 10'><animate attributeName='d' values='M0 0L10 10;M0 0C10 0 10 10 20 20' dur='1s'/></path>"));
        if (!r.AnimationWarnings.Any(w=>w.Contains("matching figures"))) throw new Exception("Incompatible path morph must report a warning");
        d=r.LoadXmlDrawing(Svg("<clipPath id='c'><path d='M0 0H20V100H0Z'><animate attributeName='d' values='M0 0H20V100H0Z;M80 0H100V100H80Z' dur='2s'/></path></clipPath><rect width='100' height='100' fill='blue' clip-path='url(#c)'/>"));
        r.SeekAnimations(TimeSpan.FromSeconds(1)); GeometryTests.Pixel(d,50,50,Colors.Blue,"path morph in clip"); GeometryTests.Pixel(d,10,50,Colors.Transparent,"morph clip moved");
        var live=r.LoadXmlDrawing(Svg("<path d='M0 0H10V10H0Z'><animate attributeName='d' values='M0 0H10V10H0Z;M80 0H90V10H80Z' dur='1s'/></path>"));
        double before=live.Bounds.X; Tick(); if(live.Bounds.X<=before+1) throw new Exception("Path morph native clock did not advance");
    }
    private static void Timing()
    {
        var r=Renderer();
        var d=r.LoadXmlDrawing(Svg("<rect width='10' height='10'><animateTransform type='translate' from='0' to='80' begin='first.end + 0.5s' dur='2s' fill='freeze'/></rect><g><animate id='first' attributeName='opacity' values='1;1' begin='1s' dur='2s' repeatCount='2'/></g>"));
        r.SeekAnimations(TimeSpan.FromSeconds(5)); Near(0,d.Bounds.X,"syncbase waits for repeated active duration");
        r.SeekAnimations(TimeSpan.FromSeconds(6.5)); Near(40,d.Bounds.X,"forward end reference with offset");
        d=r.LoadXmlDrawing(Svg("<rect width='10' height='10'><animateTransform id='loop' type='translate' from='0' to='80' begin='0s;loop.end+1s' dur='1s'/></rect>"));
        r.SeekAnimations(TimeSpan.FromSeconds(100.5)); Near(40,d.Bounds.X,"seeded self-end syncbase loops at long times");
        d=r.LoadXmlDrawing(Svg("<rect width='10' height='10'><animateTransform type='translate' from='0' to='80' begin='clicker.begin+0.25s' dur='1s'/></rect><g><animate id='clicker' attributeName='opacity' values='1;1' begin='play.click' dur='1s'/></g>"));
        r.SeekAnimations(TimeSpan.Zero); r.TriggerAnimationEvent("play"); r.SeekAnimations(TimeSpan.FromSeconds(.75)); Near(40,d.Bounds.X,"syncbase chain starts from click");
        r.SeekAnimations(TimeSpan.FromSeconds(5)); r.TriggerAnimationEvent("play"); r.SeekAnimations(TimeSpan.FromSeconds(.75)); Near(40,d.Bounds.X,"seeking retains earlier event instances");
        d=r.LoadXmlDrawing(Svg("<rect width='10' height='10'><animateTransform id='a' type='translate' from='0' to='80' begin='b.end' dur='1s'/><animate id='b' attributeName='opacity' from='1' to='1' begin='a.end' dur='1s'/></rect>"));
        r.SeekAnimations(TimeSpan.FromSeconds(100)); Near(0,d.Bounds.X,"unseeded syncbase cycle is inactive");
    }
    private static void Masks()
    {
        foreach(string type in new[]{"alpha","luminance"})
        {
            var r=Renderer();
            string fill=type=="alpha"?"black":"white";
            var d=r.LoadXmlDrawing(Svg("<mask id='m' mask-type='"+type+"' maskUnits='userSpaceOnUse' x='0' y='0' width='100' height='100'><circle cx='20' cy='50' r='15' fill='"+fill+"'><animate attributeName='cx' values='20;80' dur='2s'/></circle></mask><rect width='100' height='100' fill='red' mask='url(#m)'/>"));
            r.SeekAnimations(TimeSpan.FromSeconds(1)); GeometryTests.Pixel(d,50,50,Colors.Red,type+" animated mask"); GeometryTests.Pixel(d,20,50,Colors.Transparent,type+" original mask position cleared");
        }
        var renderer=Renderer();
        string colored="<mask id='m' maskUnits='userSpaceOnUse' x='0' y='0' width='100' height='100'><rect width='100' height='100' fill='white'/><circle cx='20' cy='50' r='15' fill='black'><animate attributeName='cx' values='20;80' dur='2s'/></circle></mask><rect width='100' height='100' fill='red' mask='url(#m)'/>";
        var drawing=renderer.LoadXmlDrawing(Svg(colored));
        renderer.SeekAnimations(TimeSpan.FromSeconds(1)); GeometryTests.Pixel(drawing,50,50,Colors.Transparent,"animated black-over-white mask cutout"); GeometryTests.Pixel(drawing,20,50,Colors.Red,"animated luminance mask restores previous position");
        var cutoutBefore = AnimationTests.Image(drawing,100,100,new Rect(0,0,100,100));
        renderer.SeekAnimations(TimeSpan.FromSeconds(1.02));
        var cutoutAfter = AnimationTests.Image(drawing,100,100,new Rect(0,0,100,100));
        int edge = (50*100+35)*4+3;
        if (cutoutAfter[edge] - cutoutBefore[edge] < 40) throw new Exception("Mask seeking must refresh within the same 30 Hz playback bucket");
        drawing=renderer.LoadXmlDrawing(Svg(colored.Replace("dur='2s'","dur='.5s'")));
        var before=AnimationTests.Image(drawing,100,100,new Rect(0,0,100,100)); Tick();
        if(before.SequenceEqual(AnimationTests.Image(drawing,100,100,new Rect(0,0,100,100)))) throw new Exception("Live luminance mask did not refresh");
    }
    private static void Transforms()
    {
        var r=Renderer();
        var d=r.LoadXmlDrawing(Svg("<style>@keyframes m{from{transform:matrix(1,0,0,1,0,0)}to{transform:matrix(0,1,-1,0,0,0)}}rect{animation:m 2s linear}</style><rect x='20' width='10' height='5'/>"));
        r.SeekAnimations(TimeSpan.FromSeconds(1)); Near(10.6066,d.Bounds.X,"matrix rotation decomposes rather than collapsing",.001); Near(10.6066,d.Bounds.Height,"matrix midpoint rotated bounds",.001);
        d=r.LoadXmlDrawing(Svg("<style>@keyframes p{from{transform:rotate(0deg) translateX(0)}to{transform:rotate(360deg) scale(1)}}rect{animation:p 2s linear}</style><rect x='10' width='10' height='5'/>"));
        r.SeekAnimations(TimeSpan.FromSeconds(1)); Near(-20,d.Bounds.X,"compatible prefix preserves full turns before matrix fallback");
        d=r.LoadXmlDrawing(Svg("<rect x='10' y='20' width='20' height='10' style='transform-box:fill-box;transform:translate(50%,100%)'/>"));
        Near(20,d.Bounds.X,"static percentage x uses fill box"); Near(30,d.Bounds.Y,"static percentage y uses fill box");
        d=r.LoadXmlDrawing(Svg("<style>@keyframes p{to{transform:translate(100%,50%)}}rect{transform-box:fill-box;animation:p 2s linear}</style><rect width='20' height='10'/>"));
        r.SeekAnimations(TimeSpan.FromSeconds(1)); Near(10,d.Bounds.X,"animated percentage x"); Near(2.5,d.Bounds.Y,"animated percentage y");
        d=r.LoadXmlDrawing(Svg("<svg x='20' width='40' height='40' viewBox='0 0 200 200'><rect width='10' height='10' transform='translate(50%,25%)'/></svg>"));
        Near(40,d.Bounds.X,"nested percentage viewport x"); Near(10,d.Bounds.Y,"nested percentage viewport y");
    }
    private static void Samples()
    {
        foreach (string file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory,"AnimationFeatures"),"*.svg"))
        {
            var renderer=Renderer(); var drawing=renderer.LoadDrawing(file);
            renderer.SeekAnimations(TimeSpan.Zero);
            var first=AnimationTests.Image(drawing,300,200,new Rect(0,0,300,200));
            renderer.SeekAnimations(TimeSpan.FromSeconds(1.25));
            var second=AnimationTests.Image(drawing,300,200,new Rect(0,0,300,200));
            if (first.SequenceEqual(second)) throw new Exception(Path.GetFileName(file)+": sample animation has no changed frame");
            if (renderer.AnimationWarnings.Count!=0) throw new Exception(Path.GetFileName(file)+": "+string.Join(", ",renderer.AnimationWarnings));
        }
    }
    private static void Tick()
    {
        var frame=new DispatcherFrame(); var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(180)};
        timer.Tick+=(s,e)=>{timer.Stop();frame.Continue=false;}; timer.Start(); Dispatcher.PushFrame(frame);
    }
    private static void Near(double expected,double actual,string name,double tolerance=.01)
    {if(Math.Abs(expected-actual)>tolerance)throw new Exception(name+": expected "+expected+", got "+actual);}
}
