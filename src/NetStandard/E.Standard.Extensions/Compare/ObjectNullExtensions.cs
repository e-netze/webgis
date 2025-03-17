using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Extensions.Compare;

public static class ObjectNullExtensions
{
    public static T ThrowIfNull<T>(this T obj, Func<string> messageFunc)
        where T : class
    {
        return obj ?? throw new Exception(messageFunc());
    }

    public static IEnumerable<T> ThrowIfNullOrEmpty<T>(this IEnumerable<T> list, Func<string> messageFunc)
    {
        if (list == null || list.Count() == 0)
        {
            throw new Exception(messageFunc());
        }

        return list;
    }

    public static bool ThrowIfFalse(this bool boolean, Func<string> messageFunc)
    {
        if (boolean == false)
        {
            throw new Exception(messageFunc());
        }

        return boolean;
    }

    public static bool ThrowIfTrue(this bool boolean, Func<string> messageFunc)
    {
        if (boolean == true)
        {
            throw new Exception(messageFunc());
        }

        return boolean;
    }

    public static IEnumerable<T> ThrowIfNullOrCountLessThan<T>(this IEnumerable<T> list, int count, Func<string> messageFunc)
    {
        if (list == null || list.Count() < count)
        {
            throw new Exception(messageFunc());
        }

        return list;
    }

    public static IEnumerable<T> ThrowIfNullOrCountNotEqual<T>(this IEnumerable<T> list, int count, Func<string> messageFunc)
    {
        if (list == null || list.Count() != count)
        {
            throw new Exception(messageFunc());
        }

        return list;
    }

    public static void IfNotNull<T>(this T obj, Action<T> action)
    {
        if (obj != null)
        {
            action(obj);
        }
    }

    public static void EachIfNotNull<T>(this IEnumerable<T> list, Action<T> processItem)
    {
        foreach (var item in list ?? Array.Empty<T>())
        {
            processItem(item);
        }
    }
}
