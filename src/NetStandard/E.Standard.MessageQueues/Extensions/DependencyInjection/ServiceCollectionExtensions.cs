using E.Standard.MessageQueues.Services;
using E.Standard.MessageQueues.Services.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace E.Standard.MessageQueues.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddDummyMessageQueueService(this IServiceCollection services)
    {
        services.AddTransient<IMessageQueueService, DummyQueueService>();
        return services;
    }

    static public IServiceCollection AddMessageQueueNetService(
        this IServiceCollection services,
        Action<MessageQueueNetServiceOptions> configureOptions)
    {
        Console.WriteLine("Adding MessageQueueNET Service...");
        services.Configure<MessageQueueNetServiceOptions>(configureOptions);
        services.AddSingleton<IMessageQueueService, MessageQueueNetService>();
        return services;
    }
}
