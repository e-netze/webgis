using System;
using System.Text;

namespace E.Standard.Extensions.Text;

static public class StringEncodingExtensions
{
    private static bool RegisteredRegisterProvider = false;

    public static string ToLatin1UrlEncoded(this string input)
    {
        if (String.IsNullOrEmpty(input)) return "";

        if (!RegisteredRegisterProvider)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            RegisteredRegisterProvider = true;
        }

        Encoding latin1 = Encoding.GetEncoding("Windows-1252");
        byte[] bytes = latin1.GetBytes(input);

        var sb = new StringBuilder();

        foreach (byte b in bytes)
        {
            // URL-safes char => directly: a-z, A-Z, 0-9, -
            if ((b >= 0x30 && b <= 0x39) || // 0-9
                (b >= 0x41 && b <= 0x5A) || // A-Z
                (b >= 0x61 && b <= 0x7A) || // a-z
                b == 0x2D)                  // -)   
            {
                sb.Append((char)b);
            }
            else  // otherwise percent-encode   
            {
                sb.Append('%');
                sb.Append(b.ToString("x2")); // Kleinschreibung wie im Beispiel: %fc
            }
        }

        return sb.ToString();
    }
}
