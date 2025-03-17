using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace E.Standard.Extensions.Collections;

public static class CollectionsExtensions
{
    public static T[] AppendArray<T>(this T[] x, T[] y)
    {
        if (x == null)
        {
            return y;
        }

        if (y == null)
        {
            return x;
        }

        int oldLen = x.Length;

        Array.Resize<T>(ref x, x.Length + y.Length);
        Array.Copy(y, 0, x, oldLen, y.Length);

        return x;
    }

    public static ICollection<T> TryAppendItems<T>(this ICollection<T> x, IEnumerable<T> y)
    {
        if (x == null)
        {
            return y?.ToArray();
        }

        if (y == null)
        {
            return x;
        }

        if (x is ICollection<T> && !x.IsReadOnly)
        {
            foreach (var item in y)
            {
                x.Add(item);
            }

            return x;
        }
        else
        {
            return AppendArray<T>(x.ToArray(), y?.ToArray());
        }
    }

    static public NameValueCollection ToNameValueCollection(this IEnumerable<KeyValuePair<string, StringValues>> collection)
    {
        NameValueCollection result = new NameValueCollection();

        if (collection != null)
        {
            foreach (var pairs in collection)
            {
                result[pairs.Key] = (string)pairs.Value;
            }
        }

        return result;
    }

    static public NameValueCollection ToNameValueCollection(this IEnumerable<KeyValuePair<string, string>> collection)
    {
        NameValueCollection result = new NameValueCollection();

        if (collection != null)
        {
            foreach (var pairs in collection)
            {
                result[pairs.Key] = pairs.Value;
            }
        }

        return result;
    }

    static public IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs(this NameValueCollection nvc)
    {
        var dict = new Dictionary<string, string>();

        foreach (var key in nvc.AllKeys)
        {
            dict.Add(key, nvc[key]);
        }

        return dict;
    }

    static public int Closest(this IEnumerable<int> list, int val)
    {
        return list.Aggregate((x, y) => Math.Abs(x - val) < Math.Abs(y - val) ? x : y);
    }

    static public double Closest<T>(this IEnumerable<double> list, double val)
    {
        return list.Aggregate((x, y) => Math.Abs(x - val) < Math.Abs(y - val) ? x : y);
    }

    static public IEnumerable<T> ConvertItems<T>(this double[] list)
    {
        return list?.Select(o => (T)Convert.ChangeType(o, typeof(T)));
    }

    static public T[] EmptyArrayToNull<T>(this T[] array)
    {
        if (array == null || array.Length == 0)
        {
            return null;
        }

        return array;
    }

    static public IEnumerable<T> OrEmptyArray<T>(this IEnumerable<T> list)
    {
        if (list == null)
        {
            return Array.Empty<T>();
        }

        return list;
    }

    static public bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        => collection is null || !collection.Any();

    static public T[] ToArrayOfOrNull<T>(this T item)
        => item is null
            ? null
            : new T[] { item };

    static public IDictionary<TKey, TItem> ToDictionaryOrNull<TKey, TItem>(this TItem item, TKey key)
        => item is null
         ? null
         : new Dictionary<TKey, TItem>() { { key, item } };

    static public OrderedDictionary<TKey, TItem> ToOrderedDictionaryOrNull<TKey, TItem>(this TItem item, TKey key)
        => item is null
         ? null
         : new OrderedDictionary<TKey, TItem>() { { key, item } };
}
