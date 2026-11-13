#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Core.AppCode.Services.Security;

/// <summary>
/// Logs a summary of the security report at startup (settings that deviate from the recommendation).
/// Details: /SecurityReport
/// </summary>
public class SecurityReportStartupService : IHostedService
{
    public const string SecurityAuditLogCategory = "WebGIS.Security.Audit";

    private readonly SecurityReportService _report;
    private readonly ILogger _logger;

    public SecurityReportStartupService(SecurityReportService report, ILoggerFactory loggerFactory)
    {
        _report = report;
        _logger = loggerFactory.CreateLogger(SecurityAuditLogCategory);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var report = _report.CreateReport();

            Log(report, internet: true);
            Log(report, internet: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Creating the security report summary failed");
        }

        return Task.CompletedTask;
    }

    private void Log(SecurityReport report, bool internet)
    {
        var profile = internet ? "internet" : "intranet";

        var critical = report.Rows
            .Where(r => (internet ? r.Internet : r.Intranet) == SecurityStatus.Critical)
            .Select(r => r.Rule.Key).ToArray();
        var warnings = report.Rows
            .Where(r => (internet ? r.Internet : r.Intranet) == SecurityStatus.Warning)
            .Select(r => r.Rule.Key).ToArray();

        if (critical.Length > 0)
        {
            _logger.LogWarning("Security report ({profile} installation): {count} high-risk finding(s): {keys}. See /SecurityReport and the Security annex of the admin documentation.",
                               profile, critical.Length, String.Join(", ", critical));
        }

        if (warnings.Length > 0)
        {
            _logger.LogInformation("Security report ({profile} installation): {count} medium-risk finding(s): {keys}.",
                                   profile, warnings.Length, String.Join(", ", warnings));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
