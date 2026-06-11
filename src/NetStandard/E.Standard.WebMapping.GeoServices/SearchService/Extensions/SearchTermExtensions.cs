using System;
using System.Linq;

using E.Standard.CMS.Core;
using E.Standard.Extensions.Text;

namespace E.Standard.WebMapping.GeoServices.SearchService.Extensions;

internal static class SearchServiceExtensions
{
    extension(string term)
    {
        public string EscapeSolrQuery()
        {
            string[] chars =
            {
            "\\", "+", "-", "!", "(", ")", ":", "^",
            "[", "]", "\"", "{", "}", "~", "*", "?",
            "|", "&", ";", "/"
        };

            foreach (var c in chars)
            {
                term = term.Replace(c, $"\\{c}");
            }

            return term;
        }

        public string ToSafeSolrTerm()
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return string.Empty;
            }

            if (term.Length > 200)
            {
                throw new ArgumentException("Search term is too long.");
            }

            if (term.Any(c => char.IsControl(c)))
            {
                throw new ArgumentException("Search term contains control characters.");
            }

            return term.EscapeSolrQuery();
        }

        
    }
}

