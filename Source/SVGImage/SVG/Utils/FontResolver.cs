using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace SVGImage.SVG.Utils
{

    /// <summary>
    /// A utility class that resolves font families based on requested font names.
    /// </summary>
    public class FontResolver
    {
        private readonly ConcurrentDictionary<string, FontFamily> _fontCache = new ConcurrentDictionary<string, FontFamily>();
        private readonly Dictionary<string, FontFamily> _availableFonts;
        private readonly Dictionary<string, string> _normalizedFontNameMap;

        /// <summary>
        /// A utility class that resolves font families based on requested font names.
        /// </summary>
        /// <param name="maxLevenshteinDistance">Maximum Levenshtein distance to consider a match. If set to ≤ 0, Levenshtein matching is disabled.</param>
        public FontResolver(int maxLevenshteinDistance = 0)
        {
            _availableFonts = Fonts.SystemFontFamilies
                .Select(ff => new { NormalName = ff.Source, Family = ff })
                .ToDictionary(x => x.NormalName, x => x.Family, StringComparer.OrdinalIgnoreCase);

            _normalizedFontNameMap = _availableFonts.Keys
                .ToDictionary(
                    name => Normalize(name),
                    name => name,
                    StringComparer.OrdinalIgnoreCase);
            MaxLevenshteinDistance = maxLevenshteinDistance;
        }
        /// <summary>
        /// Maximum Levenshtein distance to consider a match.
        /// If set to ≤ 0, Levenshtein matching is disabled.
        /// </summary>
        public int MaxLevenshteinDistance { get; set; } = 0;

        /// <summary>
        /// Attempts to a font family based on the requested font name.
        /// </summary>
        /// <param name="requestedFontName">The name of the font to resolve.</param>
        /// <returns>
        /// A <see cref="FontFamily"/> if a match is found, otherwise null.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the requested font name is null or empty.
        /// </exception>
        public FontFamily ResolveFontFamily(string requestedFontName)
        {
            if (string.IsNullOrWhiteSpace(requestedFontName))
            {
                throw new ArgumentException("Font name cannot be null or empty.", nameof(requestedFontName));
            }

            if (_fontCache.TryGetValue(requestedFontName, out var cachedFont))
            {
                return cachedFont;
            }

            // 1. Exact match
            if (_availableFonts.TryGetValue(requestedFontName, out var exactMatch))
            {
                _fontCache[requestedFontName] = exactMatch;
                return exactMatch;
            }

            // 2. Normalized match
            string normalizedRequested = Normalize(requestedFontName);
            if (_normalizedFontNameMap.TryGetValue(normalizedRequested, out var normalizedMatchName) &&
                _availableFonts.TryGetValue(normalizedMatchName, out var normalizedMatch))
            {
                _fontCache[requestedFontName] = normalizedMatch;
                return normalizedMatch;
            }

            // 3. Substring match
            var substringMatch = _availableFonts
                .FirstOrDefault(kv => Normalize(kv.Key).Contains(normalizedRequested));
            if (substringMatch.Value != null)
            {
                _fontCache[requestedFontName] = substringMatch.Value;
                return substringMatch.Value;
            }

            // 4. Levenshtein match (optional but slow)
            if ( MaxLevenshteinDistance > 0)
            {
                var bestMatch = _availableFonts
                .Select(kv => new
                {
                    FontName = kv.Key,
                    Font = kv.Value,
                    Distance = Levenshtein(normalizedRequested, Normalize(kv.Key))
                })
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

                if (bestMatch != null && bestMatch.Distance <= 4)
                {
                    _fontCache[requestedFontName] = bestMatch.Font;
                    return bestMatch.Font;
                }
            }

            

            // 5. No match
            _fontCache[requestedFontName] = null;
            return null;
        }

        /// <summary>
        /// Matches spaces, hyphens, and underscores
        /// </summary>
        private static readonly Regex _normalizationRegex = new Regex(@"[\s\-_]", RegexOptions.Compiled);

        /// <summary>
        /// Remove spaces, hyphens, underscores, and make lowercase
        /// </summary>
        /// <param name="fontName">The font name to normalize.</param>
        /// <returns>
        /// A normalized version of the font name, with spaces, hyphens, and underscores removed, and all characters in lowercase.
        /// </returns>
        private static string Normalize(string fontName)
        {
            if (fontName is null)
            {
                return string.Empty;
            }
            return _normalizationRegex.Replace(fontName, String.Empty).ToLowerInvariant();
            
        }

        private static int[,] CreateDistanceMatrix(int length1, int length2)
        {
            var matrix = new int[length1 + 1, length2 + 1];
            for (int i = 0; i <= length1; i++)
            {
                matrix[i, 0] = i;
            }
            for (int j = 0; j <= length2; j++)
            {
                matrix[0, j] = j;
            }
            return matrix;
        }

        /// <summary>
        /// Calculates the Levenshtein distance between two strings.
        /// The Levenshtein distance is a measure of the difference between two sequences.
        /// It is defined as the minimum number of single-character edits (insertions, deletions, or substitutions)
        /// </summary>
        /// <param name="string1">The first string to compare.</param>
        /// <param name="string2">The second string to compare.</param>
        /// <returns>
        /// The Levenshtein distance between the two strings.
        /// </returns>
        private static int Levenshtein(string string1, string string2)
        {
            if (string1 == string2)
            {
                return 0;
            }

            if (string.IsNullOrEmpty(string1))
            {
                return string2.Length;
            }

            if (string.IsNullOrEmpty(string2))
            {
                return string1.Length;
            }

            
            int[,] distanceMatrix = CreateDistanceMatrix(string1.Length, string2.Length);

            for (int i = 1; i <= string1.Length; i++)
            {
                for (int j = 1; j <= string2.Length; j++)
                {
                    int cost = string1[i - 1] == string2[j - 1] ? 0 : 1;

                    distanceMatrix[i, j] = Math.Min(
                        Math.Min(distanceMatrix[i - 1, j] + 1,  // deletion
                                 distanceMatrix[i, j - 1] + 1), // insertion
                        distanceMatrix[i - 1, j - 1] + cost);  // substitution
                }
            }

            return distanceMatrix[string1.Length, string2.Length];
        }

    }
    
}
