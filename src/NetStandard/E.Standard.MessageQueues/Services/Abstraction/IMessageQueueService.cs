using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.MessageQueues.Services.Abstraction;

public interface IMessageQueueService
{
    Task<bool> RegisterQueueAsync(int lifetime = 0, int itemLifetime = 0);

    Task<bool> EnqueueAsync(IEnumerable<string> messages);

    Task<bool> EnqueueAsync(string queuePrefix, IEnumerable<string> messages, bool includeOwnQueue = true);

    Task<IEnumerable<string>> DequeueAsync(int count = 1);
}
