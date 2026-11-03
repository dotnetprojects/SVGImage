using System;

namespace SVGImage.SVG.Filters
{
    internal sealed partial class RasterFilter
    {
        // These pure numeric kernels need optimized code on their first image,
        // rather than waiting for tiered-JIT promotion. Older targets retain
        // their usual JIT behavior; benchmark cold and warm paths together.
#if NETNEXT
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
#endif
        private static void DecodePixels(byte[] pixels,float[] output,int start,int end,bool linear)
        {
            for(int i=start;i<end;i+=4)
            {
                // Match the old byte -> float -> double conversion order.
                double a=pixels[i+3]/255f;
                output[i+3]=(float)a;
                if(linear && a==0) continue;
                float r=pixels[i+2]/255f,g=pixels[i+1]/255f,b=pixels[i]/255f;
                if(linear)
                {
                    if(r==g && r==b)
                    { r=(float)(a*ConvertColor(r/a,true));g=r;b=r; }
                    else
                    { r=(float)(a*ConvertColor(r/a,true));g=(float)(a*ConvertColor(g/a,true));b=(float)(a*ConvertColor(b/a,true)); }
                }
                output[i]=r;output[i+1]=g;output[i+2]=b;
            }
        }
#if NETNEXT
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
#endif
        private static void ConvertPixels(float[] input,float[] output,int start,int end,bool linear)
        {
            for(int i=start;i<end;i+=4)
            {
                double a=input[i+3]; output[i+3]=(float)a;
                if(!(a>0)) continue;
                if(input[i]==input[i+1] && input[i]==input[i+2])
                {
                    float gray=(float)(a*ConvertColor(input[i]/a,linear));
                    output[i]=gray; output[i+1]=gray; output[i+2]=gray;
                }
                else for(int c=0;c<3;c++) output[i+c]=(float)(a*ConvertColor(input[i+c]/a,linear));
            }
        }
#if NETNEXT
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
#endif
        private static void EncodePixels(float[] input,byte[] bytes,int start,int end,bool linear)
        {
            for(int i=start;i<end;i+=4)
            {
                double a=input[i+3];
                if(linear && (!(a>0) || input[i]==input[i+1] && input[i]==input[i+2]))
                {
                    byte gray=Byte((float)(a>0 ? a*ConvertColor(input[i]/a,false) : 0));
                    bytes[i]=gray; bytes[i+1]=gray; bytes[i+2]=gray;
                }
                else if(linear)
                {
                    // Preserve the old intermediate float rounding exactly,
                    // without allocating another full RGBA float frame.
                    bytes[i]=Byte((float)(a>0 ? a*ConvertColor(input[i+2]/a,false) : 0));
                    bytes[i+1]=Byte((float)(a>0 ? a*ConvertColor(input[i+1]/a,false) : 0));
                    bytes[i+2]=Byte((float)(a>0 ? a*ConvertColor(input[i]/a,false) : 0));
                }
                else
                {
                    bytes[i]=Byte(input[i+2]); bytes[i+1]=Byte(input[i+1]); bytes[i+2]=Byte(input[i]);
                }
                bytes[i+3]=Byte(a);
            }
        }
    }
}
