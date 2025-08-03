using System;
using System.Windows.Media;
using System.Windows.Markup;
using System.IO;
using System.Xml;

namespace SVGImage.SVG.Utils
{
    internal static class DrawingGroupSerializer
    {
        public static string SerializeToXaml(DrawingGroup drawingGroup)
        {
            if (drawingGroup is null)
            {
                throw new ArgumentNullException(nameof(drawingGroup));
            }

            // Freezing can help ensure serialization works without exceptions
            if (drawingGroup.CanFreeze && !drawingGroup.IsFrozen)
            {
                drawingGroup.Freeze();
            }

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                OmitXmlDeclaration = true
            };

            using (var stringWriter = new StringWriter())
            using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
            {
                XamlWriter.Save(drawingGroup, xmlWriter);
                return stringWriter.ToString();
            }
        }
    }
}
