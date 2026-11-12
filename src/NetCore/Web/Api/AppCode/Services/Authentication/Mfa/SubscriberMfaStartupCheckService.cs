#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

using E.Standard.Api.App.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Core.AppCode.Services.Authentication.Mfa;

/// <summary>
/// Logs a warning at startup, if MFA is configured as required but can't be enforced
/// (SQL subscriber database: table webgis_api_subscriber_mfa missing => run the Setup page).
/// </summary>
public class SubscriberMfaStartupCheckService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SubscriberSecurityOptions _options;
    private readonly ILogger<SubscriberMfaStartupCheckService> _logger;

    public SubscriberMfaStartupCheckService(IServiceProvider serviceProvider,
                                            IOptions<SubscriberSecurityOptions> options,
                                            ILogger<SubscriberMfaStartupCheckService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.MfaRequired == MfaRequiredMode.None)
        {
            return Task.CompletedTask;
        }

        // don't block the startup (database may be slow/unreachable)
        _ = Task.Run(() =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var accountSecurity = scope.ServiceProvider.GetRequiredService<SubscriberAccountSecurityService>();

                if (!accountSecurity.IsMfaAvailable())
                {
                    _logger.LogWarning(
                        "security:mfa-required is '{mfaRequired}', but the MFA storage of the subscriber database is not available. " +
                        "Subscribers can log in WITHOUT a second factor until the subscriber database is migrated (run the Setup page to create table webgis_api_subscriber_mfa).",
                        _options.MfaRequired);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Checking the MFA storage of the subscriber database failed");
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
