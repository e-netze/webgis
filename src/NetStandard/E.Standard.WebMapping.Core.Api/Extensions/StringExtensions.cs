using E.Standard.WebMapping.Core.Api.Models;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.Core.Api.Extensions;

static public class StringExtensions
{
    public static IEnumerable<AutoCompleteResultItem> ToAutocompleteItems(this IEnumerable<string> values)
    {
        if (values == null)
        {
            return new AutoCompleteResultItem[0];
        }

        return values.Select(v => new AutoCompleteResultItem()
        {
            Label = v,
            Value = v
        });
    }
}
