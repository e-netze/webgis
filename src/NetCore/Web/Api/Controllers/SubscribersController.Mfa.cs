using System;
using System.Threading.Tasks;

using Api.Core.AppCode.Exceptions;
using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Reflection;
using Api.Core.AppCode.Services.Authentication.Mfa;

using E.Standard.Api.App;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Models;
using E.Standard.Api.App.Reflection;
using E.Standard.Security.App.Exceptions;
using E.Standard.Security.Cryptography.Services;
using E.Standard.WebApp.Reflection;
using E.Standard.WebGIS.SubscriberDatabase;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Api.Core.Controllers;

/// <summary>
/// Two-factor authentication (TOTP) for the subscriber forms login and the account pages.
/// </summary>
public partial class SubscribersController
{
    internal const string MfaErrorInvalidCode = "invalid-code";
    internal const string MfaErrorExpired = "expired";
    internal const string MfaErrorInvalidPassword = "invalid-password";
    internal const string MfaErrorNotAllowed = "not-allowed";
    internal const string AccountErrorPasswordsNotEqual = "passwords-not-equal";

    #region Login (second step)

    [HttpPost]
    [AppRole(AppRoles.All)]
    [ConfigurableValidateAntiforgeryToken]
    async public Task<IActionResult> LoginMfa(ApiSubscribersMfaLogin model)
    {
        if (_config.AllowSubscriberLogin() == false)
        {
            return RedirectToActionResult("Index", "Home");
        }

        model ??= new ApiSubscribersMfaLogin();
        string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        bool enroll = model.Mode == ApiSubscribersMfaLogin.ModeEnroll;

        var token = _accountSecurity.ReadFlowToken(model.Token, enroll ? MfaFlowToken.StageEnroll : MfaFlowToken.StageVerify);
        var subscriber = token != null ? _subscriberDb.CreateInstance(_logger).GetSubscriberById(token.SubscriberId) : null;
        if (subscriber == null)
        {
            return await LoginErrorResult(MfaErrorExpired);
        }

        var throttleStatus = _loginThrottle.GetStatus(subscriber.Name, clientIp);
        if (throttleStatus.IsLocked)
        {
            return await MfaErrorResult(model, subscriber, token, LoginErrorTooManyAttemptsMinutes, LockedMinutes(throttleStatus.LockedSeconds));
        }

        if (enroll)
        {
            var recoveryCodes = _accountSecurity.EnableMfa(subscriber.Id, token.Secret, model.Code);
            if (recoveryCodes == null)
            {
                return await MfaErrorResult(model, subscriber, token, MfaErrorInvalidCode);
            }

            _auditLogger.LogInformation("Subscriber MFA enabled (login enrollment): {username} from {clientIp}", subscriber.Name, clientIp);

            // the security stamp has changed => new token
            return ViewResult("MfaRecoveryCodes", new ApiSubscribersMfaRecoveryCodes()
            {
                RecoveryCodes = recoveryCodes,
                ContinueToken = _accountSecurity.CreateFlowToken(MfaFlowToken.StageDone, subscriber.Id, redirect: token.Redirect)
            });
        }

        var verification = _accountSecurity.VerifyLoginCode(subscriber.Id, model.Code);
        if (verification == MfaCodeVerification.Invalid)
        {
            _loginThrottle.RegisterFailure(subscriber.Name, clientIp);
            _auditLogger.LogWarning("Subscriber login failed (invalid second factor): {username} from {clientIp}", subscriber.Name, clientIp);

            throttleStatus = _loginThrottle.GetStatus(subscriber.Name, clientIp);
            if (throttleStatus.IsLocked)
            {
                _auditLogger.LogWarning("Subscriber login locked: {username} from {clientIp} for {seconds} seconds", subscriber.Name, clientIp, throttleStatus.LockedSeconds);
                return await MfaErrorResult(model, subscriber, token, LoginErrorTooManyAttemptsMinutes, LockedMinutes(throttleStatus.LockedSeconds));
            }

            return await MfaErrorResult(model, subscriber, token, MfaErrorInvalidCode);
        }

        if (!_loginThrottle.TryConsumeOneTimeNonce(token.Nonce, SubscriberAccountSecurityService.FlowTokenLifetime))
        {
            return await LoginErrorResult(MfaErrorExpired);
        }

        if (verification == MfaCodeVerification.RecoveryCode)
        {
            _auditLogger.LogWarning("Subscriber used a MFA recovery code: {username} from {clientIp}", subscriber.Name, clientIp);
        }

        return await ContinueLogin(subscriber, token.Redirect, clientIp,
                                   passwordChangeRequired: token.HasFlag(MfaFlowToken.FlagPasswordChangeRequired),
                                   secondFactorDone: true);
    }

    [HttpPost]
    [AppRole(AppRoles.All)]
    [ConfigurableValidateAntiforgeryToken]
    async public Task<IActionResult> LoginMfaContinue(string continueToken)
    {
        if (_config.AllowSubscriberLogin() == false)
        {
            return RedirectToActionResult("Index", "Home");
        }

        var token = _accountSecurity.ReadFlowToken(continueToken, MfaFlowToken.StageDone);
        var subscriber = token != null ? _subscriberDb.CreateInstance(_logger).GetSubscriberById(token.SubscriberId) : null;

        if (subscriber == null || !_loginThrottle.TryConsumeOneTimeNonce(token.Nonce, SubscriberAccountSecurityService.FlowTokenLifetime))
        {
            return await LoginErrorResult(MfaErrorExpired);
        }

        return await CompleteLogin(subscriber, token.Redirect, HttpContext.Connection.RemoteIpAddress?.ToString());
    }

    /// <summary>
    /// Forced password change during the login (known default password, e.g. "author" of the initial installation).
    /// The auth cookie is only issued after the password has been changed.
    /// </summary>
    [HttpPost]
    [AppRole(AppRoles.All)]
    [ConfigurableValidateAntiforgeryToken]
    async public Task<IActionResult> LoginChangePassword(ApiSubscribersLoginChangePassword model)
    {
        if (_config.AllowSubscriberLogin() == false)
        {
            return RedirectToActionResult("Index", "Home");
        }

        model ??= new ApiSubscribersLoginChangePassword();
        string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        var token = _accountSecurity.ReadFlowToken(model.Token, MfaFlowToken.StageChangePassword);
        var db = _subscriberDb.CreateInstance(_logger);
        var subscriber = token != null ? db.GetSubscriberById(token.SubscriberId) : null;
        if (subscriber == null)
        {
            return await LoginErrorResult(MfaErrorExpired);
        }

        if (model.NewPassword1 != model.NewPassword2)
        {
            return LoginChangePasswordError(model, AccountErrorPasswordsNotEqual);
        }

        var passwordPolicy = _passwordPolicy.Validate(model.NewPassword1, subscriber.Name, subscriber.FirstName, subscriber.LastName, subscriber.Email);
        if (!passwordPolicy.IsValid)
        {
            return LoginChangePasswordError(model, passwordPolicy.ErrorKey, passwordPolicy.ErrorArgument);
        }

        if (!_loginThrottle.TryConsumeOneTimeNonce(token.Nonce, SubscriberAccountSecurityService.FlowTokenLifetime))
        {
            return await LoginErrorResult(MfaErrorExpired);
        }

        db.UpdateApiSubscriberPassword(subscriber, model.NewPassword1);
        _auditLogger.LogInformation("Subscriber password changed (required at login): {username} from {clientIp}", subscriber.Name, clientIp);

        // terminate all other sessions that may have used the default password
        _accountSecurity.RotateSecurityStamp(subscriber.Id);

        return await ContinueLogin(subscriber, token.Redirect, clientIp,
                                   passwordChangeRequired: false,
                                   secondFactorDone: token.HasFlag(MfaFlowToken.FlagSecondFactorDone));
    }

    #endregion

    #region Account

    public IActionResult Account()
    {
        try
        {
            return ViewResult(CreateAccountModel(CurrentAuthSubscriber()));
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
    }

    public IActionResult Mfa() => RedirectToActionResult("Account");

    /// <summary>
    /// A subscriber changes its own password (current password required, throttled).
    /// Admins change the passwords of other subscribers via Update/ChangePassword.
    /// </summary>
    [HttpPost]
    [ConfigurableValidateAntiforgeryToken]
    public IActionResult AccountChangePassword(ApiSubscribersAccount model)
    {
        try
        {
            var subscriber = CurrentAuthSubscriber();
            model ??= new ApiSubscribersAccount();

            if (model.NewPassword1 != model.NewPassword2)
            {
                return AccountError(subscriber, AccountErrorPasswordsNotEqual);
            }

            var passwordPolicy = _passwordPolicy.Validate(model.NewPassword1, subscriber.Name, subscriber.FirstName, subscriber.LastName, subscriber.Email);
            if (!passwordPolicy.IsValid)
            {
                return AccountError(subscriber, passwordPolicy.ErrorKey, passwordPolicy.ErrorArgument);
            }

            if (!VerifyCurrentPassword(subscriber, model.CurrentPassword))
            {
                return AccountError(subscriber, MfaErrorInvalidPassword);
            }

            var db = _subscriberDb.CreateInstance();
            db.UpdateApiSubscriberPassword(db.GetSubscriberById(subscriber.Id), model.NewPassword1);

            _auditLogger.LogInformation("Subscriber password changed: {username} by {changedBy} from {clientIp}", subscriber.Name, subscriber.Name, HttpContext.Connection.RemoteIpAddress?.ToString());

            // terminate all other sessions of this subscriber, keep the current one
            _accountSecurity.RotateSecurityStamp(subscriber.Id);
            AuthenticateSubscriber(subscriber);

            var account = CreateAccountModel(subscriber);
            account.SuccessMessage = "password-changed";

            return ViewResult("Account", account);
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
    }

    public IActionResult MfaSetup()
    {
        try
        {
            var subscriber = CurrentAuthSubscriber();

            if (!_accountSecurity.IsMfaAvailable() || _accountSecurity.IsMfaEnabled(subscriber.Id))
            {
                return RedirectToActionResult("Account");
            }

            var setup = _accountSecurity.CreateSetupData(subscriber);

            return ViewResult("LoginMfa", new ApiSubscribersMfaLogin()
            {
                Mode = ApiSubscribersMfaLogin.ModeSetup,
                Token = _accountSecurity.CreateFlowToken(MfaFlowToken.StageSetup, subscriber.Id, secret: setup.Secret),
                QrCodeSvg = setup.QrCodeSvg,
                Secret = setup.Secret
            });
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
    }

    [HttpPost]
    [ConfigurableValidateAntiforgeryToken]
    public IActionResult MfaSetup(ApiSubscribersMfaLogin model)
    {
        try
        {
            var subscriber = CurrentAuthSubscriber();

            var token = _accountSecurity.ReadFlowToken(model?.Token, MfaFlowToken.StageSetup);
            if (token == null || token.SubscriberId != subscriber.Id)
            {
                return RedirectToActionResult("MfaSetup");
            }

            var recoveryCodes = _accountSecurity.EnableMfa(subscriber.Id, token.Secret, model.Code);
            if (recoveryCodes == null)
            {
                var setup = _accountSecurity.CreateSetupData(subscriber, token.Secret);

                model.Code = null;
                model.Mode = ApiSubscribersMfaLogin.ModeSetup;
                model.QrCodeSvg = setup.QrCodeSvg;
                model.Secret = setup.Secret;
                model.ErrorMessage = MfaErrorInvalidCode;

                return ViewResult("LoginMfa", model);
            }

            _auditLogger.LogInformation("Subscriber MFA enabled: {username} from {clientIp}", subscriber.Name, HttpContext.Connection.RemoteIpAddress?.ToString());

            // security stamp changed (other sessions are terminated) => renew the own cookie
            AuthenticateSubscriber(subscriber);

            return ViewResult("MfaRecoveryCodes", new ApiSubscribersMfaRecoveryCodes() { RecoveryCodes = recoveryCodes });
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
    }

    [HttpPost]
    [ConfigurableValidateAntiforgeryToken]
    public IActionResult MfaDisable(ApiSubscribersAccount model)
    {
        try
        {
            var subscriber = CurrentAuthSubscriber();

            if (_accountSecurity.IsMfaRequired(subscriber))
            {
                return AccountError(subscriber, MfaErrorNotAllowed);
            }

            if (!VerifyCurrentPassword(subscriber, model?.CurrentPassword))
            {
                return AccountError(subscriber, MfaErrorInvalidPassword);
            }

            _accountSecurity.DisableMfa(subscriber.Id);
            _auditLogger.LogWarning("Subscriber MFA disabled: {username} from {clientIp}", subscriber.Name, HttpContext.Connection.RemoteIpAddress?.ToString());

            AuthenticateSubscriber(subscriber);

            return RedirectToActionResult("Account");
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
    }

    [HttpPost]
    [ConfigurableValidateAntiforgeryToken]
    public IActionResult MfaRegenerateRecoveryCodes(ApiSubscribersAccount model)
    {
        try
        {
            var subscriber = CurrentAuthSubscriber();

            if (!_accountSecurity.IsMfaEnabled(subscriber.Id))
            {
                return RedirectToActionResult("Account");
            }

            if (!VerifyCurrentPassword(subscriber, model?.CurrentPassword))
            {
                return AccountError(subscriber, MfaErrorInvalidPassword);
            }

            var recoveryCodes = _accountSecurity.RegenerateRecoveryCodes(subscriber.Id);
            _auditLogger.LogInformation("Subscriber MFA recovery codes regenerated: {username} from {clientIp}", subscriber.Name, HttpContext.Connection.RemoteIpAddress?.ToString());

            return ViewResult("MfaRecoveryCodes", new ApiSubscribersMfaRecoveryCodes() { RecoveryCodes = recoveryCodes });
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
    }

    /// <summary>
    /// Admin: resets MFA of another subscriber (e.g. lost device) and terminates its sessions.
    /// </summary>
    [HttpPost]
    [ConfigurableValidateAntiforgeryToken]
    public IActionResult MfaReset(string id)
    {
        try
        {
            var currentSubscriber = CurrentAuthSubscriber();
            if (!currentSubscriber.IsAdminSubscriber(_config) || String.IsNullOrWhiteSpace(id) || id == currentSubscriber.Id)
            {
                return RedirectToActionResult("Index");
            }

            var subscriber = _subscriberDb.CreateInstance().GetSubscriberById(id);
            if (subscriber == null)
            {
                return RedirectToActionResult("AdminSubscribers");
            }

            _accountSecurity.DisableMfa(subscriber.Id);
            _auditLogger.LogWarning("Subscriber MFA reset: {username} by {changedBy} from {clientIp}", subscriber.Name, currentSubscriber.Name, HttpContext.Connection.RemoteIpAddress?.ToString());

            return RedirectToActionResult("Update", parameters: new { id = subscriber.Id });
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
    }

    #endregion

    #region Helper

    /// <summary>
    /// Password verified: second factor, forced password change, MFA enrollment - then complete the login.
    /// The login (and the reset of the failed attempts counter) is only completed after the second factor,
    /// otherwise the TOTP code could be brute forced.
    /// </summary>
    async private Task<IActionResult> ContinueLogin(SubscriberDb.Subscriber subscriber, string redirect, string clientIp, bool passwordChangeRequired, bool secondFactorDone)
    {
        bool mfaAvailable = _accountSecurity.IsMfaAvailable();

        // an enabled second factor is verified before the password change, so the password can't be changed without it
        if (!secondFactorDone && mfaAvailable && _accountSecurity.IsMfaEnabled(subscriber.Id))
        {
            _auditLogger.LogInformation("Subscriber password verified, second factor required: {username} from {clientIp}", subscriber.Name, clientIp);
            return await MfaVerifyResult(new ApiSubscribersMfaLogin()
            {
                Mode = ApiSubscribersMfaLogin.ModeVerify,
                Token = _accountSecurity.CreateFlowToken(MfaFlowToken.StageVerify, subscriber.Id, redirect: redirect,
                                                         flags: passwordChangeRequired ? MfaFlowToken.FlagPasswordChangeRequired : null)
            });
        }

        if (passwordChangeRequired)
        {
            return await LoginChangePasswordResult(new ApiSubscribersLoginChangePassword()
            {
                Token = _accountSecurity.CreateFlowToken(MfaFlowToken.StageChangePassword, subscriber.Id, redirect: redirect,
                                                         flags: secondFactorDone ? MfaFlowToken.FlagSecondFactorDone : null)
            });
        }

        if (!secondFactorDone && _accountSecurity.IsMfaRequired(subscriber))
        {
            if (mfaAvailable)
            {
                _auditLogger.LogInformation("Subscriber password verified, MFA setup required: {username} from {clientIp}", subscriber.Name, clientIp);
                return await MfaEnrollResult(subscriber, redirect);
            }

            _logger.LogWarning("MFA is required for subscriber {username}, but the MFA storage of the subscriber database is not available (run Setup). Login without second factor.", subscriber.Name);
        }

        return await CompleteLogin(subscriber, redirect, clientIp);
    }

    async private Task<IActionResult> LoginChangePasswordResult(ApiSubscribersLoginChangePassword model)
    {
        if (Request.Query["f"] == "json")
        {
            // the password change is only supported in the browser
            return await JsonObject(new { succeeded = false, password_change_required = true });
        }

        return ViewResult("LoginChangePassword", model);
    }

    private IActionResult LoginChangePasswordError(ApiSubscribersLoginChangePassword model, string errorKey, string errorArgument = null)
    {
        model.NewPassword1 = model.NewPassword2 = null;
        model.ErrorMessage = errorKey;
        model.ErrorMessageArgument = errorArgument;

        return ViewResult("LoginChangePassword", model);
    }

    /// <summary>
    /// All factors verified: reset the throttling, set the auth cookie or redirect to the portal.
    /// </summary>
    async private Task<IActionResult> CompleteLogin(SubscriberDb.Subscriber subscriber, string redirect, string clientIp)
    {
        _loginThrottle.RegisterSuccess(subscriber.Name);
        _auditLogger.LogInformation("Subscriber login succeeded: {username} from {clientIp}", subscriber.Name, clientIp);

        if (!String.IsNullOrWhiteSpace(redirect))
        {
            if (!_urlHelper.IsAllowedPortalLoginRedirect(redirect))
            {
                return BadRequest("Redirect target is not allowed");
            }

            var credentialToken = _jwtAccessTokenService.GenerateToken(subscriber.FullName, 1, JwtAccessTokenService.PortalLoginAudience);

            return RedirectResult($"{redirect}{(redirect.Contains("?") ? "&" : "?")}credential_token={credentialToken}");
        }

        AuthenticateSubscriber(subscriber);

        if (Request.Query["f"] == "json")
        {
            return await JsonObject(new { succeeded = true });
        }

        return RedirectToActionResult("Index");
    }

    async private Task<IActionResult> MfaVerifyResult(ApiSubscribersMfaLogin model)
    {
        if (Request.Query["f"] == "json")
        {
            return await JsonObject(new { succeeded = false, mfa_required = true, mfa_token = model.Token });
        }

        return ViewResult("LoginMfa", model);
    }

    async private Task<IActionResult> MfaEnrollResult(SubscriberDb.Subscriber subscriber, string redirect)
    {
        if (Request.Query["f"] == "json")
        {
            // the setup (QR code) is only supported in the browser
            return await JsonObject(new { succeeded = false, mfa_enrollment_required = true });
        }

        return MfaEnrollView(subscriber, redirect);
    }

    private IActionResult MfaEnrollView(SubscriberDb.Subscriber subscriber, string redirect)
    {
        var setup = _accountSecurity.CreateSetupData(subscriber);

        return ViewResult("LoginMfa", new ApiSubscribersMfaLogin()
        {
            Mode = ApiSubscribersMfaLogin.ModeEnroll,
            Token = _accountSecurity.CreateFlowToken(MfaFlowToken.StageEnroll, subscriber.Id, secret: setup.Secret, redirect: redirect),
            QrCodeSvg = setup.QrCodeSvg,
            Secret = setup.Secret
        });
    }

    async private Task<IActionResult> MfaErrorResult(ApiSubscribersMfaLogin model, SubscriberDb.Subscriber subscriber, MfaFlowToken token, string errorKey, string errorArgument = null)
    {
        if (Request.Query["f"] == "json")
        {
            var throttleStatus = _loginThrottle.GetStatus(subscriber.Name, HttpContext.Connection.RemoteIpAddress?.ToString());
            return await JsonObject(new { succeeded = false, mfa_required = true, mfa_token = model.Token, error = errorKey, locked_seconds = throttleStatus.LockedSeconds });
        }

        model.Code = null;
        model.ErrorMessage = errorKey;
        model.ErrorMessageArgument = errorArgument;

        if (model.Mode == ApiSubscribersMfaLogin.ModeEnroll)
        {
            var setup = _accountSecurity.CreateSetupData(subscriber, token.Secret);
            model.QrCodeSvg = setup.QrCodeSvg;
            model.Secret = setup.Secret;
        }
        else
        {
            model.Mode = ApiSubscribersMfaLogin.ModeVerify;
        }

        return ViewResult("LoginMfa", model);
    }

    async private Task<IActionResult> LoginErrorResult(string mfaErrorKey)
    {
        if (Request.Query["f"] == "json")
        {
            return await JsonObject(new { succeeded = false, error = mfaErrorKey });
        }

        // the MFA error keys are localized in the login view too (subscribers.login.error.mfa-*)
        return ViewResult("Login", new ApiSubscribersLogin() { ErrorMessage = $"mfa-{mfaErrorKey}" });
    }

    private ApiSubscribersAccount CreateAccountModel(SubscriberDb.Subscriber subscriber)
    {
        var info = TryGetSecurityInfo(subscriber.Id);

        return new ApiSubscribersAccount()
        {
            Available = info != null,
            Enabled = info?.MfaEnabled == true,
            Required = _accountSecurity.IsMfaRequired(subscriber),
            RecoveryCodesLeft = _accountSecurity.RecoveryCodesLeft(info)
        };
    }

    private IActionResult AccountError(SubscriberDb.Subscriber subscriber, string errorKey, string errorArgument = null)
    {
        var status = CreateAccountModel(subscriber);
        status.ErrorMessage = errorKey;
        status.ErrorMessageArgument = errorArgument;

        return ViewResult("Account", status);
    }

    private bool VerifyCurrentPassword(SubscriberDb.Subscriber subscriber, string password)
    {
        string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (_loginThrottle.GetStatus(subscriber.Name, clientIp).IsLocked)
        {
            return false;
        }

        var storedSubscriber = _subscriberDb.CreateInstance().GetSubscriberById(subscriber.Id);
        if (storedSubscriber != null && storedSubscriber.VerifyPassword(password ?? String.Empty))
        {
            return true;
        }

        _loginThrottle.RegisterFailure(subscriber.Name, clientIp);
        _auditLogger.LogWarning("Subscriber account change rejected (wrong current password): {username} from {clientIp}", subscriber.Name, clientIp);

        return false;
    }

    /// <summary>Null if the MFA storage is not available (or can't be read).</summary>
    private SubscriberSecurityInfo TryGetSecurityInfo(string subscriberId)
    {
        try
        {
            return _accountSecurity.GetSecurityInfo(subscriberId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Can't read subscriber security info");
            return null;
        }
    }

    #endregion
}
