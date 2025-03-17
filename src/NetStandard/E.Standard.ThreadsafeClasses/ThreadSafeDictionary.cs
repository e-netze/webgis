using System.Collections.Generic;
using System.Threading;

namespace E.Standard.ThreadSafe;

public class ThreadSafeDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ILockable
{
    public object locker = new object();

    new public void Add(TKey key, TValue val)
    {
        lock (locker)
        {
            if (base.ContainsKey(key))
            {
                base[key] = val;
            }
            else
            {
                base.Add(key, val);
            }
        }
    }

    new public TValue this[TKey key]
    {
        get
        {
            lock (locker)
            {
                return base[key];
            }
        }
        set
        {
            lock (locker)
            {
                base[key] = value;
            }
        }
    }

    new public bool ContainsKey(TKey key)
    {
        lock (locker)
        {
            return base.ContainsKey(key);
        }
    }

    new public bool ContainsValue(TValue val)
    {
        lock (locker)
        {
            return base.ContainsValue(val);
        }
    }

    new public void Clear()
    {
        lock (locker)
        {
            base.Clear();
        }
    }

    new public bool Remove(TKey key)
    {
        lock (locker)
        {
            try { return base.Remove(key); }
            catch { }
            return false;
        }
    }

    public List<TKey> AllKeys
    {
        get
        {
            lock (locker)
            {
                List<TKey> keys = new List<TKey>();

                foreach (TKey key in base.Keys)
                {
                    keys.Add(key);
                }

                return keys;
            }
        }
    }

    public Dictionary<TKey, TValue> Copy()
    {
        var dict = new Dictionary<TKey, TValue>();
        lock (locker)
        {
            foreach (TKey key in base.Keys)
            {
                dict.Add(key, base[key]);
            }
        }
        return dict;
    }

    #region ILockable Member

    public void Lock()
    {
        Monitor.Enter(locker);
    }

    public void Unlock()
    {
        Monitor.Exit(locker);
    }

    #endregion
}
