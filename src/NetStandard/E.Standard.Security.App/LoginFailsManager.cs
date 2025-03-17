using E.Standard.Security.Core.Excetions;
using System;
using System.Collections.Concurrent;

namespace E.Standard.Security.App;

public class LoginFailsManager
{
    private static readonly ConcurrentDictionary<string, FailInfo> _fails = new ConcurrentDictionary<string, FailInfo>();

    public LoginFailsManager(int maxFails = 3, double maxLockSeconds = 30D)
    {
        this.MaxFails = maxFails;
        this.MaxLockSeconds = maxLockSeconds;
    }

    public int MaxFails { get; private set; }
    public double MaxLockSeconds { get; private set; }

    public void AddFail(string username)
    {
        if (!_fails.ContainsKey(username))
        {
            _fails.TryAdd(username, new FailInfo(1));
        }
        else
        {
            _fails[username] = new FailInfo(_fails[username].Count + 1);
        }
    }

    public void CheckFails(string username)
    {
        if (_fails.ContainsKey(username))
        {
            var failInfo = _fails[username];

            if (failInfo.Span.TotalSeconds > this.MaxLockSeconds)
            {
                _fails.TryRemove(username, out failInfo);
            }
            else if (failInfo.Count >= this.MaxFails)
            {
                AddFail(username);
                throw new SecurityException("No many fails, try again in " + this.MaxLockSeconds + " seconds");
            }
        }
    }

    #region Classes

    private class FailInfo
    {
        public FailInfo(int count)
        {
            this.Count = count;
            this.Last = DateTime.Now;
        }

        public int Count { get; private set; }
        public DateTime Last { get; private set; }

        public TimeSpan Span
        {
            get
            {
                return DateTime.Now - Last;
            }
        }
    }

    #endregion
}
