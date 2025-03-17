using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace E.Standard.WebGIS.Tools.Extensions;

internal static class StringExtensions
{
    private static readonly Regex InvalidScriptTagPattern = new Regex("<\\s*script\\s*>", RegexOptions.IgnoreCase);
    public const string InvalidProjekctNameChars = @":\'?";

    public static bool IsValidProjectName(this string input, out string invalidChars)
    {
        // Überprüfen, ob ein <script>-Tag (in jeglicher Schreibweise) im String vorkommt
        if (InvalidScriptTagPattern.IsMatch(input))
        {
            invalidChars = "<script>";
            return false;
        }

        if (input.IndexOfAny(InvalidProjekctNameChars.ToCharArray()) >= 0)
        {
            invalidChars = InvalidProjekctNameChars;
            return false;
        }

        invalidChars = "";
        return true;
    }

    public static readonly char[] InvalidFilenameChars = System.IO.Path.GetInvalidFileNameChars();

    public static bool IsValidFilename(this string input, out string invalidChars)
    {
        if (input.IndexOfAny(InvalidFilenameChars) >= 0)
        {
            invalidChars = String.Join(" ", InvalidFilenameChars.Where(c => IsPrintable(c)));
            return false;
        }

        invalidChars = "";
        return true;
    }

    private static bool IsPrintable(char c)
    {
        return c >= 0x20 && c != 0x7F; // Nicht berücksichtigt sind Steuerzeichen von 0x00 bis 0x1F und das Löschen-Zeichen 0x7F.
    }
}
