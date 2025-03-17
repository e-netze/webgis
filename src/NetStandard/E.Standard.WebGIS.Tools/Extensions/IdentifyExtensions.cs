using E.Standard.WebGIS.Tools.Identify;
using E.Standard.WebMapping.Core.Api;
using System;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Extensions;

static internal class IdentifyExtensions
{
    static public bool IsIdentifyToolQuery(this string queryTheme)
    {
        return queryTheme.StartsWith("~~");
    }

    static public string ToolIdFromQueryTheme(this string queryTheme, ApiToolEventArguments e)
    {
        if (!queryTheme.IsIdentifyToolQuery())
        {
            throw new Exception("Invalid IdentifyToolQuery Theme");
        }

        string toolId = queryTheme.Substring(2);

        if (toolId.Contains("~"))
        {
            string toolParameters = toolId.Substring(toolId.IndexOf("~") + 1);
            toolId = toolId.Substring(0, toolId.IndexOf("~"));

            foreach (var toolParameter in toolParameters.Split(';'))
            {
                int pos = toolParameter.IndexOf("=");
                if (pos > 0)
                {
                    e[toolParameter.Substring(0, pos)] = toolParameter.Substring(pos + 1);
                }
            }
        }

        return toolId;
    }

    static public bool IsFavoritesRemovedQueryThemeType(this string queryTheme)
    {
        return new string[] {
                    IdentifyConst.QueryVisibleRemoveFavorites,
                    IdentifyConst.QueryInvisibleRemoveFavorites,
                    IdentifyConst.QueryAllRemoveFavorites }.Contains(queryTheme);
    }

    static public bool IsDefaultQueryThemeType(this string queryTheme)
    {
        return new string[] {
                    IdentifyConst.QueryVisibleDefault,
                    IdentifyConst.QueryInvisibleDefault,
                    IdentifyConst.QueryAllDefault }.Contains(queryTheme);
    }

    static public bool IsIgnoreFavoritesQueryThemeType(this string queryTheme)
    {
        return new string[] {
                    IdentifyConst.QueryVisibleIgnoreFavorites,
                    IdentifyConst.QueryInvisibleIgnoreFavorites,
                    IdentifyConst.QueryAllIgnoreFavorites }.Contains(queryTheme);
    }

    static public bool IsAllInOneQueryThemeType(this string queryTheme)
    {
        return new string[] {
                    IdentifyConst.QueryAllDefault,
                    IdentifyConst.QueryAllIgnoreFavorites }.Contains(queryTheme);
    }

    static public bool HasAppendToUIFlag(this string queryTheme)
    {
        return !String.IsNullOrEmpty(queryTheme) && queryTheme.EndsWith(".append");
    }

    static public string RemoveAppendToUIFlag(this string queryTheme)
    {
        return queryTheme.HasAppendToUIFlag() ?
            queryTheme.Substring(0, queryTheme.Length - ".append".Length) :
            queryTheme;

    }

    static public string AddAppendToUIFlag(this string queryTheme)
    {
        return queryTheme.HasAppendToUIFlag() ?
            queryTheme :
            $"{queryTheme}.append";
    }

    static public string ToDefaultTypeQueryTheme(this string queryTheme)
    {
        if (queryTheme.Length == 1)
        {
            return queryTheme;
        }
        else if (queryTheme.Length == 2)
        {
            return queryTheme.Substring(1);
        }

        throw new AggregateException($"Querytheme {queryTheme} can't converted to DefaultTypeQueryTheme");
    }

    static public string ToRemoveFavoritesTypeQueryTheme(this string queryTheme)
    {
        if (queryTheme.Length == 1)
        {
            return $".{queryTheme}";
        }
        else if (queryTheme.Length == 2)
        {
            return $".{queryTheme.Substring(1)}";
        }

        throw new AggregateException($"Querytheme {queryTheme} can't converted to RemoveFavoritesTypeQueryTheme");
    }

    static public string ToIgnoreFavoritesTypeQueryTheme(this string queryTheme)
    {
        if (queryTheme.Length == 1)
        {
            return $"!{queryTheme}";
        }
        else if (queryTheme.Length == 2)
        {
            return $"!{queryTheme.Substring(1)}";
        }

        throw new AggregateException($"Querytheme {queryTheme} can't converted to RemoveFavoritesTypeQueryTheme");
    }
}
