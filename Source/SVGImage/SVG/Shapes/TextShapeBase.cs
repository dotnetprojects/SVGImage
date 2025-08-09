using System;
using System.Collections.Generic;
using System.Xml;

namespace SVGImage.SVG.Shapes
{
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    [DebuggerDisplay("{DebugDisplayText}")]
    public class TextShapeBase: Shape, ITextNode
    {
        protected TextShapeBase(SVG svg, XmlNode node, Shape parent) : base(svg, node, parent)
        {
        }

        private string DebugDisplayText => GetDebugDisplayText(new StringBuilder());
        private string GetDebugDisplayText(StringBuilder sb)
        {
            if (Children.Count == 0)
            {
                return "<Empty>";
            }
            foreach(var child in Children)
            {
                if (child is TextString textString)
                {
                    sb.Append(textString.Text);
                }
                else if (child is TextSpan textSpan)
                {
                    sb.Append('(');
                    textSpan.GetDebugDisplayText(sb);
                    sb.Append(')');
                }
            }

            return sb.ToString();
        }


        protected override Fill DefaultFill()
        {
            return Fill.CreateDefault(Svg, "black");
        }
        protected override Stroke DefaultStroke()
        {
            return Stroke.CreateDefault(Svg, 0.1);
        }

        public LengthPercentageOrNumberList X { get; protected set; }
        public LengthPercentageOrNumberList Y { get; protected set; } 
        public LengthPercentageOrNumberList DX { get; protected set; }
        public LengthPercentageOrNumberList DY { get; protected set; }
        public WritingMode WritingMode { get; set; }
        public List<StyleItem> Attributes { get; set; } = new List<StyleItem>();
        public List<double> Rotate { get; protected set; } = new List<double>();
        public LengthPercentageOrNumber? TextLength { get; set; }
        public LengthAdjustment LengthAdjust { get; set; } = LengthAdjustment.Spacing;
        public List<ITextChild> Children { get; } = new List<ITextChild>();
        public CharacterLayout FirstCharacter => GetFirstCharacter();
        public CharacterLayout LastCharacter => GetLastCharacter();
        public string Text => GetText();
        public int Length => GetLength();



        public CharacterLayout[] GetCharacters()
        {
            return Children.SelectMany(c => c.GetCharacters()).ToArray();
        }

        public CharacterLayout GetFirstCharacter()
        {
            foreach(var child in Children)
            {
                if (child.GetFirstCharacter() is CharacterLayout firstChar)
                {
                    return firstChar;
                }
            }
            throw new InvalidOperationException("No characters found in text node.");
        }
        public CharacterLayout GetLastCharacter()
        {
            for (int i = Children.Count - 1; i >= 0; i--)
            {
                if (Children[i].GetLastCharacter() is CharacterLayout LastChar)
                {
                    return LastChar;
                }
            }
            throw new InvalidOperationException("No characters found in text node.");
        }

        public int GetLength()
        {
            return Children.Sum(c => c.GetLength());
        }

        public string GetText()
        {
            return string.Concat(Children.Select(c => c.GetText()));
        }

        protected override void ParseAtStart(SVG svg, XmlNode node)
        {
            base.ParseAtStart(svg, node);

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name)
                {
                    case "x":
                        X = new LengthPercentageOrNumberList(this, attr.Value, LengthOrientation.Horizontal);
                        break;
                    case "y":
                        Y = new LengthPercentageOrNumberList(this, attr.Value, LengthOrientation.Vertical);
                        break;
                    case "dx":
                        DX = new LengthPercentageOrNumberList(this, attr.Value, LengthOrientation.Horizontal);
                        break;
                    case "dy":
                        DY = new LengthPercentageOrNumberList(this, attr.Value, LengthOrientation.Vertical);
                        break;
                    case "rotate":
                        Rotate = attr.Value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(v => double.Parse(v, NumberStyles.Number, CultureInfo.InvariantCulture)).ToList();
                        break;
                    case "textLength":
                        TextLength = LengthPercentageOrNumber.Parse(this, attr.Value);
                        break;
                    case "lengthAdjust":
                        LengthAdjust = Enum.TryParse(attr.Value, true, out LengthAdjustment adj) ? adj : LengthAdjustment.Spacing;
                        break;
                    case "writing-mode":
                        switch (attr.Value)
                        {
                            case "horizontal-tb":
                            case "lr":
                            case "lr-tb":
                            case "rl":
                            case "rl-tb":
                                WritingMode = WritingMode.HorizontalTopToBottom;
                                break;
                                case "vertical-rl":
                            case "tb":
                            case "tb-rl":
                                WritingMode = WritingMode.VerticalRightToLeft;
                                break;
                            case "vertical-lr":
                                WritingMode = WritingMode.VerticalLeftToRight;
                                break;
                            default:
                                Debug.WriteLine($"Unknown writing-mode: {attr.Value}");
                                break;
                        }
                        break;
                    default:
                        Attributes.Add(new StyleItem(attr.Name, attr.Value));
                        break;
                }
            }
            if(X is null)
            {
                X = LengthPercentageOrNumberList.Empty(this, LengthOrientation.Horizontal);
            }
            if (Y is null)
            {
                Y = LengthPercentageOrNumberList.Empty(this, LengthOrientation.Vertical);
            }
            if (DX is null)
            {
                DX = LengthPercentageOrNumberList.Empty(this, LengthOrientation.Horizontal);
            }
            if (DY is null)
            {
                DY = LengthPercentageOrNumberList.Empty(this, LengthOrientation.Vertical);
            }

            ParseChildren(svg, node);
        }
        private static readonly Regex _trimmedWhitespace = new Regex(@"\s+", RegexOptions.Compiled | RegexOptions.Singleline);

        protected void ParseChildren(SVG svg, XmlNode node)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Text || child.NodeType == XmlNodeType.CDATA)
                {
                    var text = _trimmedWhitespace.Replace(child.InnerText, " ");
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        Children.Add(new TextString(this, text));
                    }
                }
                else if (child.NodeType == XmlNodeType.Element && child.Name == "tspan")
                {
                    var span = new TextSpan(svg, child, this);
                    Children.Add(span);
                }
                // Future support for <textPath>, <tref>, etc. could go here
            }
        }

    }




}
