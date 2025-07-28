using System;
using System.Collections.Generic;

namespace SVGImage.SVG.Shapes
{
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Collections;

    public class LengthPercentageOrNumberList : IList<LengthPercentageOrNumber>
    {
        private readonly Shape _owner;
        private List<LengthPercentageOrNumber> _list = new List<LengthPercentageOrNumber>();
        private readonly LengthOrientation _orientation;
        private static readonly Regex _splitRegex = new Regex(@"\b(?:,|\s*,?\s+)\b", RegexOptions.Compiled);
        private LengthPercentageOrNumberList(Shape owner, LengthOrientation orientation = LengthOrientation.None)
        {
            _owner = owner;
            _orientation = orientation;
        }
        /// <summary>
        /// Creates a new instance of LengthPercentageOrNumberList with the specified owner and orientation.
        /// </summary>
        /// <param name="owner">
        /// The element that the length list is associated with.
        /// </param>
        /// <param name="value">A string representation of a length list</param>
        /// <param name="orientation">Used to establish the context of the lengths.
        /// Should be <see cref="LengthOrientation.Horizontal"/> for inherntly horizontal values like 'x' and 'dx'.
        /// Should be <see cref="LengthOrientation.Vertical"/> for inherntly vertical values like 'y' and 'dy'.
        /// Should be <see cref="LengthOrientation.None"/> for other values.
        /// </param>
        public LengthPercentageOrNumberList(Shape owner, string value, LengthOrientation orientation = LengthOrientation.None) : this(owner, orientation)
        {
            Parse(value);
        }
        private void Parse(string value)
        {
            string[] list = _splitRegex.Split(value.Trim());

            if (list.Any(string.IsNullOrEmpty))
            {
                throw new ArgumentException("Invalid length/percentage/number list: " + value);
            }
            _list = list.Select(s=>LengthPercentageOrNumber.Parse(_owner, s, _orientation)).ToList();
        }

        public static LengthPercentageOrNumberList Empty(Shape owner, LengthOrientation orientation = LengthOrientation.None)
        {
            return new LengthPercentageOrNumberList(owner, orientation);
        }


        public LengthPercentageOrNumber this[int index] { get => ((IList<LengthPercentageOrNumber>)_list)[index]; set => ((IList<LengthPercentageOrNumber>)_list)[index] = value; }

        public int Count => ((ICollection<LengthPercentageOrNumber>)_list).Count;

        public bool IsReadOnly => ((ICollection<LengthPercentageOrNumber>)_list).IsReadOnly;

        public void Add(LengthPercentageOrNumber item)
        {
            //Remove units because Child elements do not inherit the relative values as specified for their parent; they inherit the computed values.
            var strippedContext = new LengthPercentageOrNumber(item.Value, new LengthContext(_owner, LengthUnit.Number));
            ((ICollection<LengthPercentageOrNumber>)_list).Add(strippedContext);
        }

        public void Clear()
        {
            ((ICollection<LengthPercentageOrNumber>)_list).Clear();
        }

        public bool Contains(LengthPercentageOrNumber item)
        {
            return ((ICollection<LengthPercentageOrNumber>)_list).Contains(item);
        }

        public void CopyTo(LengthPercentageOrNumber[] array, int arrayIndex)
        {
            ((ICollection<LengthPercentageOrNumber>)_list).CopyTo(array, arrayIndex);
        }

        public IEnumerator<LengthPercentageOrNumber> GetEnumerator()
        {
            return ((IEnumerable<LengthPercentageOrNumber>)_list).GetEnumerator();
        }

        public int IndexOf(LengthPercentageOrNumber item)
        {
            return ((IList<LengthPercentageOrNumber>)_list).IndexOf(item);
        }

        public void Insert(int index, LengthPercentageOrNumber item)
        {
            //Remove units because Child elements do not inherit the relative values as specified for their parent; they inherit the computed values.
            var strippedContext = new LengthPercentageOrNumber(item.Value, new LengthContext(_owner, LengthUnit.Number));
            ((ICollection<LengthPercentageOrNumber>)_list).Add(strippedContext);
            ((IList<LengthPercentageOrNumber>)_list).Insert(index, strippedContext);
        }

        public bool Remove(LengthPercentageOrNumber item)
        {
            return ((ICollection<LengthPercentageOrNumber>)_list).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<LengthPercentageOrNumber>)_list).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_list).GetEnumerator();
        }
    }




}
