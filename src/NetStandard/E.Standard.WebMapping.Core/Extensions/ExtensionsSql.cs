using System;

namespace E.Standard.WebMapping.Core.Extensions;

public static class SqlExtensions
{
    static public string AppendWhereClause(this string filter, string appendFilter)
    {
        if (string.IsNullOrWhiteSpace(appendFilter))
        {
            return filter;
        }
        if (String.IsNullOrWhiteSpace(filter))
        {
            return appendFilter;
        }

        if (filter.ToLower().Contains(" or "))
        {
            filter = $"({filter})";
        }

        if (appendFilter.ToLower().Contains(" or "))
        {
            appendFilter = $"({appendFilter})";
        }

        return $"{filter} and {appendFilter}";
    }
}
