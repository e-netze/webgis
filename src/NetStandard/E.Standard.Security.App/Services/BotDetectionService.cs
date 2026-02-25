using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace E.Standard.Security.App.Services;

public class BotDetectionService
{
    private ConcurrentDictionary<string, DateTime> _suspiciousUsers = new ConcurrentDictionary<string, DateTime>();
    private ConcurrentDictionary<string, DateTime> _suspiciousUserBlocks = new ConcurrentDictionary<string, DateTime>();

    public void AddSuspiciousUser(string username)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            _suspiciousUsers[username] = DateTime.UtcNow;
        }
    }

    public bool IsSuspiciousUser(string username)
    {
        if (String.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        if (_suspiciousUsers.ContainsKey(username))
        {
            var lastSet = _suspiciousUsers[username];
            if ((DateTime.UtcNow - lastSet).TotalHours >= 1.0)
            {
                _suspiciousUsers.TryRemove(username, out lastSet);
                return false;
            }
            return true;
        }

        return false;
    }

    public void RemoveSuspiciousUser(string username)
    {
        if (_suspiciousUsers.ContainsKey(username))
        {
            DateTime lastSet;

            _suspiciousUsers.TryRemove(username, out lastSet);
        }
    }

    async public Task BlockSuspicousUserAsync(string username, int milliseconds = 10000)
    {
        if (String.IsNullOrEmpty(username))
        {
            return;
        }

        if (_suspiciousUserBlocks.ContainsKey(username) && (DateTime.UtcNow - _suspiciousUserBlocks[username]).TotalMilliseconds < milliseconds)
        {
            throw new Exception("Suspicous bot request detected");
        }

        _suspiciousUserBlocks.TryAdd(username, DateTime.UtcNow);
        await Task.Delay(milliseconds);
        _suspiciousUserBlocks.TryRemove(username, out DateTime value);
    }
}
