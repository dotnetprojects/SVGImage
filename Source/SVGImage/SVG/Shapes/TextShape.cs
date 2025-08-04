using System.Linq;
using System.Xml;

namespace SVGImage.SVG.Shapes
{
    public class TextShape : TextShapeBase
    {
        public TextShape(SVG svg, XmlNode node, Shape parent) : base(svg, node, parent)
        {
            CollapseWhitespaceBetweenTextNodes(this);
        }
        private static bool EndsWithWhitespace(string text)
        {
            return !string.IsNullOrEmpty(text) && char.IsWhiteSpace(text[text.Length - 1]);
        }

        private static bool StartsWithWhitespace(string text)
        {
            return !string.IsNullOrEmpty(text) && char.IsWhiteSpace(text[0]);
        }
        private static TextString GetFirstLeaf(ITextNode node)
        {
            if (node is TextString leaf)
            {
                return leaf;
            }

            if (node is TextShapeBase container)
            {
                foreach (var child in container.Children)
                {
                    var result = GetFirstLeaf(child);
                    if (!(result is null))
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        private static TextString GetLastLeaf(ITextNode node)
        {
            if (node is TextString leaf)
            {
                return leaf;
            }

            if (node is TextShapeBase container)
            {
                for (int i = container.Children.Count - 1; i >= 0; i--)
                {
                    var result = GetLastLeaf(container.Children[i]);
                    if (!(result is null))
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        private static void CollapseWhitespaceBetweenTextNodes(TextShape root)
        {
            TextString previousLeaf = null;
            CollapseWhitespaceBetweenTextNodes(root, ref previousLeaf);
        }

        private static void CollapseWhitespaceBetweenTextNodes(ITextNode node, ref TextString previousLeaf)
        {
            if (node is TextString currentLeaf)
            {
                if (!(previousLeaf is null) &&
                    EndsWithWhitespace(previousLeaf.Text) &&
                    StartsWithWhitespace(currentLeaf.Text))
                {
                    currentLeaf.Characters = currentLeaf.Characters.Skip(1).ToArray();
                }

                previousLeaf = currentLeaf;
            }
            else if (node is TextShapeBase container)
            {
                foreach (var child in container.Children)
                {
                    CollapseWhitespaceBetweenTextNodes(child, ref previousLeaf);
                }
            }
        }

    }




}
