using System;

namespace SVGImage.SVG.Shapes
{
    /// <summary>
    /// Represents a per-character layout result.
    /// </summary>
    public class CharacterLayout
    {
        private CharacterLayout()
        {
            // Default constructor for array creation
        }
        public CharacterLayout(char character)
        {
            Character = character;
        }
        public char Character { get; set; } = '\0';
        public int GlobalIndex { get; set; }
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double DX { get; set; } = Double.NaN;
        public double DY { get; set; } = Double.NaN;
        public double Rotation { get; set; } = Double.NaN;
        public bool Hidden { get; set; } = false;
        public bool Addressable { get; set; } = true;
        public bool Middle { get; set; } = false;
        public bool AnchoredChunk { get; set; } = false;
        /// <summary>
        /// Not used, part of the SVG 2.0 spec.
        /// </summary>
        internal bool FirstCharacterInResolvedDescendant { get; set; }
        /// <summary>
        /// The character redefines the X position for anteceding characters.
        /// </summary>
        public bool DoesPositionX { get; internal set; }
        /// <summary>
        /// The character redefines the Y position for anteceding characters.
        /// </summary>
        public bool DoesPositionY { get; internal set; }


    }




}
