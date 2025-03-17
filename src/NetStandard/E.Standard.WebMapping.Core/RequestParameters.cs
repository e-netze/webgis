using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core;

public class RequestParameters
{
    private readonly IDictionary<string, string> _dict = null;

    public RequestParameters()
    {
    }

    public RequestParameters(IDictionary<string, string> dict)
    {
        _dict = dict;
    }

    public RequestParameters(string url)
    {
        _dict = new Dictionary<string, string>();

        int pos = url.IndexOf("?");
        url = url.Substring(pos + 1, url.Length - pos - 1);

        foreach (string p in url.Split('&'))
        {
            pos = p.IndexOf("=");
            if (pos == -1)
            {
                _dict.Add(p, String.Empty);
            }
            else
            {
                _dict.Add(p.Substring(0, pos), p.Substring(pos + 1, p.Length - pos - 1));
            }
        }
    }

    public string this[string key]
    {
        get
        {
            string ret = String.Empty;

            if (_dict != null && _dict.TryGetValue(key, out ret))
            {
                return ret;
            }

            return String.Empty;
        }
    }

    public string GetValue(string key, bool ignoreCase)
    {
        if (ignoreCase == false)
        {
            return this[key];
        }
        else
        {
            foreach (var k in _dict.Keys)
            {
                if (k.Equals(key, StringComparison.OrdinalIgnoreCase) && !String.IsNullOrEmpty(_dict[k]))
                {
                    return _dict[k];
                }
            }
        }

        return String.Empty;
    }
}