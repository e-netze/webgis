using E.Standard.CMS.Core.IO.Abstractions;
using System;
using System.Collections;

namespace E.Standard.CMS.Core.Security;

public class AuthorizableArray : IPersistable
{
    private string _nodeName;
    private Array _array;
    private Type _itemType;

    public AuthorizableArray(string nodeName, Array array, Type itemType)
    {
        _nodeName = nodeName;
        _array = array;
        _itemType = itemType;
    }

    public Array Array
    {
        get { return _array; }
    }

    #region IPersistable Member

    public void Load(IStreamDocument stream)
    {
        int count = (int)stream.Load(_nodeName + "_count", 0);
        if (count == 0)
        {
            return;
        }

        ArrayList array = new ArrayList();
        for (int i = 0; i < count; i++)
        {
            AuthorizableArrayItem item = Activator.CreateInstance(_itemType) as AuthorizableArrayItem;
            if (item == null)
            {
                continue;
            }

            string itemGuid = (string)stream.Load(_nodeName + i, string.Empty);
            if (string.IsNullOrEmpty(itemGuid))
            {
                continue;
            }

            item.ItemGuid = itemGuid;

            item.Load(stream, _nodeName);
            array.Add(item);
        }

        _array = array.ToArray();
    }

    public void Save(IStreamDocument stream)
    {
        if (_array == null || _array.Length == 0)
        {
            stream.Remove(_nodeName + "_count");
            return;
        }

        int i = 0;
        foreach (object o in _array)
        {
            if (!(o is AuthorizableArrayItem))
            {
                continue;
            }

            AuthorizableArrayItem item = (AuthorizableArrayItem)o;
            stream.Save(_nodeName + i, item.ItemGuid);
            item.Save(stream, _nodeName);
            i++;
        }

        stream.Save(_nodeName + "_count", i);

    }

    #endregion
}
