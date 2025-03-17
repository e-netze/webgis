using E.Standard.WebMapping.Core.Abstraction;
using System;
using System.Collections;

namespace E.Standard.WebMapping.Core;

public class UserData : IUserData
{
    private Hashtable _userdata = new Hashtable();
    private object locker = new object();

    #region IUserData Member

    public object UserValue(string key, object defaultValue)
    {
        try
        {
            lock (locker)
            {
                if (_userdata.ContainsKey(key))
                {
                    return _userdata[key];
                }
            }
        }
        catch
        {
        }
        return defaultValue;
    }

    public string UserString(string key)
    {
        return (string)UserValue(key, String.Empty);
    }
    public int UserInt(string key)
    {
        return (int)UserValue(key, 0);
    }

    public void SetUserValue(string key, object value)
    {
        lock (locker)
        {
            if (_userdata.ContainsKey(key))
            {
                _userdata[key] = value;
            }
            else
            {
                _userdata.Add(key, value);
            }
        }
    }

    #endregion

    #region IUserData Member

    public void ClearAllUserValues()
    {
        lock (locker)
        {
            _userdata.Clear();
        }
    }

    public void ClearUserValue(string key)
    {
        lock (locker)
        {
            if (_userdata.ContainsKey(key))
            {
                _userdata.Remove(key);
            }
        }
    }

    #endregion

    public void CopyData(UserData copyFrom)
    {
        if (copyFrom?._userdata is null)
        {
            return;
        }

        lock (locker)
        {
            foreach (object key in copyFrom._userdata.Keys)
            {
                if (_userdata.ContainsKey(key))
                {
                    _userdata[key] = copyFrom._userdata[key];
                }
                else
                {
                    _userdata.Add(key, copyFrom._userdata[key]);
                }
            }
        }
    }
}
