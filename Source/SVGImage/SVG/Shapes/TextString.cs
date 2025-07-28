namespace SVGImage.SVG.Shapes
{
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Diagnostics;

    [DebuggerDisplay("{Text}")]
    /// <summary>
    /// Text not wrapped in a tspan element.
    /// </summary>
    public class TextString : ITextChild
    {
        public CharacterLayout[] Characters { get; set; }
        public Shape Parent { get; set; }
        public int Index { get; set; }
        private static readonly Regex _trimmedWhitespace = new Regex(@"\s+", RegexOptions.Compiled | RegexOptions.Singleline);
        public TextString(Shape parent, string text)
        {
            Parent = parent;
            string trimmed = _trimmedWhitespace.Replace(text.Trim(), " ");
            Characters = new CharacterLayout[trimmed.Length];
            for(int i = 0; i < trimmed.Length; i++)
            {
                var c = trimmed[i];
                Characters[i] = new CharacterLayout(c);
            }
        }
        public CharacterLayout GetFirstCharacter()
        {
            return Characters.FirstOrDefault();
        }
        public CharacterLayout GetLastCharacter()
        {
            return Characters.LastOrDefault();
        }
        public CharacterLayout FirstCharacter => GetFirstCharacter();
        public CharacterLayout LastCharacter => GetLastCharacter();
        public string Text => GetText();
        public int Length => GetLength();

        public TextStyle TextStyle { get; internal set; }

        public string GetText()
        {
            return new string(Characters.Select(c => c.Character).ToArray());
        }

        public int GetLength()
        {
            return Characters.Length;
        }

        public CharacterLayout[] GetCharacters()
        {
            return Characters;
        }
    }




}
