using System;
using System.ComponentModel;

using E.Standard.WebGIS.SubscriberDatabase;

namespace E.Standard.Api.App.Models;

public class ApiClients
{
    public SubscriberDb.Client[] Clients { get; set; }
}

public class UpdateClient : ErrorHandlingModel
{
    public UpdateClient() { }
    public UpdateClient(SubscriberDb.Client client)
    {
        this.Id = client.Id;
        this.ClientName = client.ClientName;
        this.ClientId = client.ClientId;
        this.ClientSecret = client.ClientSecret;
        this.ClientReferer = client.ClientReferer;
        this.Created = client.Created;
        this.Expires = client.Expires;
        this.Locked = client.Locked;
        this.Subscriber = client.Subscriber;
    }

    public string Id { get; set; }
    public string ClientName { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string ClientReferer { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Expires { get; set; }
    public bool Locked { get; set; }
    public string Subscriber { get; set; }

    public SubscriberDb.Client ToClient()
    {
        return new SubscriberDb.Client()
        {
            Id = this.Id,
            ClientName = this.ClientName,
            ClientId = this.ClientId,
            ClientSecret = this.ClientSecret,
            ClientReferer = this.ClientReferer,
            Created = this.Created,
            Expires = this.Expires,
            Locked = this.Locked,
            Subscriber = this.Subscriber
        };
    }
}

public class ApiSubscribersLogin : ErrorHandlingModel
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Redirect { get; set; }

    public string CaptchaCodeEncrypted { get; set; }
    public string CaptchaDataBase64 { get; set; }
    [DisplayName("Captcha-Code")]
    public string CaptchaInput { get; set; }

    /// <summary>
    /// Optional value for a {0} placeholder in the localized error message (e.g. remaining lockout minutes).
    /// </summary>
    public string ErrorMessageArgument { get; set; }
}

public class RegisterSubscriberModel : ErrorHandlingModel
{
    public RegisterSubscriberModel()
    {

    }
    public RegisterSubscriberModel(SubscriberDb.Subscriber subscriber)
        : this()
    {
        this.Id = subscriber.Id;
        this.Username = subscriber.Name;
        this.FirstName = subscriber.FirstName;
        this.LastName = subscriber.LastName;
        this.Email = subscriber.Email;
    }

    public string Id { get; set; }
    public string Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }

    [DisplayName("Password")]
    public string Password1 { get; set; }
    [DisplayName("Repeat password")]
    public string Password2 { get; set; }

    [DisplayName("Current password")]
    public string CurrentPassword { get; set; }

    /// <summary>
    /// True if a subscriber changes its own password => current password is required.
    /// </summary>
    public bool RequireCurrentPassword { get; set; }

    #region MFA (admin view of another subscriber)

    public bool MfaAvailable { get; set; }
    public bool MfaEnabled { get; set; }
    public bool CanResetMfa { get; set; }

    #endregion
}

/// <summary>
/// Second login step (verify), forced setup at login (enroll) or setup from the account page (setup).
/// </summary>
public class ApiSubscribersMfaLogin : ErrorHandlingModel
{
    public const string ModeVerify = "verify";
    public const string ModeEnroll = "enroll";
    public const string ModeSetup = "setup";

    public string Mode { get; set; }

    /// <summary>Encrypted flow state (subscriber, stage, secret, redirect...).</summary>
    public string Token { get; set; }

    public string Code { get; set; }

    // setup/enroll only (display)
    public string QrCodeSvg { get; set; }
    public string Secret { get; set; }

    public string ErrorMessageArgument { get; set; }
}

public class ApiSubscribersMfaRecoveryCodes : ErrorHandlingModel
{
    public string[] RecoveryCodes { get; set; }

    /// <summary>Set during the login (enrollment) => continue to complete the login.</summary>
    public string ContinueToken { get; set; }
}

/// <summary>
/// Forced password change during the login (e.g. the password is a known default password like the one of the initial installation).
/// </summary>
public class ApiSubscribersLoginChangePassword : ErrorHandlingModel
{
    /// <summary>Encrypted flow state (subscriber, stage, redirect...).</summary>
    public string Token { get; set; }

    public string NewPassword1 { get; set; }
    public string NewPassword2 { get; set; }

    public string ErrorMessageArgument { get; set; }
}

public class ApiSubscribersAccount : ErrorHandlingModel
{
    public bool Available { get; set; }
    public bool Enabled { get; set; }
    public bool Required { get; set; }
    public int RecoveryCodesLeft { get; set; }

    [DisplayName("Current password")]
    public string CurrentPassword { get; set; }

    #region Change password

    public string NewPassword1 { get; set; }
    public string NewPassword2 { get; set; }

    #endregion

    /// <summary>Localization key (subscribers.account.success.*) of a success message.</summary>
    public string SuccessMessage { get; set; }

    public string ErrorMessageArgument { get; set; }
}