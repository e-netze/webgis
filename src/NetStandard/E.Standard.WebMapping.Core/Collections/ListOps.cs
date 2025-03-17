using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Collections;

public class ListOps<T>
{
    static public List<T> Clone(List<T> l)
    {
        if (l == null)
        {
            return null;
        }

        List<T> c = new List<T>();

        foreach (T e in l)
        {
            c.Add(e);
        }

        return c;
    }

    static public List<T> Reverse(List<T> l)
    {
        if (l == null)
        {
            return null;
        }

        List<T> c = new List<T>();
        for (int i = l.Count - 1; i >= 0; i--)
        {
            c.Add(l[i]);
        }

        return c;
    }
}
