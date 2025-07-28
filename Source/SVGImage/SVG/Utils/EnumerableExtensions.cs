using System;
using System.Collections.Generic;

namespace SVGImage.SVG.Utils
{
    internal static class EnumerableExtensions
    {
        public static int IndexOfFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            int i = 0;
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    return i;
                }
                i++;
            }
            return -1; // Not found
        }
    }




}
