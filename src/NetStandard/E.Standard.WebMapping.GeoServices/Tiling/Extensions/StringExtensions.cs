using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace E.Standard.WebMapping.GeoServices.Tiling.Extensions;

static internal partial class StringExtensions
{
    // Reuse one compiled regex instance for all calls
    //private static Regex TrailingIntegerRegex =
    //    new Regex(@"^(.*?)(-?\d+)$", RegexOptions.Compiled);

    // Generate the regex at compile time for better startup and runtime performance
    [GeneratedRegex(@"^(.*?)(-?\d+)$", RegexOptions.Compiled)]
    private static partial Regex TrailingIntegerRegex();

    extension(string input)
    {
        // 0,1,2 => 0,1,2 (Pattern "{0}")
        // EPSG:4326:11 => 11 (Pattern "EPSG:4326:{0}"
        public int MatrixSetIdentifierToIntWithPattern(out string pattern)
        {
            if (!input.Contains(":"))
            {
                pattern = "{0}";
                return int.Parse(input);
            }

            string[] parts = input.Split(':');
            pattern = $"{String.Join(":", parts.Take(parts.Length - 1))}:{{0}}";

            return int.Parse(parts.Last());
        }

        // Same with Regex (works with every pattern)
        public (int Value, string Pattern) MatrixSetIdentifierToIntWithPattern()
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input must not be null, empty, or whitespace.", nameof(input));

            // Match the last integer at the end of the string
            var match = TrailingIntegerRegex().Match(input);

            // Throw if the string does not end with an integer
            if (!match.Success)
                throw new FormatException($"Input does not end with a valid integer: {input}");

            // Parse the numeric part
            int value = int.Parse(match.Groups[2].Value);

            // Build the format pattern
            string prefix = match.Groups[1].Value;
            string pattern = string.IsNullOrEmpty(prefix) ? "{0}" : $"{prefix}{{0}}";

            return (value, pattern);
        }

        public int MatrixSetIdentifierToInt()
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input must not be null, empty, or whitespace.", nameof(input));

            // Match the last integer at the end of the string
            var match = TrailingIntegerRegex().Match(input);

            // Throw if the string does not end with an integer
            if (!match.Success)
                throw new FormatException($"Input does not end with a valid integer: {input}");

            // Parse the numeric part
            int value = int.Parse(match.Groups[2].Value);

            return value;
        }
    }

    extension(string[] stringArray)
    {
        public string[] ReplaceTileUrlConstants(
                string style,
                string tileMatrixSet,
                string tileMatrixPattern
            )
        {
            for (int i = 0; i < stringArray.Length; i++)
            {
                stringArray[i] = stringArray[i]
                    .Replace("{Style}", style, StringComparison.OrdinalIgnoreCase)
                    .Replace("{TileMatrixSet}", tileMatrixSet, StringComparison.OrdinalIgnoreCase)
                    .Replace("{TileMatrix}",
                        String.Format(tileMatrixPattern, "[LEVEL]"),
                        StringComparison.OrdinalIgnoreCase)
                    .Replace("{TileRow}", "[ROW]", StringComparison.OrdinalIgnoreCase)
                    .Replace("{TileCol}", "[COL]", StringComparison.OrdinalIgnoreCase);
            }

            return stringArray;
        }
    }
}
