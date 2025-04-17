using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.CMS.Core.Collections;

public class UniqueListBuilder<T>
{
    private readonly List<T> _list = new();

    public bool Add(T item, Func<T, bool> equalityPredicate)
    {
        foreach(var candidate in _list)
        {
            if (equalityPredicate(candidate) == true)
            {
                return false;
            }
        }

        _list.Add(item);
        return true;
    }

    public int AddRange(IEnumerable<T> items, Func<T, bool> equalityPredicate)
    {
        int counter = 0;

        foreach(var item in items)
        {
            if (Add(item, equalityPredicate))
            {
                counter++;
            }
        }

        return counter;
    }

    public void Remove(T item)
    {
        _list.Remove(item);
    }

    public List<T> GetList() { return _list; }
}
