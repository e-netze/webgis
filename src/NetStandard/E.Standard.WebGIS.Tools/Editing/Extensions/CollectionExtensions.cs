using E.Standard.WebMapping.Core.Api.Bridge;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Editing.Extensions;
internal static class CollectionExtensions
{
    static public IEnumerable<IQueryBridge> FilterByEditThemeTags(this IEnumerable<IQueryBridge> queries, IBridge bridge, string[] tags)
    {
        if (tags == null || tags.Length == 0)
        {
            return queries;
        }

        List<IQueryBridge> result = new List<IQueryBridge>(queries.Count());

        foreach (var query in queries)
        {
            var editTheme = bridge.GetEditThemes(query.GetServiceId())
                .Where(e => e.LayerId == query.GetLayerId())
                .FirstOrDefault();

            if (editTheme.Tags.ContainsAny(tags, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(query);
            }
        }

        return result;
    }

    static private bool ContainsAny(this string[] values, string[] candidates, StringComparer stringComparer)
    {
        if (values == null || candidates == null || values.Length == 0 || candidates.Length == 0)
            return false;

        var valueSet = new HashSet<string>(values, stringComparer);
        foreach (var candidate in candidates)
        {
            if (valueSet.Contains(candidate))
                return true;
        }

        return false;
    }
}
