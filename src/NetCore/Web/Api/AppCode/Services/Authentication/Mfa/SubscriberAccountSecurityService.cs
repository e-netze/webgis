#nullable enable

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Api.Core.AppCode.Extensions;

using E.Standard.Api.App.Configuration;
using E.Standard.Configuration.Services;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.SubscriberDatabase;
using E.Standard.WebGIS.SubscriberDatabase.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using QRCoder;

namespace Api.Core.AppCode.Services.Authentication.Mfa;

public enum MfaCodeVerification
{
    Invalid,
    Totp,
    RecoveryCode
}

public record MfaSetupData(string Secret, string OtpAuthUri, string QrCodeSvg);

/// <summary>
/// Encrypted, short-lived state between the steps of the MFA login/setup (no server side session needed).
/// </summary>
public record MfaFlowToken(string Stage, string SubscriberId, string Stamp, long IssuedUnix, string Nonce, string Secret, string Redirect, string Flags)
{
    public const string StageVerify = "verify";     // password ok, TOTP/recovery code required
    public const string StageEnroll = "enroll";     // password ok, MFA required but not set up yet
    public const string StageDone = "done";         // enrollment done, recovery codes shown => complete login
    public const string StageSetup = "setup";       // logged in subscriber sets up MFA
    public const string StageChangePassword = "change-password";  // password ok (+ second factor), a new password is required

    public const string FlagPasswordChangeRequired = "pwchange";   // the login password must be changed (known default password)
    public const string FlagSecondFactorDone = "mfadone";          // the second factor was already verified in this login flow

    public bool HasFlag(string flag) => (Flags ?? String.Empty).Split(',').Contains(flag);
}

/// <summary>
/// Security related subscriber account functions: security stamp (session invalidation) and
/// two-factor authentication (TOTP + recovery codes).
/// The data is stored in an optional <see cref="ISubscriberSecurityStore"/>. If it is not available
/// (SQL: table missing, Setup not run yet), MFA is reported as unavailable and login works as before.
/// </summary>
public class SubscriberAccountSecurityService
{
    public const int RecoveryCodeCount = 10;
    public static readonly TimeSpan FlowTokenLifetime = TimeSpan.FromMinutes(10);

    private const string FlowTokenVersion = "mfa2";
    private const string RecoveryCodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";   // no 0/O, 1/I/L

    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly ICryptoService _crypto;
    private readonly TotpService _totp;
    private readonly SubscriberSecurityStampCache _stampCache;
    private readonly ConfigurationService _config;
    private readonly SubscriberSecurityOptions _options;
    private readonly ILogger<SubscriberAccountSecurityService> _logger;

    public SubscriberAccountSecurityService(SubscriberDatabaseService subscriberDb,
                                            ICryptoService crypto,
                                            TotpService totp,
                                            SubscriberSecurityStampCache stampCache,
                                            ConfigurationService config,
                                            IOptions<SubscriberSecurityOptions> options,
                                            ILogger<SubscriberAccountSecurityService> logger)
    {
        _subscriberDb = subscriberDb;
        _crypto = crypto;
        _totp = totp;
        _stampCache = stampCache;
        _config = config;
        _options = options.Value;
        _logger = logger;
    }

    #region Availability / Policy

    public bool IsMfaAvailable() => GetAvailableStore() != null;

    public bool IsMfaRequired(SubscriberDb.Subscriber subscriber)
        => _options.MfaRequired switch
        {
            MfaRequiredMode.All => true,
            MfaRequiredMode.Admins => subscriber.IsAdminSubscriber(_config),
            _ => false
        };

    /// <summary>Configured as required, but can't be enforced because the storage is missing.</summary>
    public bool IsMfaRequiredButUnavailable(SubscriberDb.Subscriber? subscriber = null)
        => (subscriber != null ? IsMfaRequired(subscriber) : _options.MfaRequired != MfaRequiredMode.None)
           && !IsMfaAvailable();

    #endregion

    #region Security Info

    /// <summary>Null if the MFA storage is not available.</summary>
    public SubscriberSecurityInfo? GetSecurityInfo(string subscriberId)
    {
        var store = GetAvailableStore();
        if (store == null)
        {
            return null;
        }

        return store.GetSubscriberSecurity(subscriberId) ?? new SubscriberSecurityInfo() { SubscriberId = subscriberId };
    }

    public bool IsMfaEnabled(string subscriberId) => GetSecurityInfo(subscriberId)?.MfaEnabled == true;

    public int RecoveryCodesLeft(SubscriberSecurityInfo? info) => SplitRecoveryCodes(info?.RecoveryCodeHashes).Length;

    #endregion

    #region Security Stamp

    /// <summary>
    /// The current security stamp of a subscriber ("" if none was set yet).
    /// Null means: can't be checked (storage not available or error) => cookies are not validated against a stamp.
    /// </summary>
    public string? GetSecurityStamp(string subscriberId)
    {
        if (_stampCache.TryGet(subscriberId, out var cachedStamp))
        {
            return cachedStamp;
        }

        string? stamp;
        try
        {
            var store = GetAvailableStore();
            stamp = store == null
                ? null
                : store.GetSubscriberSecurity(subscriberId)?.SecurityStamp ?? String.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Can't read subscriber security stamp, session is not validated against it");
            stamp = null;
        }

        _stampCache.Set(subscriberId, stamp);
        return stamp;
    }

    /// <summary>
    /// Invalidates all existing sessions of the subscriber. Returns the new stamp or null if the storage is not available.
    /// </summary>
    public string? RotateSecurityStamp(string subscriberId)
        => Update(subscriberId, info => { });

    #endregion

    #region MFA Setup

    public MfaSetupData CreateSetupData(SubscriberDb.Subscriber subscriber, string? secret = null)
    {
        secret ??= _totp.GenerateSecret();
        string uri = _totp.GetOtpAuthUri(_options.MfaIssuer, subscriber.Name, secret);

        using var qrCodeData = QRCodeGenerator.GenerateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        using var svgRenderer = new SvgQRCode(qrCodeData);

        return new MfaSetupData(secret, uri, svgRenderer.GetGraphic(4));
    }

    /// <summary>
    /// Enables MFA, if the code matches the new secret. Returns the recovery codes (shown once) or null if the code is invalid.
    /// </summary>
    public string[]? EnableMfa(string subscriberId, string secret, string? code)
    {
        long? step = _totp.VerifyCode(secret, code, 0);
        if (step == null)
        {
            return null;
        }

        string[] recoveryCodes = GenerateRecoveryCodes();

        Update(subscriberId, info =>
        {
            info.MfaEnabled = true;
            info.TotpSecret = _crypto.EncryptTextDefault(secret, CryptoResultStringType.Base64);
            info.LastTotpTimeStep = step.Value;
            info.RecoveryCodeHashes = String.Join(",", recoveryCodes.Select(c => HashRecoveryCode(subscriberId, c)));
        });

        return recoveryCodes;
    }

    public void DisableMfa(string subscriberId)
        => Update(subscriberId, info =>
        {
            info.MfaEnabled = false;
            info.TotpSecret = null;
            info.RecoveryCodeHashes = null;
            info.LastTotpTimeStep = 0;
        });

    public string[] RegenerateRecoveryCodes(string subscriberId)
    {
        string[] recoveryCodes = GenerateRecoveryCodes();

        Update(subscriberId,
               info => info.RecoveryCodeHashes = String.Join(",", recoveryCodes.Select(c => HashRecoveryCode(subscriberId, c))),
               rotateStamp: false);

        return recoveryCodes;
    }

    #endregion

    #region MFA Login

    /// <summary>
    /// Verifies a TOTP code or a recovery code (recovery codes are consumed).
    /// </summary>
    public MfaCodeVerification VerifyLoginCode(string subscriberId, string? code)
    {
        var store = GetAvailableStore();
        var info = store?.GetSubscriberSecurity(subscriberId);

        if (store == null || info?.MfaEnabled != true || String.IsNullOrWhiteSpace(code))
        {
            return MfaCodeVerification.Invalid;
        }

        if (!String.IsNullOrEmpty(info.TotpSecret))
        {
            string secret;
            try
            {
                secret = _crypto.DecryptTextDefault(info.TotpSecret);
            }
            catch (Exception ex)
            {
                // e.g. the crypto key has changed => an admin has to reset MFA for this subscriber
                _logger.LogError(ex, "Can't decrypt the TOTP secret of subscriber {subscriberId}", subscriberId);
                secret = String.Empty;
            }

            if (!String.IsNullOrEmpty(secret))
            {
                long? step = _totp.VerifyCode(secret, code, info.LastTotpTimeStep);
                if (step != null)
                {
                    info.LastTotpTimeStep = step.Value;
                    store.SetSubscriberSecurity(info);
                    return MfaCodeVerification.Totp;
                }
            }
        }

        string hash = HashRecoveryCode(subscriberId, code);
        var hashes = SplitRecoveryCodes(info.RecoveryCodeHashes);
        var matching = hashes.FirstOrDefault(h => CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(h), Encoding.ASCII.GetBytes(hash)));

        if (matching != null)
        {
            info.RecoveryCodeHashes = String.Join(",", hashes.Where(h => h != matching));
            store.SetSubscriberSecurity(info);
            return MfaCodeVerification.RecoveryCode;
        }

        return MfaCodeVerification.Invalid;
    }

    #endregion

    #region Flow Token

    public string CreateFlowToken(string stage, string subscriberId, string? secret = null, string? redirect = null, params string[] flags)
    {
        string payload = String.Join("|",
            FlowTokenVersion,
            stage,
            subscriberId,
            GetSecurityStamp(subscriberId) ?? String.Empty,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Guid.NewGuid().ToString("N"),
            secret ?? String.Empty,
            String.Join(",", flags.Where(f => !String.IsNullOrEmpty(f))),
            redirect ?? String.Empty);   // last: may contain any characters

        return _crypto.EncryptTextDefault(payload, CryptoResultStringType.Base64);
    }

    /// <summary>
    /// Returns null if the token is invalid, expired, for another stage or the security stamp has changed meanwhile.
    /// </summary>
    public MfaFlowToken? ReadFlowToken(string? token, string expectedStage)
    {
        if (String.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var parts = _crypto.DecryptTextDefault(token).Split('|', 9);
            if (parts.Length != 9
                || parts[0] != FlowTokenVersion
                || parts[1] != expectedStage
                || String.IsNullOrEmpty(parts[2])
                || !long.TryParse(parts[4], out long issued))
            {
                return null;
            }

            long age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - issued;
            if (age < 0 || age > FlowTokenLifetime.TotalSeconds)
            {
                return null;
            }

            _stampCache.Invalidate(parts[2]);
            if ((GetSecurityStamp(parts[2]) ?? String.Empty) != parts[3])
            {
                return null;
            }

            return new MfaFlowToken(parts[1], parts[2], parts[3], issued, parts[5], parts[6], parts[8], parts[7]);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Helper

    private ISubscriberSecurityStore? GetAvailableStore()
    {
        try
        {
            return _subscriberDb.CreateInstance() is ISubscriberSecurityStore store && store.IsSubscriberSecurityStoreAvailable()
                ? store
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Subscriber security store not available");
            return null;
        }
    }

    private string? Update(string subscriberId, Action<SubscriberSecurityInfo> apply, bool rotateStamp = true)
    {
        var store = GetAvailableStore();
        if (store == null)
        {
            return null;
        }

        var info = store.GetSubscriberSecurity(subscriberId) ?? new SubscriberSecurityInfo() { SubscriberId = subscriberId };
        info.SubscriberId = subscriberId;

        apply(info);

        if (rotateStamp)
        {
            info.SecurityStamp = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        }

        store.SetSubscriberSecurity(info);
        _stampCache.Invalidate(subscriberId);

        return info.SecurityStamp ?? String.Empty;
    }

    private static string[] GenerateRecoveryCodes()
        => Enumerable.Range(0, RecoveryCodeCount)
                     .Select(_ => $"{RandomString(5)}-{RandomString(5)}")
                     .ToArray();

    private static string RandomString(int length)
    {
        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = RecoveryCodeAlphabet[RandomNumberGenerator.GetInt32(RecoveryCodeAlphabet.Length)];
        }
        return new string(chars);
    }

    private static string HashRecoveryCode(string subscriberId, string code)
    {
        string normalized = code.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{subscriberId}:{normalized}")));
    }

    private static string[] SplitRecoveryCodes(string? hashes)
        => (hashes ?? String.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    #endregion
}
