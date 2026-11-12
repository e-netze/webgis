using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

using Api.Core.AppCode.Exceptions;
using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Reflection;
using Api.Core.AppCode.Services;
using Api.Core.AppCode.Services.Api;
using Api.Core.AppCode.Services.Authentication;
using Api.Core.AppCode.Services.Authentication.LoginThrottling;

using E.Standard.Api.App;
using E.Standard.Api.App.Configuration;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Models;
using E.Standard.Api.App.Reflection;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.Security.App.Exceptions;
using E.Standard.Security.App.Json;
using E.Standard.Security.App.Services;
using E.Standard.Security.Captcha;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Services;
using E.Standard.Security.Cryptography.Token;
using E.Standard.Web.Abstractions;
using E.Standard.WebApp.Reflection;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Models;
using E.Standard.WebGIS.SDK.Services;
using E.Standard.WebGIS.SubscriberDatabase;
using E.Standard.WebGIS.SubscriberDatabase.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Core.Controllers;

[ApiAuthentication(ApiAuthenticationTypes.Cookie)]
[AppRole(AppRoles.SubscriberPages)]
public class SubscribersController : ApiBaseController
{
    private readonly ILogger<SubscribersController> _logger;
    private readonly UrlHelperService _urlHelper;
    private readonly ConfigurationService _config;
    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly ApiToolsService _apiTools;
    private readonly WebgisPortalService _portal;
    private readonly UploadFilesService _uploadFiles;
    private readonly ViewDataHelperService _viewDataHelper;
    private readonly ApiCookieAuthenticationService _cookies;
    private readonly SDKPluginManagerService _sdkPlugins;
    private readonly ApplicationSecurityConfig _appSecurityConfig;
    private readonly ICryptoService _crypto;
    private readonly SubscriberLoginThrottleService _loginThrottle;
    private readonly JwtAccessTokenService _jwtAccessTokenService;
    private readonly IEnumerable<ICustomSubscriberIdenticationService> _customSubscriberIdentications;
    private readonly ILogger _auditLogger;
    private readonly SubscriberSecurityOptions _securityOptions;

    private static readonly SubscriberDb.Subscriber TimingDummySubscriber = new SubscriberDb.Subscriber() { Password = Guid.NewGuid().ToString() };

    internal const string LoginErrorInvalidCredentials = "invalid-credentials";
    internal const string LoginErrorCaptchaRequired = "captcha-required";
    internal const string LoginErrorCaptchaInvalid = "captcha-invalid";
    internal const string LoginErrorTooManyAttempts = "too-many-attempts";
    internal const string LoginErrorTooManyAttemptsMinutes = "too-many-attempts-minutes";
    internal const string LoginErrorFailed = "login-failed";

    /// <summary>Fixed log category for security relevant events (logins, lockouts, ...), e.g. for SIEM filtering.</summary>
    public const string SecurityAuditLogCategory = "WebGIS.Security.Audit";

    private string InvalidPasswordMessage
        => $"Invalid password: min. {_securityOptions.PasswordMinLength}, max. {SubscriberSecurityOptions.PasswordMaxLength} characters, must not be equal to the username";

    public SubscribersController(ILogger<SubscribersController> logger,
                                 ILoggerFactory loggerFactory,
                                 UrlHelperService urlHelper,
                                 ConfigurationService config,
                                 SubscriberDatabaseService subscriberDb,
                                 ApiToolsService apiTools,
                                 WebgisPortalService portal,
                                 UploadFilesService uploadFiles,
                                 ViewDataHelperService viewDataHelper,
                                 ApiCookieAuthenticationService cookies,
                                 SDKPluginManagerService sdkPlugins,
                                 ICryptoService crypto,
                                 SubscriberLoginThrottleService loginThrottle,
                                 IHttpService http,
                                 IOptionsMonitor<ApplicationSecurityConfig> appSecurityConfig,
                                 IOptions<SubscriberSecurityOptions> securityOptions,
                                 JwtAccessTokenService jwtAccessTokenService,
                                 IEnumerable<ICustomApiService> customServices = null,
                                 IEnumerable<ICustomSubscriberIdenticationService> customSubscriberIdentications = null)
        : base(logger, urlHelper, http, customServices)
    {
        _logger = logger;
        _auditLogger = loggerFactory.CreateLogger(SecurityAuditLogCategory);
        _urlHelper = urlHelper;
        _config = config;
        _subscriberDb = subscriberDb;
        _apiTools = apiTools;
        _portal = portal;
        _uploadFiles = uploadFiles;
        _viewDataHelper = viewDataHelper;
        _cookies = cookies;
        _sdkPlugins = sdkPlugins;
        _appSecurityConfig = appSecurityConfig.CurrentValue;
        _securityOptions = securityOptions.Value;
        _customSubscriberIdentications = customSubscriberIdentications;
        _crypto = crypto;
        _loginThrottle = loginThrottle;
        _jwtAccessTokenService = jwtAccessTokenService;
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);

        ViewData["AppRootUrl"] = _urlHelper.AppRootUrl(HttpSchema.Default);
    }

    public IActionResult Index()
    {
        try
        {
            var subscriber = CurrentAuthSubscriber();

            return ViewResult(subscriber);
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
        catch (UnknownSubscriberException)
        {
            return RedirectToAction("UnknownSubscriber", "Authenticate");
        }
    }

    #region Login / Register Subscriber / Update Subscriber

    [AppRole(AppRoles.All)]
    public IActionResult Login(string id)
    {
        if (_config.AllowSubscriberLogin() == false)
        {
            return RedirectToActionResult("Index", "Home");
        }

        if (_appSecurityConfig?.IdentityType == "oidc")
        {
            return RedirectToActionResult("Index", "Authenticate", new { redirect = Request.Query["redirect"], pageId = id });
        }

        var login = new ApiSubscribersLogin()
        {
            Redirect = Request.Query["redirect"]
        };

        if (_loginThrottle.GetStatus(null, HttpContext.Connection.RemoteIpAddress?.ToString()).CaptchaRequired)
        {
            ApplyNewCaptcha(login);
        }

        return ViewResult(login);
    }

    [HttpPost]
    [AppRole(AppRoles.All)]
    [ConfigurableValidateAntiforgeryToken]
    async public Task<IActionResult> Login(string id, ApiSubscribersLogin login)
    {
        if (_config.AllowSubscriberLogin() == false)
        {
            return RedirectToActionResult("Index", "Home");
        }

        login ??= new ApiSubscribersLogin();
        string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            if (String.IsNullOrWhiteSpace(login.Username) || String.IsNullOrEmpty(login.Password))
            {
                throw new SubscriberLoginException(LoginErrorInvalidCredentials);
            }

            #region Throttling / Captcha

            var throttleStatus = _loginThrottle.GetStatus(login.Username, clientIp);

            if (throttleStatus.IsLocked)
            {
                // don't verify the password while locked => brute force is not possible during the lockout
                throw new SubscriberLoginException(LoginErrorTooManyAttemptsMinutes, LockedMinutes(throttleStatus.LockedSeconds));
            }

            if (throttleStatus.IpThrottled)
            {
                await Task.Delay(1000);
            }

            if (throttleStatus.CaptchaRequired)
            {
                if (String.IsNullOrWhiteSpace(login.CaptchaInput))
                {
                    throw new SubscriberLoginException(LoginErrorCaptchaRequired);
                }

                string captchaNonce;
                try
                {
                    captchaNonce = CaptchaResult.VerifyCaptchaCode(_crypto, login.CaptchaInput, login.CaptchaCodeEncrypted,
                                                                   TimeSpan.FromMinutes(_securityOptions.CaptchaLifetimeMinutes));
                }
                catch (Exception ex)
                {
                    // a wrong captcha only counts for the client IP, otherwise anybody could lock out a user
                    _loginThrottle.RegisterFailure(login.Username, clientIp, countForUsername: false);
                    throw new SubscriberLoginException(LoginErrorCaptchaInvalid, ex);
                }

                if (!_loginThrottle.TryConsumeCaptchaNonce(captchaNonce))
                {
                    _loginThrottle.RegisterFailure(login.Username, clientIp, countForUsername: false);
                    throw new SubscriberLoginException(LoginErrorCaptchaInvalid);
                }
            }

            #endregion

            var db = _subscriberDb.CreateInstance(_logger);
            var subscriber = db.GetSubscriberByName(login.Username);

            bool passwordValid;
            if (subscriber != null)
            {
                passwordValid = subscriber.VerifyPassword(login.Password);
            }
            else
            {
                // always verify a password hash, so unknown users can't be detected by response timing
                TimingDummySubscriber.VerifyPassword(login.Password);
                passwordValid = false;
            }

            if (!passwordValid)
            {
                // unknown usernames are counted too, so the throttling doesn't reveal whether a user exists
                _loginThrottle.RegisterFailure(login.Username, clientIp);
                throw new SubscriberLoginException(LoginErrorInvalidCredentials);
            }

            _loginThrottle.RegisterSuccess(login.Username);

            _auditLogger.LogInformation("Subscriber login succeeded: {username} from {clientIp}", subscriber.Name, clientIp);

            if (subscriber.PasswordNeedsRehash)
            {
                try
                {
                    // upgrade legacy/weaker password hashes transparently on login
                    db.UpdateApiSubscriberPassword(subscriber, login.Password);
                    _auditLogger.LogInformation("Subscriber password hash upgraded: {username}", subscriber.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Subscriber password hash upgrade failed: {username}", subscriber.Name);
                }
            }

            if (!String.IsNullOrWhiteSpace(login.Redirect))
            {
                if (!_urlHelper.IsAllowedPortalLoginRedirect(login.Redirect))
                {
                    _auditLogger.LogWarning("Subscriber login: redirect target not allowed for {username} from {clientIp}: {redirect}", subscriber.Name, clientIp, login.Redirect);
                    return BadRequest("Redirect target is not allowed");
                }

                var credentialToken = _jwtAccessTokenService.GenerateToken(subscriber.FullName, 1, JwtAccessTokenService.PortalLoginAudience);

                return RedirectResult($"{login.Redirect}{(login.Redirect.Contains("?") ? "&" : "?")}credential_token={credentialToken}");
            }

            AuthenticateSubscriber(subscriber);

            if (Request.Query["f"] == "json")
            {
                return await JsonObject(new { succeeded = true });
            }

            return RedirectToActionResult("Index");
        }
        catch (Exception ex)
        {
            string errorKey = ex is SubscriberLoginException sle ? sle.ErrorKey : LoginErrorFailed;
            string errorArgument = (ex as SubscriberLoginException)?.ErrorArgument;

            if (ex is SubscriberLoginException)
            {
                _auditLogger.LogWarning("Subscriber login failed ({reason}): {username} from {clientIp}", errorKey, login.Username, clientIp);
            }
            else
            {
                _logger.LogError(ex, "Subscriber login error");
                _auditLogger.LogWarning("Subscriber login failed (error): {username} from {clientIp}", login.Username, clientIp);
            }

            var throttleStatus = _loginThrottle.GetStatus(login.Username, clientIp);
            if (throttleStatus.IsLocked)
            {
                if (errorKey != LoginErrorTooManyAttemptsMinutes)
                {
                    _auditLogger.LogWarning("Subscriber login locked: {username} from {clientIp} for {seconds} seconds", login.Username, clientIp, throttleStatus.LockedSeconds);
                }
                errorKey = LoginErrorTooManyAttemptsMinutes;
                errorArgument = LockedMinutes(throttleStatus.LockedSeconds);
            }

            login.Password = null;
            login.CaptchaInput = null;
            login.ErrorMessage = errorKey;
            login.ErrorMessageArgument = errorArgument;
            login.CaptchaCodeEncrypted = login.CaptchaDataBase64 = null;

            if (throttleStatus.CaptchaRequired)
            {
                ApplyNewCaptcha(login);
            }

            if (Request.Query["f"] == "json")
            {
                return await JsonObject(new
                {
                    succeeded = false,
                    captcha_required = throttleStatus.CaptchaRequired,
                    locked_seconds = throttleStatus.LockedSeconds
                });
            }

            return ViewResult(login);
        }
    }

    [AppRole(AppRoles.All)]
    public IActionResult LoginCaptcha()
    {
        if (_config.AllowSubscriberLogin() == false)
        {
            return NotFound();
        }

        var login = ApplyNewCaptcha(new ApiSubscribersLogin());

        Response.Headers.CacheControl = "no-store";
        return Json(new { encrypted = login.CaptchaCodeEncrypted, base64 = login.CaptchaDataBase64 });
    }

    private ApiSubscribersLogin ApplyNewCaptcha(ApiSubscribersLogin login)
    {
        var captchaResult = Captcha.GenerateCaptchaImage(Captcha.GenerateCaptchaCode());

        login.CaptchaCodeEncrypted = captchaResult.CaptchaCodeEncrypted(_crypto);
        login.CaptchaDataBase64 = captchaResult.CaptchBase64Data;

        return login;
    }

    private static string LockedMinutes(int lockedSeconds)
        => Math.Max(1, (int)Math.Ceiling(lockedSeconds / 60.0)).ToString();

    [AppRole(AppRoles.All)]
    public IActionResult Logout()
    {
        if (_appSecurityConfig?.IdentityType == "oidc")
        {
            return SignOutSchemes("Cookies", "oidc");
        }

        _cookies.SignOut(HttpContext);
        return RedirectToActionResult("Index", "Home");
    }

    public IActionResult Register()
    {
        if (!_config.AllowRegisterNewSubscribers())
        {
            return RedirectToActionResult("Index");
        }

        return ViewResult(new RegisterSubscriberModel());
    }

    [HttpPost]
    [ConfigurableValidateAntiforgeryToken]
    public IActionResult Register(RegisterSubscriberModel registerSubscriber)
    {
        if (!_config.AllowRegisterNewSubscribers())
        {
            return RedirectToActionResult("Index");
        }

        try
        {
            var db = _subscriberDb.CreateInstance();

            if (!registerSubscriber.Username.IsValidUsername())
            {
                throw new Exception("Username invalid: min. 5, max. 32 characters. Only letters, numbers and ._- are allowed");
            }

            if (!registerSubscriber.Email.IsValidEmailAddress())
            {
                throw new Exception("Invalid E-Mail address");
            }

            if (registerSubscriber.Password1 != registerSubscriber.Password2)
            {
                throw new Exception("Passwords are not ident");
            }

            if (!registerSubscriber.Password1.IsValidPassword(_securityOptions, registerSubscriber.Username))
            {
                throw new Exception(InvalidPasswordMessage);
            }

            var subscriber = db.GetSubscriberByName(registerSubscriber.Username);
            if (subscriber != null)
            {
                throw new Exception("Username already exists");
            }

            /*
            subscriber = db.GetSubscriberByName(registerSubscriber.Email);
            if (subscriber != null)
                throw new Exception("User with this email already exists");
             * */

            subscriber = new SubscriberDb.Subscriber()
            {
                Name = registerSubscriber.Username,
                FirstName = registerSubscriber.FirstName,
                LastName = registerSubscriber.LastName,
                Email = registerSubscriber.Email,
                Password = registerSubscriber.Password1
            };

            db.CreateApiSubscriber(subscriber);
            subscriber = db.GetSubscriberByName(registerSubscriber.Username);
            if (subscriber != null)
            {
                AuthenticateSubscriber(subscriber);
            }

            return RedirectToActionResult("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Register error: {0}", ex.Message);
            registerSubscriber.ErrorMessage = ex.Message;
            return ViewResult(registerSubscriber);
        }
    }

    public IActionResult Update(string id)
    {
        try
        {
            var subscriber = CurrentAuthSubscriber();

            if (!String.IsNullOrWhiteSpace(id) && id != subscriber.Id)
            {
                if (!subscriber.IsAdminSubscriber(_config))
                {
                    return RedirectToActionResult("Index");
                }

                var db = _subscriberDb.CreateInstance();
                subscriber = db.GetSubscriberById(id);
            }

            return ViewResult(new RegisterSubscriberModel(subscriber)
            {
                RequireCurrentPassword = subscriber.Id == CurrentAuthSubscriber().Id
            });
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
    }

    [HttpPost]
    [ConfigurableValidateAntiforgeryToken]
    public IActionResult Update(RegisterSubscriberModel updateSubscriber)
    {
        try
        {
            if (!updateSubscriber.Email.IsValidEmailAddress())
            {
                throw new ArgumentException("Invalid Email");
            }

            var db = _subscriberDb.CreateInstance();

            var subscriber = CurrentAuthSubscriber();
            if (updateSubscriber.Id != subscriber.Id)
            {
                if (!subscriber.IsAdminSubscriber(_config))
                {
                    throw new Exception("Not allowed");
                }

                subscriber = db.GetSubscriberById(updateSubscriber.Id);
            }

            subscriber.FirstName = updateSubscriber.FirstName;
            subscriber.LastName = updateSubscriber.LastName;
            subscriber.Email = updateSubscriber.Email;

            db.UpdateApiSubscriberSettings(subscriber);

            return RedirectToActionResult("Index");
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update error: {0}", ex.Message);
            updateSubscriber.ErrorMessage = ex.Message;
            return ViewResult(updateSubscriber);
        }
    }

    [HttpPost]
    [ConfigurableValidateAntiforgeryToken]
    public IActionResult ChangePassword(RegisterSubscriberModel updateSubscriber)
    {
        try
        {
            if (updateSubscriber.Password1 != updateSubscriber.Password2)
            {
                throw new ArgumentException("Passwords not ident");
            }

            var currentSubscriber = CurrentAuthSubscriber();
            var subscriber = currentSubscriber;

            var db = _subscriberDb.CreateInstance();

            if (updateSubscriber.Id != subscriber.Id)
            {
                if (!subscriber.IsAdminSubscriber(_config))
                {
                    throw new Exception("Not allowed");
                }

                subscriber = db.GetSubscriberById(updateSubscriber.Id);
            }
            else
            {
                updateSubscriber.RequireCurrentPassword = true;

                // re-authenticate: changing the own password requires the current password
                var storedSubscriber = db.GetSubscriberById(subscriber.Id);
                if (storedSubscriber == null || !storedSubscriber.VerifyPassword(updateSubscriber.CurrentPassword ?? String.Empty))
                {
                    _auditLogger.LogWarning("Subscriber password change rejected (wrong current password): {username} from {clientIp}", subscriber.Name, HttpContext.Connection.RemoteIpAddress?.ToString());
                    throw new Exception("Current password is not correct");
                }
            }

            if (!updateSubscriber.Password1.IsValidPassword(_securityOptions, subscriber.Name))
            {
                throw new Exception(InvalidPasswordMessage);
            }

            db.UpdateApiSubscriberPassword(subscriber, updateSubscriber.Password1);

            _auditLogger.LogInformation("Subscriber password changed: {username} by {changedBy} from {clientIp}", subscriber.Name, currentSubscriber.Name, HttpContext.Connection.RemoteIpAddress?.ToString());

            return RedirectToActionResult("Index");
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change password error: {0}", ex.Message);
            updateSubscriber.ErrorMessage = ex.Message;
            return ViewResult("Update", updateSubscriber);
        }
    }

    #endregion

    #region Clients

    public IActionResult Clients()
    {
        try
        {
            var subscriber = CurrentAuthSubscriber();

            ISubscriberDb db = _subscriberDb.CreateInstance();

            return ViewResult(new ApiClients()
            {
                Clients = db.GetSubscriptionClients(subscriber.Id)
            });
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
    }

    public IActionResult NewClient()
    {
        try
        {
            var subscriber = CurrentAuthSubscriber();

            var db = _subscriberDb.CreateInstance();
            var clients = db.GetSubscriptionClients(subscriber.Id);

            int i = 1;
            string name = "client" + i;
            while (true)
            {
                var sameNameClient = (from c in clients where c.ClientName == name select c).FirstOrDefault();
                if (sameNameClient == null)
                {
                    break;
                }

                i++;
                name = "client" + i;
            }

            string clientId = Guid.NewGuid().ToString("N").ToLower();
            if (db is ISubscriberDb2)
            {
                clientId = ((ISubscriberDb2)db).GenerateNewClientId().OrTake(clientId);
            }

            var client = new UpdateClient();

            client.ClientName = name;
            client.ClientId = clientId;
            client.ClientSecret = Guid.NewGuid().ToString("N");
            client.ClientReferer = "http://";
            client.Created = DateTime.UtcNow;

            return ViewResult("UpdateClient", client);
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
    }

    [HttpGet]
    public IActionResult UpdateClient(string clientId)
    {
        try
        {
            var subscriber = CurrentAuthSubscriber();

            ISubscriberDb db = _subscriberDb.CreateInstance();

            SubscriberDb.Client client = db.GetClientByClientId(clientId);

            if (client == null || client.Subscriber != subscriber.Id)
            {
                return RedirectToActionResult("Index");
            }

            return ViewResult(new UpdateClient(client));
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
    }

    [HttpPost]
    [ConfigurableValidateAntiforgeryToken]
    public IActionResult UpdateClient(UpdateClient client)
    {
        try
        {
            var subscriber = CurrentAuthSubscriber();
            client.Subscriber = subscriber.Id;

            if (!client.ClientName.IsValidUsername())
            {
                throw new Exception("ClientName invalid: min. 5, max. 32 characters. Only letters, numbers and ._- are allowed");
            }

            ISubscriberDb db = _subscriberDb.CreateInstance();

            if (db.GetClientByClientId(client.ClientId) == null)
            {
                //if (db.GetClientByName(subscriber, client.ClientName) != null)
                //    throw new Exception("ClientName already exists");

                client.Created = DateTime.UtcNow;
                db.CreateApiClient(client.ToClient());
            }
            else
            {
                db.UpdateApiClient(client.ToClient());
            }

            return RedirectToActionResult("Clients");
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update client error: {0}", ex.Message);
            client.ErrorMessage = ex.Message;
            return ViewResult(client);
        }
    }

    public IActionResult LoginAsClient(string clientId)
    {
        try
        {
            var subscriber = CurrentAuthSubscriber();

            ISubscriberDb db = _subscriberDb.CreateInstance();

            SubscriberDb.Client client = db.GetClientByClientId(clientId);

            if (client == null || client.Subscriber != subscriber.Id)
            {
                return RedirectToActionResult("Index");
            }

            _cookies.SignOut(HttpContext);
            _cookies.SetAuthCookie(HttpContext, "clientid:" + client.ClientId + ":" + subscriber.Name + "@" + client.ClientName);

            return RedirectToActionResult("Index", "Rest");
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
    }

    #endregion

    #region Portal Pages

    public IActionResult PortalPages()
    {
        try
        {
            var subscriber = CurrentAuthSubscriber();

            string[] portalIds = _apiTools.ExecuteToolCommand<E.Standard.WebGIS.Tools.Portal.Portal, string[]>(HttpContext, "list-user-pages", new NameValueCollection(), subscriber.FullName);

            List<ApiPortalPageDTO> portals = new List<ApiPortalPageDTO>();
            if (portalIds != null)
            {
                foreach (string portalId in portalIds)
                {
                    var parameters = new NameValueCollection();
                    parameters.Add("page-id", portalId);

                    ApiPortalPageDTO portal = _apiTools.ExecuteToolCommand<E.Standard.WebGIS.Tools.Portal.Portal, ApiPortalPageDTO>(HttpContext, "user-page", parameters, subscriber.FullName);
                    portals.Add(portal);
                }
            }

            return ViewResult(new ApiPortals()
            {
                Portals = portals.ToArray()
            });
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
    }

    async public Task<IActionResult> NewPortalPage()
    {
        var subscriber = CurrentAuthSubscriber();

        AddViewData("security-prefixes", (await _portal.SecurityPrefixes(HttpContext)).ToJavascriptStringArray());

        return ViewResult("UpdatePortalPage",
            new ApiPortalPageDTO()
            {
                MapAuthors = new string[] { UserManagement.AppendUserPrefix(subscriber.Name, UserType.ApiSubscriber) },
                Users = new string[] { "*" },
                Subscriber = subscriber.FullName,
                Created = DateTime.UtcNow
            });
    }

    [HttpGet]
    async public Task<IActionResult> UpdatePortalPage(string id)
    {
        try
        {
            var subscriber = CurrentAuthSubscriber();

            AddViewData("security-prefixes", (await _portal.SecurityPrefixes(HttpContext)).ToJavascriptStringArray());

            NameValueCollection parameters = new NameValueCollection();
            parameters.Add("page-id", id);

            ApiPortalPageDTO portal = _apiTools.ExecuteToolCommand<E.Standard.WebGIS.Tools.Portal.Portal, ApiPortalPageDTO>(HttpContext, "user-page", parameters, subscriber.FullName);

            return ViewResult(portal);
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
    }

    [HttpPost]
    [ConfigurableValidateAntiforgeryToken]
    async public Task<IActionResult> UpdatePortalPage(ApiPortalPageDTO page)
    {
        try
        {
            var subscriber = CurrentAuthSubscriber();
            if (subscriber.FullName != page.Subscriber)
            {
                throw new Exception("Not allowed");
            }

            page.SubscriberId = subscriber.Id;

            if (!page.Id.IsValidUrlId())
            {
                page.Id = String.Empty;
                throw new Exception("Id invalid: min. 3, max. 32 characters. Only letters, numbers and - are allowed");
            }

            if (String.IsNullOrWhiteSpace(page.Name))
            {
                throw new Exception("Name is empty");
            }

            if (String.IsNullOrWhiteSpace(page.MapAuthors[0]))
            {
                page.MapAuthors[0] = UserManagement.AppendUserPrefix(subscriber.Name, UserType.ApiSubscriber);
            }

            if (String.IsNullOrWhiteSpace(page.ContentAuthors[0]))
            {
                page.ContentAuthors[0] = UserManagement.AppendUserPrefix(subscriber.Name, UserType.ApiSubscriber);
            }

            if (String.IsNullOrWhiteSpace(page.Users[0]))
            {
                page.Users[0] = "*";
            }

            page.MapAuthors = page.MapAuthors[0].Split(',');
            page.ContentAuthors = page.ContentAuthors[0].Split(',');
            page.Users = page.Users[0].Split(',');

            NameValueCollection parameters = new NameValueCollection();
            parameters.Add("page-id", page.Id);

            var file = _uploadFiles.GetFiles(this.Request)["banner-image"];
            if (file != null)
            {
                byte[] buffer = file.Data;

                parameters.Add("page-banner-image", Convert.ToBase64String(buffer));
                page.BannerId = _crypto.EncryptTextDefault(page.Id, CryptoResultStringType.Hex);
            }

            parameters.Add("page-json", JSerializer.Serialize(page));

            _apiTools.ExecuteToolCommand<E.Standard.WebGIS.Tools.Portal.Portal, ApiPortalPageDTO>(HttpContext, "update-page", parameters, subscriber.FullName);

            return RedirectToActionResult("PortalPages");
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update portal page error: {0}", ex.Message);

            AddViewData("security-prefixes", (await _portal.SecurityPrefixes(HttpContext)).ToJavascriptStringArray());

            page.ErrorMessage = ex.Message;
            return ViewResult(page);
        }
    }

    async public Task<IActionResult> Autocomplete_Portal_Auth(string prefix, string term)
    {
        var ret = await _portal.SecurityAutocomplete(HttpContext, term, prefix);

        return await JsonObject(ret.ToArray());
    }

    public Task<IActionResult> CreatePortalCredentialToken(string id)
    {
        var subscriber = CurrentAuthSubscriber();

        //string credentials = id + "|" + subscriber.FullName + "|" + DateTime.UtcNow.Ticks;
        //credentials = _crypto.EncryptTextDefault(credentials, CryptoResultStringType.Hex);

        string credentialToken = _jwtAccessTokenService.GenerateToken(subscriber.FullName, 1, JwtAccessTokenService.PortalLoginAudience);

        return JsonObject(new
        {
            credentialToken = credentialToken
        });
    }

    #endregion

    #region Admin

    public IActionResult AdminSubscribers()
    {
        try
        {
            var subscriber = CurrentAuthSubscriber();
            if (!subscriber.IsAdminSubscriber(_config))
            {
                throw new Exception("Not allowed");
            }

            var db = _subscriberDb.CreateInstance();
            var subcribers = db.GetSubscribers();

            return ViewResult(subcribers);
        }
        catch (NotAuthorizedException)
        {
            return RedirectToActionResult("Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin subscribers error: {0}", ex.Message);

            return RedirectToActionResult("Index");
        }
    }

    #endregion

    #region Helper

    #region Authentication

    private void AuthenticateSubscriber(SubscriberDb.Subscriber subscriber)
    {
        _cookies.SetAuthCookie(HttpContext, $"subscriber:{subscriber.Id}:{subscriber.Name.ToLower()}");
    }

    internal SubscriberDb.Subscriber CurrentAuthSubscriber(bool throwException = true, string endpoint = "", ApiAuthenticationTypes acceptedAuthTypes = ApiAuthenticationTypes.Cookie)
    {
        return DetermineCurrentAuthSubscriber(throwException, endpoint, acceptedAuthTypes);
    }

    internal SubscriberDb.Subscriber DetermineCurrentAuthSubscriber(bool throwException = true, string endpoint = "", ApiAuthenticationTypes acceptedAuthTypes = ApiAuthenticationTypes.Cookie)
    {
        var securityConfig = _appSecurityConfig;
        var baseController = this;
        var request = this.Request;

        try
        {
            var ui = this.User.ToUserIdentification(acceptedAuthTypes);
            SubscriberDb.Subscriber subscriber = null;

            if (securityConfig?.IdentityType == ApplicationSecurityIdentityTypes.OpenIdConnection ||
                securityConfig?.IdentityType == ApplicationSecurityIdentityTypes.AzureAD)
            {
                if (!User.Identity.IsAuthenticated)
                {
                    throw new NotAuthorizedException();
                }

                subscriber = new SubscriberDb.Subscriber()
                {
                    IsAdministrator = false,
                    Name = ui.Username,
                    Id = ui.UserId,
                };

                var db = _subscriberDb.CreateInstance();
                if (db.GetSubscriberById(ui.UserId) == null)
                {
                    throw new UnknownSubscriberException();
                }

                if (String.IsNullOrWhiteSpace(subscriber.Id))
                {
                    throw new NotAuthorizedException();
                }
            }
            else
            {
                if (ui.Userroles != null && ui.Userroles.Contains("subscriber"))
                {
                    var db = _subscriberDb.CreateInstance();
                    subscriber = db.GetSubscriberById(ui.UserId);
                    if (subscriber == null || !subscriber.Name.Equals(ui.Username, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (throwException)
                        {
                            throw new NotAuthorizedException();
                        }

                        return null;
                    }
                }
                else if (_customSubscriberIdentications != null)
                {
                    foreach (var customSubscriberIdentication in _customSubscriberIdentications)
                    {
                        if (customSubscriberIdentication.HasSubscriberRole(ui.Userroles))
                        {
                            subscriber = new SubscriberDb.Subscriber()
                            {
                                Id = ui.UserId,
                                Name = ui.Username,
                                FullName = customSubscriberIdentication.ToSubscriberFullName(ui.Username, ui.Userroles)
                            };
                            break;
                        }
                    }
                }

                if (subscriber == null)
                {
                    if (throwException)
                    {
                        throw new NotAuthorizedException();
                    }

                    return null;
                }
            }

            _viewDataHelper.AddUsernameViewData(this, subscriber);
            return subscriber;
        }
        catch (NotAuthorizedException aex)
        {
            Console.WriteLine("EXCEPTION DetermineCurrentAuthSubscriber " + aex.Message);
            Console.WriteLine("Stacktrace: " + aex.StackTrace);

            _cookies.SignOut(HttpContext);
            throw;
        }
    }

    #endregion

    #endregion
}
