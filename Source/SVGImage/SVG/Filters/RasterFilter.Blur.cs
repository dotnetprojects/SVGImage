using System;
using System.Threading.Tasks;

namespace SVGImage.SVG.Filters
{
    internal sealed partial class RasterFilter
    {
        private Frame BlurAxes(Frame input, double sigmaX, double sigmaY, string edge,bool disposableInput=false)
        {
            Frame result=input, first=disposableInput ? input : null, second=null;
            int channels=BlurChannels(input.Data);
            for(int axis=0;axis<2;axis++)
            {
                double sigma=axis==0 ? sigmaX : sigmaY;
                if(sigma<=0) continue;
                double ideal=Math.Sqrt(4*sigma*sigma+1);
                int lower=(int)Math.Min(1000000,Math.Floor(ideal));
                if(lower%2==0) lower--;
                lower=Math.Max(1,lower);
                int upper=lower+2;
                int small=(int)Math.Round((12*sigma*sigma-3*lower*lower-12*lower-9)/(-4*lower-4));
                bool transposed=false;
                for(int pass=0;pass<3;pass++)
                {
                    int radius=(pass<small ? lower : upper)/2;
                    if(radius==0) continue;
                    if(axis==1 && !transposed)
                    {
                        var target=BlurScratch(result,ref first,ref second);
                        Transpose(result,target,width,height);
                        result=target; transposed=true;
                    }
                    var output=BlurScratch(result,ref first,ref second);
                    BoxBlurRows(result,output,radius,axis==0 ? width : height,axis==0 ? height : width,edge,channels);
                    result=output;
                }
                if(transposed)
                {
                    var target=BlurScratch(result,ref first,ref second);
                    Transpose(result,target,height,width);
                    result=target;
                }
            }
            if(!disposableInput && ReferenceEquals(result,input))
            { result=new Frame(input.Data.Length,input.Linear); Array.Copy(input.Data,result.Data,input.Data.Length); }
            return result;
        }
        private static Frame BlurScratch(Frame current,ref Frame first,ref Frame second)
        {
            // Only a sole SourceGraphic blur can donate its temporary input.
            // Multi-primitive graphs always receive private scratch buffers.
            if(ReferenceEquals(current,first)) return second ?? (second=new Frame(current.Data.Length,current.Linear));
            return first ?? (first=new Frame(current.Data.Length,current.Linear));
        }
#if NETNEXT
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
#endif
        private static void Transpose(Frame input,Frame output,int columns,int rows)
        {
            const int tile=16;
            for(int top=0;top<rows;top+=tile)
            for(int left=0;left<columns;left+=tile)
            {
                int bottom=Math.Min(rows,top+tile), right=Math.Min(columns,left+tile);
                for(int y=top;y<bottom;y++) for(int x=left;x<right;x++)
                {
                    int source=(y*columns+x)*4,target=(x*rows+y)*4;
                    output.Data[target]=input.Data[source]; output.Data[target+1]=input.Data[source+1];
                    output.Data[target+2]=input.Data[source+2]; output.Data[target+3]=input.Data[source+3];
                }
            }
        }
        private static void BoxBlurRows(Frame input,Frame output,int radius,int length,int lines,string edge,int channels)
        {
            bool duplicate=edge=="duplicate",wrap=edge=="wrap";
            double divisor=2.0*radius+1;
            RunNumericRows(lines,input.Data.Length/4,line =>
            {
                if(channels==0) BlurRow(input.Data,output.Data,line*length*4,length,radius,duplicate,wrap,divisor);
                else BlurGrayRow(input.Data,output.Data,line*length*4,length,radius,duplicate,wrap,divisor,channels);
            });
        }
#if NETNEXT
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
#endif
        private static int BlurChannels(float[] data)
        {
            bool black=true,white=true;
            for(int i=0;i<data.Length;i+=4)
            {
                float r=data[i];
                if(r!=data[i+1] || r!=data[i+2]) return 0;
                if(r!=0) black=false;
                if(r!=data[i+3]) white=false;
            }
            return black ? 1 : white ? 2 : 3;
        }
#if NETNEXT
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
#endif
        private static void BlurGrayRow(float[] input,float[] output,int start,int length,int radius,bool duplicate,bool wrap,double divisor,int channels)
        {
            // Black and white height maps require only alpha; other gray maps
            // require gray and alpha. Equal channels have identical recurrences.
            bool gray=channels==3;
            double value=0,alpha=0;
            int initial=wrap ? length : Math.Min(length,radius+1);
            for(int at=0;at<initial;at++)
            {
                int copies=1;
                if(duplicate)
                {
                    if(at==0) copies+=radius;
                    if(at==length-1) copies+=Math.Max(0,radius-length+1);
                }
                else if(wrap)
                {
                    int window=2*radius+1,first=((-radius%length)+length)%length;
                    copies=window/length+(((at-first+length)%length)<window%length ? 1 : 0);
                }
                int index=start+at*4;
                if(gray) value+=(double)input[index]*copies;
                alpha+=(double)input[index+3]*copies;
            }
            for(int at=0;at<length;at++)
            {
                int index=start+at*4;
                float a=(float)(alpha/divisor),v=gray ? (float)(value/divisor) : channels==2 ? a : 0;
                output[index]=v;output[index+1]=v;output[index+2]=v;output[index+3]=a;
                int entering=at+radius+1,leaving=at-radius;
                if(duplicate) { entering=Math.Min(length-1,entering);leaving=Math.Max(0,leaving); }
                else if(wrap) { entering%=length;leaving=(leaving%length+length)%length; }
                if(entering>=0 && entering<length)
                { int i=start+entering*4;if(gray) value+=input[i];alpha+=input[i+3]; }
                if(leaving>=0 && leaving<length)
                { int i=start+leaving*4;if(gray) value-=input[i];alpha-=input[i+3]; }
            }
        }
        private static readonly ParallelOptions NumericWorkers=new ParallelOptions
        { MaxDegreeOfParallelism=Math.Min(4,Environment.ProcessorCount) };
        private static void RunNumericRows(int rows,int pixels,Action<int> action)
        {
            // Keep small filters serial. Worker callbacks touch numeric buffers
            // only; WPF rasterization and object creation stay on the caller STA.
            if(pixels<131072 || NumericWorkers.MaxDegreeOfParallelism==1)
                for(int row=0;row<rows;row++) action(row);
            else Parallel.For(0,rows,NumericWorkers,action);
        }
#if NETNEXT
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
#endif
        private static void BlurRow(float[] input,float[] output,int start,int length,int radius,bool duplicate,bool wrap,double divisor)
        {
            double r=0,g=0,b=0,a=0;
            int initial=wrap ? length : Math.Min(length,radius+1);
            for(int at=0;at<initial;at++)
            {
                int copies=1;
                if(duplicate)
                {
                    if(at==0) copies+=radius;
                    if(at==length-1) copies+=Math.Max(0,radius-length+1);
                }
                else if(wrap)
                {
                    int window=2*radius+1,first=((-radius%length)+length)%length;
                    copies=window/length+(((at-first+length)%length)<window%length ? 1 : 0);
                }
                int index=start+at*4;
                r+=(double)input[index]*copies; g+=(double)input[index+1]*copies;
                b+=(double)input[index+2]*copies; a+=(double)input[index+3]*copies;
            }
            for(int at=0;at<length;at++)
            {
                int index=start+at*4;
                output[index]=(float)(r/divisor); output[index+1]=(float)(g/divisor);
                output[index+2]=(float)(b/divisor); output[index+3]=(float)(a/divisor);
                int entering=at+radius+1,leaving=at-radius;
                if(duplicate) { entering=Math.Min(length-1,entering); leaving=Math.Max(0,leaving); }
                else if(wrap) { entering%=length; leaving=(leaving%length+length)%length; }
                if(entering>=0 && entering<length)
                {
                    int i=start+entering*4;
                    r+=input[i]; g+=input[i+1]; b+=input[i+2]; a+=input[i+3];
                }
                if(leaving>=0 && leaving<length)
                {
                    int i=start+leaving*4;
                    r-=input[i]; g-=input[i+1]; b-=input[i+2]; a-=input[i+3];
                }
            }
        }
    }
}
