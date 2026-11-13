#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using E.Standard.Configuration.Services;
using E.Standard.WebApp.Options;

using Microsoft.Extensions.Options;

namespace Api.Core.AppCode.Services.Security;

/// <summary>
/// Evaluates <see cref="SecurityReportRules"/> against the current api.config.
/// </summary>
public class SecurityReportService
{
    private readonly ConfigurationService _config;
    private readonly SecurityOptions _securityOptions;

    public SecurityReportService(ConfigurationService config, IOptions<SecurityOptions> securityOptions)
    {
        _config = config;
        _securityOptions = securityOptions.Value;
    }

    public bool HasEndpointCredentials
        => !String.IsNullOrEmpty(_securityOptions.EndpointAuthorizationUrlPassword)
           || (!String.IsNullOrEmpty(_securityOptions.EndpointAuthorizationBasicUsername)
               && !String.IsNullOrEmpty(_securityOptions.EndpointAuthorizationBasicPassword));

    public SecurityReport CreateReport()
    {
        string? Read(string key) => _config[$"Api:{key}"];

        var loginEnabled = bool.TryParse(Read("allow-subscriber-login"), out var login) && login;
        var context = new SecurityRuleContext(Read, loginEnabled);

        var rows = SecurityReportRules.Rules.Select(rule =>
        {
            var isSet = context.IsSet(rule.Key);

            return new SecurityReportRow
            {
                Rule = rule,
                IsSet = isSet,
                Value = isSet && !rule.Secret ? context.Raw(rule.Key) : null,
                Internet = rule.ToStatus(rule.Internet(context), context),
                Intranet = rule.ToStatus(rule.Intranet(context), context)
            };
        }).ToArray();

        return new SecurityReport
        {
            Rows = rows,
            LoginEnabled = loginEnabled,
            HasEndpointCredentials = HasEndpointCredentials
        };
    }
}
