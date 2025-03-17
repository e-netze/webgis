using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.WebGIS.CMS;

public class UniqueList : List<string>
{
    List<string> _allowed = null;
    private bool _allowShortNameCheck = false;

    public UniqueList() : base() { }
    public UniqueList(List<string> allowed, bool allowShortNameCheck)
        : this()
    {
        _allowed = allowed;
        _allowShortNameCheck = allowShortNameCheck;
    }

    new public void Add(string n)
    {
        if (String.IsNullOrEmpty(n))
        {
            return;
        }

        if (base.Contains(n))
        {
            return;
        }

        if (_allowed != null)
        {
            bool found = false;
            foreach (string allowed in _allowed)
            {
                if (allowed == n ||
                    (
                    _allowShortNameCheck && Globals.ShortName(allowed) == n
                    ))
                {
                    found = true;
                    break;
                }

            }
            if (!found)
            {
                return;
            }
        }
        //if (_allowed != null &&
        //   _allowed.Contains(n) == false) return;

        base.Add(n);
    }

    public void Add(IEnumerable<string> ns)
    {
        if (ns == null)
        {
            return;
        }

        foreach (string n in ns)
        {
            this.Add(n);
        }
    }

    new public void AddRange(IEnumerable<string> ns)
    {
        Add(ns);
    }

    public string ToString(string seperator)
    {
        StringBuilder sb = new StringBuilder();
        foreach (string item in this)
        {
            if (String.IsNullOrEmpty(item))
            {
                continue;
            }

            if (sb.Length > 0)
            {
                sb.Append(seperator);
            }

            sb.Append(item);
        }

        return sb.ToString();
    }

    public static UniqueList Reduce(List<string> allowed, List<string> candidates, bool allowShortNameCheck)
    {
        UniqueList ret = new UniqueList(allowed, allowShortNameCheck);

        foreach (string candidate in candidates)
        {
            ret.Add(candidate);
        }
        return ret;
    }


}

public class UniList<T> : List<T>
{
    new public void Add(T x)
    {
        if (base.Contains(x))
        {
            return;
        }

        base.Add(x);
    }
}
