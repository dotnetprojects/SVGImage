using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

namespace SVGImage.SVG.Shapes
{
    internal class ImageShape : Shape
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public ImageSource ImageSource { get; private set; }

        public ImageShape(SVG svg, XmlNode node)
            : base(svg, node)
        {
            this.X = XmlUtil.AttrValue(node, "x", 0);
            this.Y = XmlUtil.AttrValue(node, "y", 0);
            this.Width = XmlUtil.AttrValue(node, "width", 0);
            this.Height = XmlUtil.AttrValue(node, "height", 0);
            string hRef = XmlUtil.AttrValue(node, "xlink:href", string.Empty);
            if (hRef.Length > 0)
            {
                try
                {
                    BitmapImage b = new BitmapImage();
                    b.BeginInit();
                    if (hRef.StartsWith("data:image/png;base64"))
                    {
                        b.StreamSource =
                            new System.IO.MemoryStream(
                                Convert.FromBase64String(hRef.Substring("data:image/png;base64,".Length)));
                    }
                    else
                    {
                        // filename given must be relative to the location of the svg file
                        string svgpath = System.IO.Path.GetDirectoryName(svg.Filename);
                        string filename = System.IO.Path.Combine(svgpath, hRef);
                        if (File.Exists(filename))
                            b.UriSource = new Uri(filename, UriKind.RelativeOrAbsolute);
                    }

                    b.EndInit();
                    this.ImageSource = b;
                }
                catch(Exception ex)
                { }
            }
        }
    }
}
