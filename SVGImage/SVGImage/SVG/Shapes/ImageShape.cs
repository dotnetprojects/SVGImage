using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

namespace SVGImage.SVG.Shapes
{
    public class ImageShape : Shape
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public ImageSource ImageSource { get; }

        public ImageShape(SVG svg, XmlNode node) : base(svg, node)
        {
            this.X = XmlUtil.AttrValue(node, "x", 0, svg.Size.Width);
            this.Y = XmlUtil.AttrValue(node, "y", 0, svg.Size.Height);
            this.Width = XmlUtil.AttrValue(node, "width", 0, svg.Size.Width);
            this.Height = XmlUtil.AttrValue(node, "height", 0, svg.Size.Height);
            string hRef = XmlUtil.AttrValue(node, "xlink:href", string.Empty);
            if (hRef.Length > 0)
            {
                try
                {
                    BitmapImage b = new BitmapImage();
                    b.BeginInit();
                    if (hRef.StartsWith("data:image/png;base64"))
                    {
                        b.StreamSource = new MemoryStream(Convert.FromBase64String(hRef.Substring("data:image/png;base64,".Length)));
                    }
                    else
                    {
                        if (svg.ExternalFileLoader != null)
                            b.StreamSource = svg.ExternalFileLoader.LoadFile(hRef, svg.Filename);
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
