#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using Api.Core.AppCode.Services.Authentication.Mfa;

using E.Standard.Api.App.Extensions;
using E.Standard.Configuration.Services;
using E.Standard.WebGIS.SubscriberDatabase.Services;

namespace Api.Core.AppCode.Services.Security;

public enum SubscriberMfaState
{
    Enabled,
    NotEnabled,
    Unavailable
}

public record SubscriberSecurityRow(string Name, bool IsAdmin, bool MfaRequired, SubscriberMfaState Mfa, int RecoveryCodesLeft);

/// <summary>
/// Lists all subscribers with their security state (2FA). No personal data beyond the user name, no secrets.
/// </summary>
public class SubscriberSecurityOverviewService
{
    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly SubscriberAccountSecurityService _accountSecurity;
    private readonly ConfigurationService _config;

    public SubscriberSecurityOverviewService(SubscriberDatabaseService subscriberDb,
                                             SubscriberAccountSecurityService accountSecurity,
                                             ConfigurationService config)
    {
        _subscriberDb = subscriberDb;
        _accountSecurity = accountSecurity;
        _config = config;
    }

    /// <summary>Null if no subscriber database is configured.</summary>
    public IReadOnlyList<SubscriberSecurityRow>? GetSubscribers()
    {
        var db = _subscriberDb.CreateInstance();
        if (db == null)
        {
            return null;
        }

        var mfaAvailable = _accountSecurity.IsMfaAvailable();

        return db.GetSubscribers()
                 .OrderByDescending(s => _config.IsAdminSubscriber(s.Name))
                 .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                 .Select(s =>
                 {
                     var info = mfaAvailable ? _accountSecurity.GetSecurityInfo(s.Id) : null;

                     return new SubscriberSecurityRow(
                         s.Name,
                         _config.IsAdminSubscriber(s.Name),
                         _accountSecurity.IsMfaRequired(s),
                         !mfaAvailable ? SubscriberMfaState.Unavailable
                                       : info?.MfaEnabled == true ? SubscriberMfaState.Enabled : SubscriberMfaState.NotEnabled,
                         info?.MfaEnabled == true ? _accountSecurity.RecoveryCodesLeft(info) : 0);
                 })
                 .ToArray();
    }
}
