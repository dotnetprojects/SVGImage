using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SVGImage.SVG.FileLoaders;
using SVGImage.SVG.Shapes;
using SVGImage.SVG.Utils;

namespace SVGImage.SVG
{
    public partial class SVGRender
    {
        private HashSet<XmlNode> activeFilterImages = new HashSet<XmlNode>();
        private HashSet<string> activeImageResources = new HashSet<string>(StringComparer.Ordinal);

        internal Drawing RenderFilterImage(XmlNode node, Rect target, double unitX, double unitY)
        {
            if(activeFilterImages.Count>=32 || !activeFilterImages.Add(node)) return new DrawingGroup();
            string resourceKey=null;
            try
            {
                string href=XmlUtil.AttrValue(node,"href",node.Attributes["href",SVGTags.sNsXLink]?.Value ?? "");
                if(href.Length==0) return new DrawingGroup();
                if(href.StartsWith("#"))
                {
                    var targetShape=SVG.GetShape(href.Substring(1));
                    if(targetShape==null || targetShape is Filters.Filter || targetShape is Clip || targetShape is Mask) return new DrawingGroup();
                    var use=node.OwnerDocument.CreateElement("use",SVGTags.sNsSvg);
                    use.SetAttribute("href",href);
                    use.SetAttribute("width",(target.Width/unitX).ToString(CultureInfo.InvariantCulture));
                    use.SetAttribute("height",(target.Height/unitY).ToString(CultureInfo.InvariantCulture));
                    var drawing=CreateDrawing(new UseShape(SVG,use));
                    return new DrawingGroup { Children={drawing}, Transform=new MatrixTransform(unitX,0,0,unitY,target.X,target.Y) };
                }
                bool data=href.StartsWith("data:",StringComparison.OrdinalIgnoreCase);
                string fragment="", location=href;
                if(!data)
                {
                    int hash=href.IndexOf('#');
                    if(hash>=0) { fragment=Uri.UnescapeDataString(href.Substring(hash+1)); location=href.Substring(0,hash); }
                    var resourceLoader=ExternalFileLoader as ResourceFileLoader;
                    resourceKey=resourceLoader!=null ? resourceLoader.Resolve(location).ToString() :
                        Uri.IsWellFormedUriString(location,UriKind.Absolute) ? location : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(SVG.Filename) ?? Environment.CurrentDirectory,location));
                    if(!activeImageResources.Add(resourceKey)) { resourceKey=null; return new DrawingGroup(); }
                }
                byte[] bytes; string filename="";
                using(var stream=data ? DataStream(href) : ExternalFileLoader?.LoadFile(location,SVG.Filename))
                {
                    if(stream==null) return new DrawingGroup();
                    if(stream is FileStream file) filename=file.Name;
                    using(var buffer=new MemoryStream()) { stream.CopyTo(buffer); bytes=buffer.ToArray(); }
                }
                bool svg=location.IndexOf("image/svg+xml",StringComparison.OrdinalIgnoreCase)>=0 ||
                    location.Split('?')[0].EndsWith(".svg",StringComparison.OrdinalIgnoreCase);
                if(!svg)
                {
                    string prefix=Encoding.UTF8.GetString(bytes,0,Math.Min(128,bytes.Length)).TrimStart('\ufeff',' ','\r','\n','\t');
                    svg=prefix.StartsWith("<");
                }
                Drawing content; Rect source;
                if(svg)
                {
                    var loader=ExternalFileLoader is ResourceFileLoader pack && !data ? pack.RelativeTo(location) : ExternalFileLoader;
                    var renderer=new SVGRender(loader) { UseFilters=UseFilters, RasterizationScale=RasterizationScale, activeFilterImages=activeFilterImages, activeImageResources=activeImageResources };
                    var document=new SVG(loader);
                    using(var stream=new MemoryStream(bytes)) document.LoadResource(stream,filename);
                    renderer.SVG=document;
                    content=fragment.Length==0 ? renderer.CreateDrawing(document) : document.GetShape(fragment) is Shape selected ? renderer.CreateDrawing(selected) : new DrawingGroup();
                    source=document.ViewBox ?? new Rect(document.Size);
                }
                else
                {
                    using(var stream=new MemoryStream(bytes))
                    {
                        var image=BitmapDecoder.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames[0]; image.Freeze();
                        source=new Rect(0,0,image.PixelWidth,image.PixelHeight); content=new ImageDrawing(image,source);
                    }
                }
                if(source.IsEmpty || source.Width<=0 || source.Height<=0) return new DrawingGroup();
                return new DrawingGroup { Children={content}, Transform=new MatrixTransform(FitImage(source,target,XmlUtil.AttrValue(node,"preserveAspectRatio","xMidYMid meet"))) };
            }
            catch(Exception ex) when(ex is IOException || ex is XmlException || ex is FormatException || ex is ArgumentException || ex is NotSupportedException || ex is InvalidOperationException)
            { Trace.TraceWarning("Could not load feImage: "+ex.Message); return new DrawingGroup(); }
            finally { activeFilterImages.Remove(node); if(resourceKey!=null) activeImageResources.Remove(resourceKey); }
        }
        private static Stream DataStream(string href)
        {
            int comma=href.IndexOf(','); if(comma<0) return null;
            string metadata=href.Substring(0,comma), payload=href.Substring(comma+1);
            return new MemoryStream(metadata.IndexOf(";base64",StringComparison.OrdinalIgnoreCase)>=0 ? Convert.FromBase64String(payload) : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload)));
        }
        private static Matrix FitImage(Rect source, Rect target, string aspect)
        {
            aspect=aspect.Trim(); if(aspect.StartsWith("defer ")) aspect=aspect.Substring(6).Trim();
            double x=target.Width/source.Width,y=target.Height/source.Height,dx=0,dy=0;
            if(aspect!="none")
            {
                x=y=aspect.Contains("slice")?Math.Max(x,y):Math.Min(x,y);
                dx=(target.Width-source.Width*x)*(aspect.Contains("xMin")?0:aspect.Contains("xMax")?1:.5);
                dy=(target.Height-source.Height*y)*(aspect.Contains("YMin")?0:aspect.Contains("YMax")?1:.5);
            }
            return new Matrix(x,0,0,y,target.X+dx-source.X*x,target.Y+dy-source.Y*y);
        }
    }
}
