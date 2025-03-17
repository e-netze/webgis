using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E.Standard.WebMapping.GeoServices.LuceneServer.Extentions;

static internal class StringExtensions
{
    static public string AppendCategories(this string query, IEnumerable<string> categories, string categoryField = "category")
    {
        if (categories == null || categories.Count() == 0)
        {
            return query;
        }

        StringBuilder sbQuery = new StringBuilder(), sbCatetories = new StringBuilder();

        sbQuery.Append("(");
        sbQuery.Append(query);
        sbQuery.Append(") AND (");

        foreach (var category in categories)
        {
            if (sbCatetories.Length > 0)
            {
                sbCatetories.Append(" OR ");
            }
            sbCatetories.Append(categoryField);
            sbCatetories.Append(":");
            sbCatetories.Append("\"");
            sbCatetories.Append(category);
            sbCatetories.Append("\"");
        }

        sbQuery.Append(sbCatetories.ToString());
        sbQuery.Append(")");

        return sbQuery.ToString();
    }

    static public string ToAsciiEncoding(this string term)
        => term?
                .Replace(".", "__ascii_2e_")
                .Replace("/", "__ascii_2f_") ?? "";
}
