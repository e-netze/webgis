#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Api.Core.AppCode.Services.Security;

public enum SecurityCategory
{
    CompatibilityOptOut,
    FeatureEnablement,
    FineTuning
}

public enum SecurityRisk
{
    High,
    Medium,
    Low,
    Info
}

/// <summary>Result of a rule evaluation as returned by a rule's evaluator.</summary>
public enum SecurityVerdict
{
    Ok,
    Deviation,
    Hint,
    NotApplicable
}

/// <summary>Traffic light state shown in the report (per profile).</summary>
public enum SecurityStatus
{
    Ok,
    Warning,
    Critical,
    Hint,
    NotApplicable
}

public class SecurityRuleContext
{
    private readonly Func<string, string?> _read;

    public SecurityRuleContext(Func<string, string?> read, bool loginEnabled)
    {
        _read = read;
        LoginEnabled = loginEnabled;
    }

    public bool LoginEnabled { get; }

    public string? Raw(string key)
    {
        var value = _read(key);
        return String.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public bool IsSet(string key) => Raw(key) != null;

    public string Str(string key, string defaultValue) => Raw(key) ?? defaultValue;

    public bool Bool(string key, bool defaultValue)
        => bool.TryParse(Raw(key), out var b) ? b : defaultValue;

    public long Int(string key, long defaultValue)
        => long.TryParse(Raw(key), out var l) ? l : defaultValue;

    public bool Is(string key, string defaultValue, params string[] values)
        => values.Contains(Str(key, defaultValue), StringComparer.OrdinalIgnoreCase);
}

public class SecurityRule
{
    public SecurityRule(string key,
                        string defaultValue,
                        SecurityCategory category,
                        SecurityRisk risk,
                        string annexAnchor,
                        Func<SecurityRuleContext, SecurityVerdict> internet,
                        Func<SecurityRuleContext, SecurityVerdict> intranet,
                        bool secret = false,
                        bool requiresLogin = false)
    {
        Key = key;
        DefaultValue = defaultValue;
        Category = category;
        Risk = risk;
        AnnexAnchor = annexAnchor;
        Internet = internet;
        Intranet = intranet;
        Secret = secret;
        RequiresLogin = requiresLogin;
    }

    /// <summary>Key as written in api.config (<c>section:key</c>), without the leading <c>Api:</c>.</summary>
    public string Key { get; }
    public string DefaultValue { get; }
    public SecurityCategory Category { get; }
    public SecurityRisk Risk { get; }
    public string AnnexAnchor { get; }
    public bool Secret { get; }

    /// <summary>The rule only matters if allow-subscriber-login is enabled.</summary>
    public bool RequiresLogin { get; }

    public Func<SecurityRuleContext, SecurityVerdict> Internet { get; }
    public Func<SecurityRuleContext, SecurityVerdict> Intranet { get; }

    public SecurityStatus ToStatus(SecurityVerdict verdict, SecurityRuleContext context)
    {
        if (RequiresLogin && !context.LoginEnabled)
        {
            return SecurityStatus.NotApplicable;
        }

        return verdict switch
        {
            SecurityVerdict.Ok => SecurityStatus.Ok,
            SecurityVerdict.NotApplicable => SecurityStatus.NotApplicable,
            SecurityVerdict.Hint => SecurityStatus.Hint,
            _ => Risk switch
            {
                SecurityRisk.High => SecurityStatus.Critical,
                SecurityRisk.Medium => SecurityStatus.Warning,
                _ => SecurityStatus.Hint
            }
        };
    }
}

public class SecurityReportRow
{
    public SecurityRule Rule { get; init; } = default!;

    public bool IsSet { get; init; }

    /// <summary>Display value; null for secrets (only shown as set / not set) and unset keys.</summary>
    public string? Value { get; init; }

    public SecurityStatus Internet { get; init; }
    public SecurityStatus Intranet { get; init; }
}

public class SecurityReport
{
    public IReadOnlyList<SecurityReportRow> Rows { get; init; } = Array.Empty<SecurityReportRow>();

    public bool LoginEnabled { get; init; }

    public bool HasEndpointCredentials { get; init; }

    public IReadOnlyList<SubscriberSecurityRow>? Subscribers { get; set; }

    public bool SubscribersError { get; set; }

    public int Count(bool internet, SecurityStatus status)
        => Rows.Count(r => (internet ? r.Internet : r.Intranet) == status);
}
