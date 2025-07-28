namespace SVGImage.SVG.Shapes
{
    public interface ITextNode
    {
        CharacterLayout GetFirstCharacter();
        CharacterLayout GetLastCharacter();
        string GetText();
        int GetLength();
        CharacterLayout[] GetCharacters();
    }




}
