using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E.Standard.WebGIS.Tools.Extensions;

static class GeneralExtensions
{
    #region Identify, Queries

    static public IEnumerable<IQueryBridge> TakeFavorites(this IEnumerable<IQueryBridge> queries, IEnumerable<string> favorites, out bool excludedQueries)
    {
        excludedQueries = false;

        if (favorites == null || favorites.Count() == 0)
        {
            return queries;
        }

        var result = queries.Where(q => favorites.Contains(q.QueryGlobalId));

        if (result.Count() == 0 || result.Count() == queries.Count())
        {
            return queries;
        }

        excludedQueries = true;
        return result;
    }

    static public IEnumerable<IQueryBridge> RemoveFavorites(this IEnumerable<IQueryBridge> queries, IEnumerable<string> favorites, out bool excludedQueries)
    {
        excludedQueries = false;

        if (favorites == null || favorites.Count() == 0)
        {
            return queries;
        }

        var result = queries.Where(q => !favorites.Contains(q.QueryGlobalId));

        if (result.Count() == 0 || result.Count() == queries.Count())
        {
            return queries;
        }

        excludedQueries = true;
        return result;
    }

    static public IEnumerable<IQueryBridge> OrderByFavorites(this IEnumerable<IQueryBridge> queries, IEnumerable<string> orderedFavorites)
    {
        if (orderedFavorites == null || orderedFavorites.Count() == 0)
        {
            return queries;
        }

        var result = queries.ToList();
        result.Sort(new QueryByFavoritesComparer(orderedFavorites));
        return result;
    }

    private class QueryByFavoritesComparer : IComparer<IQueryBridge>
    {
        public QueryByFavoritesComparer(IEnumerable<string> orderedFavories)
        {
            this.OrderedFavorites = orderedFavories.ToList();
        }

        private List<string> OrderedFavorites { get; set; }

        public int Compare(IQueryBridge x, IQueryBridge y)
        {
            int indexX = OrderedFavorites.IndexOf(x.QueryGlobalId);
            int indexY = OrderedFavorites.IndexOf(y.QueryGlobalId);

            if (indexX >= 0 && indexY >= 0)
            {
                return indexX.CompareTo(indexY);
            }

            if (indexX >= 0 && indexY < 0)
            {
                return -1;
            }

            if (indexX < 0 && indexX >= 0)
            {
                return 1;
            }

            return x.Name.CompareTo(y.Name);  // sonst alphabetisch
        }
    }

    #endregion

    #region QueryResult / Features

    static public (string serviceId, string queryId, int featureId) ParseFeatureGlobalOid(this string oid)
    {
        if (String.IsNullOrEmpty(oid))
        {
            throw new ArgumentException("Invalid FeatureOid: String.Empty");
        }

        var parts = oid.Split(':');
        if (parts.Length != 3)
        {
            throw new ArgumentException($"Invalid FeatureOid: {oid}");
        }

        return (parts[0], parts[1], int.Parse(parts[2]));
    }

    static public void SetGlobalOid(this Feature feature, IQueryBridge query)
    {
        if (feature != null && query != null)
        {
            feature.GlobalOid = $"{query.QueryGlobalId}:{feature.Oid}";
        }
    }

    #endregion

    #region Publish

    static public string ToMapNameOrCategory(this string name)
    {
        foreach (char c in "/\\()!\"'~#%&*:<>?{|}")
        {
            if (name.Contains(c))
            {
                throw new Exception("Invalid charcter " + c);
            }
        }

        if (name.Trim().StartsWith("_"))
        {
            throw new Exception("underscore not allowed as first character");
        }

        return name.Trim();
    }

    #endregion

    #region Error Handling

    public static void AppendErrorMessage(this StringBuilder sb, string title, Exception ex)
    {
        sb.Append($"{title}: {ex.Message}\n");
    }

    #endregion

    #region UIElements

    static public ICollection<T> FindRecursive<T>(this ICollection<IUIElement> uiElements)
    {
        List<T> foundedElements = new List<T>();
        var typeOfT = typeof(T);

        if (uiElements != null)
        {
            foreach (var uiElement in uiElements)
            {
                if (typeOfT.Equals(uiElement?.GetType()))
                {
                    foundedElements.Add((T)uiElement);
                }

                foundedElements.AddRange(uiElement.elements.FindRecursive<T>());
            }
        }

        return foundedElements;
    }

    #endregion
}
