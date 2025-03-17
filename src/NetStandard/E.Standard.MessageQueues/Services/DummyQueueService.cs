using E.Standard.MessageQueues.Services.Abstraction;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.MessageQueues.Services;

public class DummyQueueService : IMessageQueueService
{
    public Task<IEnumerable<string>> DequeueAsync(int count = 1)
    {
        return Task.FromResult<IEnumerable<string>>(new string[0]);
    }

    public Task<bool> EnqueueAsync(IEnumerable<string> messages)
    {
        return Task.FromResult(true);
    }

    public Task<bool> EnqueueAsync(string queuePrefix, IEnumerable<string> messages, bool includeOwnQueue = true)
    {
        return Task.FromResult(true);
    }

    public Task<bool> RegisterQueueAsync(int lifetime = 0, int itemLifetime = 0)
    {
        return Task.FromResult(true);
    }
}
