using E.Standard.Extensions.Compare;
using E.Standard.Security.App.Services.Abstraction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

#nullable enable

namespace E.Standard.WebGIS.SubscriberDatabase.Services;

public class SubscriberDatabaseService
{
    private readonly string _connectionString;

    public SubscriberDatabaseService(
        ISecurityConfigurationService config,
        IOptionsMonitor<SubscriberDatabaseServiceOptions> options)
    {
        _connectionString = GetConnectionString(config, options);
    }

    virtual protected string GetConnectionString(
                            ISecurityConfigurationService config,
                            IOptionsMonitor<SubscriberDatabaseServiceOptions> options) =>
        config[options.CurrentValue.ConnectionStringConfigurationKey]
            .OrTake(options.CurrentValue.ConnectionStringConfigurationKey);

    public ISubscriberDb? CreateInstance(ILogger? logger = null)
    {
        if (String.IsNullOrEmpty(_connectionString))
        {
            return null;
        }

        return SubscriberDb.Create(_connectionString, logger);
    }
}

public class MigrateSubscriberDatabaseService : SubscriberDatabaseService
{

    public MigrateSubscriberDatabaseService(
        ISecurityConfigurationService config,
        IOptionsMonitor<SubscriberDatabaseServiceOptions> options)
                : base(config, options)
    {
    }

    protected override string GetConnectionString(ISecurityConfigurationService config, IOptionsMonitor<SubscriberDatabaseServiceOptions> options) =>
        config[$"{options.CurrentValue.ConnectionStringConfigurationKey}-mig"];
    

    public bool HasMigrationSettings() => this.CreateInstance() != null;
}
