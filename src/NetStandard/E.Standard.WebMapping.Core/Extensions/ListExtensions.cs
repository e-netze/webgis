using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.Core.Extensions;
static internal class ListExtensions
{
    static public T Get<T>(this IReadOnlyList<KeyValuePair<string, object>> keyValuePairs, string key)
    {
        KeyValuePair<string, object>? kvp = keyValuePairs.FirstOrDefault(x => x.Key == key);

        if (kvp is null)
        {
            return default(T);
        }

        return (T)kvp.Value.Value;
    }
}
