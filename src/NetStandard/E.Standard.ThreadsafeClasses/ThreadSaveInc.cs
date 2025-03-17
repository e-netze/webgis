namespace E.Standard.ThreadSafe;

public class ThreadSaveInc
{
    public object locker = new object();
    public int _p = 0;

    public void Inc()
    {
        lock (locker)
        {
            _p++;
        }
    }

    public int Position
    {
        get
        {
            lock (locker)
            {
                return _p;
            }
        }
    }
}
