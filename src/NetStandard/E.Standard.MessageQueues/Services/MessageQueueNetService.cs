using E.Standard.MessageQueues.Services.Abstraction;
using MessageQueueNET.Client;
using MessageQueueNET.Client.Models;
using MessageQueueNET.Client.Models.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.MessageQueues.Services;

public class MessageQueueNetService : IMessageQueueService
{
    private readonly MessageQueueNetServiceOptions _options;
    private readonly ILogger<MessageQueueNetService> _logger;

    public MessageQueueNetService(
            IOptionsMonitor<MessageQueueNetServiceOptions> optionsMonitor,
            ILogger<MessageQueueNetService> logger)
    {
        _options = optionsMonitor.CurrentValue;
        _logger = logger;
    }

    #region IMessageQueueService

    async public Task<bool> RegisterQueueAsync(int lifetime = 0, int itemLifetime = 0)
    {
        try
        {
            await ClientInstance().RegisterAsync(lifetime, itemLifetime);

            return true;
        }
        catch { return false; }
    }

    async public Task<bool> EnqueueAsync(IEnumerable<string> messages)
    {
        try
        {
            var apiResult = await ClientInstance().EnqueueAsync(messages);

            return apiResult.Success;
        }
        catch { return false; }
    }

    async public Task<bool> EnqueueAsync(string queuePrefix, IEnumerable<string> messages, bool includeOwnQueue = true)
    {
        try
        {
            queuePrefix = String.IsNullOrEmpty(_options.Namespace)
                                ? queuePrefix
                                : $"{_options.Namespace}.{queuePrefix}";

            var ownQueueName = String.IsNullOrEmpty(_options.Namespace)
                                ? _options.QueueName
                                : $"{_options.Namespace}.{_options.QueueName}";

            var queueNamesResult = await ClientInstance().QueueNamesAsync();
            if (!queueNamesResult.Success || queueNamesResult.QueueNames is null)
            {
                return false;
            }

            foreach (var queueName in queueNamesResult.QueueNames)
            {
                if (queueName.StartsWith(queuePrefix))
                {
                    if (queueName.Equals(ownQueueName) && includeOwnQueue == false)
                    {
                        continue;
                    }

                    var queueClient = ClientInstance(queueName);

                    await queueClient.EnqueueAsync(messages);

                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("EnqueueAsync: {queueName} => {queueMessages}", queueName, String.Join(", ", messages));
                    }
                }
            }

            return true;
        }
        catch { return false; }
    }

    async public Task<IEnumerable<string>> DequeueAsync(int count = 1)
    {
        try
        {
            var result = await ClientInstance().DequeueAsync(count, register: false);

            if (_logger.IsEnabled(LogLevel.Information)
                && result?.Messages != null
                && result.Messages.Count() > 0)
            {
                _logger.LogInformation("DequeueAsync: {queueName} <= {queueMessages}", _options.QueueName, String.Join(", ", result.Messages.Select(m => m.Value)));
            }

            return result?.Messages?.Select(m => m.Value) ?? new string[0];
        }
        catch (Exception ex)
        {
            _logger.LogError("Error on DequeueAsync: {message}", ex.Message);
            return new string[0];
        }
    }

    #endregion

    #region Helper


    private QueueClient ClientInstance()
    {
        return ClientInstance(String.IsNullOrEmpty(_options.Namespace)
                                ? _options.QueueName
                                : $"{_options.Namespace}.{_options.QueueName}"
                );
    }

    private QueueClient ClientInstance(string queueNameWithNamespace)
    {
        var messageQueueConnection = new MessageQueueConnection(_options.QueueServiceUrl);

        if (!string.IsNullOrEmpty(_options.ApiClient))
        {
            messageQueueConnection.Authentication = new BasicAuthentication(
                    _options.ApiClient,
                    _options.ApiClientSecret
               );
        }

        return new QueueClient(
                    messageQueueConnection,
                    queueNameWithNamespace
                );
    }

    #endregion
}
