using E.Standard.Extensions.Compare;
using E.Standard.Security.App.Services.Abstraction;
using Microsoft.Extensions.Options;
using System;

namespace E.Standard.WebGIS.SubscriberDatabase.Services;

public class SubscriberDatabaseService
{
    private readonly string _connectionString;

    public SubscriberDatabaseService(
        ISecurityConfigurationService config,
        IOptionsMonitor<SubscriberDatabaseServiceOptions> options)
    {
        _connectionString = config[options.CurrentValue.ConnectionStringConfigurationKey]
            .OrTake(options.CurrentValue.ConnectionStringConfigurationKey);
    }

    public ISubscriberDb CreateInstance()
    {
        if (String.IsNullOrEmpty(_connectionString))
        {
            return null;
        }

        return SubscriberDb.Create(_connectionString);
    }
}
