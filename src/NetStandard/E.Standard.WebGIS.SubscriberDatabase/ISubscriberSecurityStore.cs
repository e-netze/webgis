using System;

namespace E.Standard.WebGIS.SubscriberDatabase;

/// <summary>
/// Optional storage for security related subscriber data (MFA, security stamp).
/// Kept separate from the subscriber record, so existing SQL subscriber tables never need to be altered:
/// for SQL databases the data lives in an own table, which is only created by the Setup page.
/// </summary>
public interface ISubscriberSecurityStore
{
    /// <summary>
    /// False if the storage does not exist (yet), e.g. the SQL table was not created via Setup.
    /// Callers must then treat MFA as unavailable instead of failing.
    /// </summary>
    bool IsSubscriberSecurityStoreAvailable();

    SubscriberSecurityInfo GetSubscriberSecurity(string subscriberId);

    /// <summary>Inserts or updates the record of <see cref="SubscriberSecurityInfo.SubscriberId"/>.</summary>
    bool SetSubscriberSecurity(SubscriberSecurityInfo info);
}

public class SubscriberSecurityInfo
{
    public string SubscriberId { get; set; }

    /// <summary>Changes whenever the password or the MFA settings change → invalidates existing sessions.</summary>
    public string SecurityStamp { get; set; }

    public bool MfaEnabled { get; set; }

    /// <summary>TOTP secret, encrypted by the caller (never stored in plain text).</summary>
    public string TotpSecret { get; set; }

    /// <summary>Comma separated hashes of the unused recovery codes.</summary>
    public string RecoveryCodeHashes { get; set; }

    /// <summary>Last accepted TOTP time step, prevents replaying a code.</summary>
    public long LastTotpTimeStep { get; set; }

    public DateTime Changed { get; set; }
}
