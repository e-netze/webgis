using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace E.Standard.ThreadsafeClasses;

public class FuzzyMutexAsync
{
    private static readonly ConcurrentDictionary<string, DateTime> _mutexDirectory = new ConcurrentDictionary<string, DateTime>();

    async static public Task<IMutex> LockAsync(string key, int timeoutMilliSeconds = 20000)
    {
        var random = new Random(Environment.TickCount);
        var start = DateTime.Now;
        //await Task.Delay(random.Next(1000));

        while (true)
        {
            //while (_mutexDirectory.ContainsKey(key))
            //{
            //    await Task.Delay(random.Next(50));
            //}

            if (_mutexDirectory.TryAdd(key, DateTime.Now))
            {
                break;
            }
            else
            {
                if ((DateTime.Now - start).TotalMilliseconds > timeoutMilliSeconds)
                {
                    throw new FuzzyMutexAsyncException($"FuzzyMutex - timeout milliseconds reached: {(DateTime.Now - start).TotalMilliseconds} > {timeoutMilliSeconds}");
                }

                await Task.Delay(random.Next(50));
            }
        }

        return new Mutex(key);
    }

    #region Classes

    public interface IMutex : IDisposable
    {

    }

    private class Mutex : IMutex
    {
        private readonly string _key;

        public Mutex(string key)
        {
            _key = key;
        }

        #region IDisposable

        public void Dispose()
        {
            if (!_mutexDirectory.TryRemove(_key, out DateTime removed))
            {

            }
        }

        #endregion
    }

    #endregion
}

public class FuzzyMutexAsyncException : Exception
{
    public FuzzyMutexAsyncException(string message)
            : base(message)
    { }
}
