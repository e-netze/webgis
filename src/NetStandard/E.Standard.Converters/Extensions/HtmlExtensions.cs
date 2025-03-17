using System;
using System.Text;
using System.Text.RegularExpressions;

namespace E.Standard.Converters.Extensions;

static public class HtmlExtensions
{
    static public string ToMarkdownString(this string? html, bool addPrefix = true)
    {
        if (html.IsHtml())
        {
            var converter = new ReverseMarkdown.Converter();
            StringBuilder markdown = new StringBuilder();

            if (addPrefix)
            {
                markdown.Append("md:");
            }

            markdown.Append(converter.Convert(html));

            return markdown.ToString();
        }

        return html ?? string.Empty;
    }

    static public string RemoveDoubleNewLines(this string? str)
        => (str ?? string.Empty).Replace("\r", "").Replace("\n\n", "\n");

    static public bool IsHtml(this string? html)
    {
        if (!String.IsNullOrEmpty(html))
        {
            string pattern = @"<(.|\n)*?>";

            if (Regex.IsMatch(html, pattern))
            {
                return true;
            }
        }

        return false;
    }
}
