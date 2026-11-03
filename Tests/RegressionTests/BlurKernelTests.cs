using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Xml;
using SVGImage.SVG;

internal static class BlurKernelTests
{
    private static readonly Type FilterType=typeof(SVGRender).Assembly.GetType("SVGImage.SVG.Filters.RasterFilter",true);
    private static readonly Type FrameType=FilterType.GetNestedType("Frame",BindingFlags.NonPublic);
    private static readonly FieldInfo Data=FrameType.GetField("Data",BindingFlags.Instance|BindingFlags.NonPublic);
    private static object Frame(float[] data,bool linear)
    {
        var frame=Activator.CreateInstance(FrameType,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{data.Length,linear},null);
        Array.Copy(data,(float[])Data.GetValue(frame),data.Length);return frame;
    }
    internal static void Run()
    {
        var random=new Random(12345);
        foreach(var size in new[]{new[]{1,1},new[]{1,9},new[]{17,7},new[]{33,17},new[]{513,257}})
        foreach(string edge in new[]{"none","duplicate","wrap"})
        for(int mode=0;mode<4;mode++)
        {
            int width=size[0],height=size[1];
            var pixels=new float[width*height*4];
            for(int i=0;i<pixels.Length;i+=4)
            {
                float a=random.Next(256)/255f,gray=random.Next(256)/255f*a;
                pixels[i]=mode==1 ? 0 : mode==2 ? a : gray;
                pixels[i+1]=mode==0 ? random.Next(256)/255f*a : pixels[i];
                pixels[i+2]=mode==0 ? random.Next(256)/255f*a : pixels[i];pixels[i+3]=a;
            }
            var svg=new SVGImage.SVG.SVG();svg.LoadXml("<svg xmlns='http://www.w3.org/2000/svg'/>");
            var doc=new XmlDocument();doc.LoadXml("<filter filterUnits='userSpaceOnUse' x='0' y='0' width='"+width+"' height='"+height+"'/>");
            var filter=Activator.CreateInstance(FilterType,BindingFlags.Instance|BindingFlags.NonPublic,null,
                new object[]{svg,doc.DocumentElement,new Rect(0,0,width,height),new Rect(0,0,width,height),new SVGRender {RasterizationScale=1}},null);
            double sx=width==1 ? 12 : 1.2,sy=height==1 ? 20 : 2.3;
            var input=Frame(pixels,false);
            var output=FilterType.GetMethod("BlurAxes",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(filter,new[]{input,(object)sx,sy,edge,false});
            var expected=Reference(pixels,width,height,sx,sy,edge);
            if(!expected.SequenceEqual((float[])Data.GetValue(output)))
                throw new Exception("Blur differs from direct box convolution: "+width+"x"+height+" "+edge+" mode="+mode);
            if(!pixels.SequenceEqual((float[])Data.GetValue(input))) throw new Exception("Shared blur input was mutated");
            var donated=Frame(pixels,false);
            output=FilterType.GetMethod("BlurAxes",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(filter,new[]{donated,(object)sx,sy,edge,true});
            if(!expected.SequenceEqual((float[])Data.GetValue(output))) throw new Exception("Donated blur scratch differs");
        }
        foreach(int count in new[]{64,131584})
        foreach(bool linear in new[]{false,true})
        {
            var pixels=new float[count*4];
            for(int i=0;i<pixels.Length;i+=4)
            {
                float a=random.Next(256)/255f;
                for(int c=0;c<3;c++) pixels[i+c]=random.Next(256)/255f*a;
                if(i%12==0) pixels[i+1]=pixels[i+2]=pixels[i];
                pixels[i+3]=a;
            }
            var actual=new byte[pixels.Length];
            FilterType.GetMethod("EncodeFrame",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new[]{Frame(pixels,linear),(object)actual});
            for(int i=0;i<pixels.Length;i+=4)
            {
                double a=pixels[i+3];
                for(int c=0;c<3;c++)
                {
                    double v=pixels[i+2-c];
                    if(linear)
                    {
                        v=a>0 ? v/a : 0;
                        v=v<=.0031308 ? v*12.92 : 1.055*Math.Pow(v,1/2.4)-.055;
                        v=(float)(a*v);
                    }
                    if(actual[i+c]!=Byte(v)) throw new Exception("Fused encoding changed intermediate float rounding");
                }
                if(actual[i+3]!=Byte(a)) throw new Exception("Encoding changed alpha");
            }
        }
        Console.WriteLine("Blur kernels, edge modes, shared/donated buffers and exact output encoding passed.");
    }
    private static byte Byte(double value)=>(byte)Math.Round(255*Math.Max(0,Math.Min(1,value)));
    private static float[] Reference(float[] pixels,int width,int height,double sigmaX,double sigmaY,string edge)
    {
        var result=(float[])pixels.Clone();
        for(int axis=0;axis<2;axis++)
        {
            double sigma=axis==0 ? sigmaX : sigmaY;
            if(sigma<=0)continue;
            int lower=(int)Math.Min(1000000,Math.Floor(Math.Sqrt(4*sigma*sigma+1)));
            if(lower%2==0)lower--;lower=Math.Max(1,lower);
            int small=(int)Math.Round((12*sigma*sigma-3*lower*lower-12*lower-9)/(-4*lower-4));
            for(int pass=0;pass<3;pass++)
            {
                int radius=(pass<small ? lower : lower+2)/2;
                var output=new float[result.Length];
                // Deliberately direct convolution: no sliding sums, transposes,
                // channel specialization, parallel loops or scratch-buffer reuse.
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)for(int c=0;c<4;c++)
                {
                    double sum=0;
                    for(int k=-radius;k<=radius;k++)
                    {
                        int px=x+(axis==0 ? k : 0),py=y+(axis==1 ? k : 0);
                        if(edge=="duplicate") {px=Math.Max(0,Math.Min(width-1,px));py=Math.Max(0,Math.Min(height-1,py));}
                        else if(edge=="wrap") {px=(px%width+width)%width;py=(py%height+height)%height;}
                        if(px>=0 && px<width && py>=0 && py<height) sum+=result[(py*width+px)*4+c];
                    }
                    output[(y*width+x)*4+c]=(float)(sum/(2.0*radius+1));
                }
                result=output;
            }
        }
        return result;
    }
}
