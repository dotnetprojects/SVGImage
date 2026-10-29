using System;
using System.Xml;
using System.Windows.Media;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG.Filters
{
    internal sealed partial class RasterFilter
    {
        private struct Vector3
        {
            internal double X, Y, Z;
            internal Vector3(double x, double y, double z) { X=x; Y=y; Z=z; }
            internal Vector3 Unit()
            {
                double length=Math.Sqrt(X*X+Y*Y+Z*Z);
                return length>0 ? new Vector3(X/length,Y/length,Z/length) : new Vector3();
            }
            internal double Dot(Vector3 other) => X*other.X+Y*other.Y+Z*other.Z;
        }
        private void Lighting(XmlNode node, Frame input, Frame output, bool specular)
        {
            XmlNode light=null;
            foreach(XmlNode child in node.ChildNodes)
                if(child.LocalName=="feDistantLight" || child.LocalName=="fePointLight" || child.LocalName=="feSpotLight") { light=child; break; }
            if(light==null) return;
            double exponent=Number(node,"specularExponent",1);
            if(specular && (exponent<1 || exponent>128)) return;
            string value=Style(node,"lighting-color","white",false);
            if(value=="currentColor") value=Style(node,"color","black",true);
            Color color; if(!CssColor.TryParse(value,out color)) color=Colors.White;
            // lighting-color's alpha does not participate in either light model.
            double cr=color.R/255.0, cg=color.G/255.0, cb=color.B/255.0;
            if(output.Linear) { cr=ConvertColor(cr,true); cg=ConvertColor(cg,true); cb=ConvertColor(cb,true); }
            double pixelScale=Math.Sqrt((sx*sx+sy*sy)/2);
            double unitZ=Math.Sqrt((unitX*unitX+unitY*unitY)/2)*pixelScale;
            double surface=Number(node,"surfaceScale",1);
            double constant=Math.Max(0,Number(node,specular?"specularConstant":"diffuseConstant",1));
            var kernel=SvgNumberReader.List(Attr(node,"kernelUnitLength"));
            double stepX=kernel.Length>0 && kernel[0]>0 ? kernel[0]*unitX*sx : 1;
            double stepY=kernel.Length>1 && kernel[1]>0 ? kernel[1]*unitY*sy : kernel.Length==1 ? kernel[0]*unitY*sy : 1;
            if(stepY<=0) stepY=1;
            bool relative=Attr(filter,"primitiveUnits")=="objectBoundingBox";
            double originX=relative?bounds.X:0, originY=relative?bounds.Y:0;
            var position=new Vector3((originX+Number(light,"x")*unitX-region.X)*sx,(originY+Number(light,"y")*unitY-region.Y)*sy,Number(light,"z")*unitZ);
            var direction=new Vector3((originX+Number(light,"pointsAtX")*unitX-region.X)*sx-position.X,(originY+Number(light,"pointsAtY")*unitY-region.Y)*sy-position.Y,Number(light,"pointsAtZ")*unitZ-position.Z).Unit();
            double azimuth=Number(light,"azimuth")*Math.PI/180, elevation=Number(light,"elevation")*Math.PI/180;
            var distant=new Vector3(Math.Cos(azimuth)*Math.Cos(elevation),Math.Sin(azimuth)*Math.Cos(elevation),Math.Sin(elevation));
            double spotExponent=Number(light,"specularExponent",1); if(spotExponent<0) spotExponent=1;
            bool cone=light.Attributes["limitingConeAngle"]!=null;
            double coneCos=Math.Cos(Number(light,"limitingConeAngle")*Math.PI/180);
            for(int y=0;y<height;y++) for(int x=0;x<width;x++)
            {
                int at=(y*width+x)*4;
                var normal=SurfaceNormal(input,x,y,stepX,stepY,surface);
                // Light positions and the alpha height are evaluated on the filter pixel grid.
                double wx=x, wy=y;
                var l=light.LocalName=="feDistantLight" ? distant : new Vector3(position.X-wx,position.Y-wy,position.Z-surface*input.Data[at+3]).Unit();
                double intensity=1;
                if(light.LocalName=="feSpotLight")
                {
                    double cosine=-l.Dot(direction);
                    intensity=cosine<=0 || cone && cosine<coneCos ? 0 : Math.Pow(cosine,spotExponent);
                }
                double amount;
                if(specular)
                {
                    var half=new Vector3(l.X,l.Y,l.Z+1).Unit();
                    amount=constant*Math.Pow(Math.Max(0,normal.Dot(half)),exponent)*intensity;
                }
                else amount=constant*Math.Max(0,normal.Dot(l))*intensity;
                double r=Clamp(cr*amount),g=Clamp(cg*amount),b=Clamp(cb*amount);
                output.Data[at]=(float)r; output.Data[at+1]=(float)g; output.Data[at+2]=(float)b;
                output.Data[at+3]=(float)(specular?Math.Max(r,Math.Max(g,b)):1);
            }
        }
        // Sobel's nine edge/corner cases share a weighted difference formula.
        // One-sided derivatives receive twice the centered-difference factor.
        private Vector3 SurfaceNormal(Frame image, int x, int y, double dx, double dy, double surface)
        {
            bool left=x-dx>=0,right=x+dx<width,top=y-dy>=0,bottom=y+dy<height;
            double gx=0,gy=0,rows=0,columns=0;
            for(int row=-1;row<=1;row++)
            {
                if(row<0&&!top || row>0&&!bottom) continue;
                double weight=row==0?2:1; rows+=weight;
                gx+=weight*(Sample(image,x+(right?dx:0),y+row*dy,3)-Sample(image,x-(left?dx:0),y+row*dy,3));
            }
            for(int column=-1;column<=1;column++)
            {
                if(column<0&&!left || column>0&&!right) continue;
                double weight=column==0?2:1; columns+=weight;
                gy+=weight*(Sample(image,x+column*dx,y+(bottom?dy:0),3)-Sample(image,x+column*dx,y-(top?dy:0),3));
            }
            return new Vector3(-surface*gx*(left&&right?1:2)/(rows*dx),-surface*gy*(top&&bottom?1:2)/(columns*dy),1).Unit();
        }
    }
}
