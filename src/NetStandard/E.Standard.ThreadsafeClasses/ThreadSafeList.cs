using System.Collections.Generic;
using System.Threading;

namespace E.Standard.ThreadSafe;

public interface ILockable
{
    void Lock();
    void Unlock();
}

public class ThreadSafeList<T> : List<T>, ILockable
{
    public object locker = new object();

    new public void Add(T t)
    {
        lock (locker)
        {
            base.Add(t);
        }
    }
    new public int Count
    {
        get
        {
            lock (locker)
            {
                return base.Count;
            }
        }
    }
    new public bool Contains(T t)
    {
        lock (locker)
        {
            return base.Contains(t);
        }
    }
    new public int IndexOf(T t)
    {
        lock (locker)
        {
            return base.IndexOf(t);
        }
    }

    new public void AddRange(IEnumerable<T> t)
    {
        lock (locker)
        {
            base.AddRange(t);
        }
    }

    public void SetItems(IEnumerable<T> t)
    {
        lock (locker)
        {
            base.Clear();
            base.AddRange(t);
        }
    }

    new public void Clear()
    {
        lock (locker)
        {
            base.Clear();
        }
    }

    public void CopyElementsThreadSafeTo(ThreadSafeList<T> t, bool clearAfterCopy = false)
    {
        lock (locker)
        {
            t.AddRange(this);
            if (clearAfterCopy == true)
            {
                base.Clear();
            }
        }
    }

    public T[] ToArrayThreadSafe()
    {
        lock (locker)
        {
            List<T> array = new List<T>();
            array.AddRange(this);
            return array.ToArray();
        }
    }

    public IEnumerable<T> ThreadSafeLinq()
    {
        return ToArrayThreadSafe();
    }

    new public List<T>.Enumerator GetEnumerator()
    {
        lock (locker)
        {
            List<T> list = new List<T>();
            list.AddRange(this.DirtyList());
            return list.GetEnumerator();
        }
    }

    protected List<T> DirtyList()
    {
        return this;
    }

    public T GetAt(int index)
    {
        //lock (locker)
        return this[index];
    }

    new public void RemoveAt(int index)
    {
        //lock (locker)
        this.RemoveAt(index);
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
