using E.Standard.GeoRSS.Abstraction;
using System.Collections.Generic;

namespace E.Standard.GeoRSS20;

public class GeoRSSItem : IItem
{
    Dictionary<string, string> _dic = new Dictionary<string, string>();

    #region IItem Member

    public string this[string attribute]
    {
        get
        {
            string obj;
            if (_dic.TryGetValue(attribute, out obj))
            {
                return obj;
            }

            return null;
        }
        set
        {
            string obj;
            if (_dic.TryGetValue(attribute, out obj))
            {
                _dic[attribute] = value;
            }
            else
            {
                _dic.Add(attribute, value);
            }
        }
    }

    #endregion
}
