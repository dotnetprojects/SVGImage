using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace SVGImage.SVG
{
    public class ClipArtElement
    {
        public string Id { get; protected set; }

        public ClipArtElement(XmlNode node)
        {
            if (node == null)
                this.Id = "<null>";
            else
                this.Id = XmlUtil.AttrValue(node, "id");
        }
    }
}
