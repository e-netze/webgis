using E.Standard.Custom.Core.Abstractions;
using E.Standard.MessageQueues.Services.Abstraction;
using System;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Services.Worker;

public class MessageQueueWorkerService : IWorkerService
{
    private readonly IMessageQueueService _messageQueue;
    private readonly InMemoryPortalAppCache _cache;

    public MessageQueueWorkerService(
            IMessageQueueService messageQueue,
            InMemoryPortalAppCache cache
        )
    {
        _messageQueue = messageQueue;
        _cache = cache;
    }

    public int DurationSeconds => 5;

    async public Task<bool> DoWork()
    {
        foreach (var message in await _messageQueue.DequeueAsync(5))
        {
            if (message.StartsWith("cacheclear:"))
            {
                var cacheId = message.Substring("cacheclear:".Length);

                Console.WriteLine($"MessageQueueWorkerService: ClearCache {cacheId}");
                _cache.Clear();
            }
        }

        return true;
    }

    public Task<bool> Init()
    {
        return _messageQueue.RegisterQueueAsync(60, 30);
    }
}
