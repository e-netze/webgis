using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.Custom.Core.Models;
using E.Standard.Security.App.Extensions;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Services;
using E.Standard.WebApp.Extensions;
using E.Standard.WebGIS.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Mvc;
using Portal.Core.AppCode.Services;
using Portal.Core.AppCode.Services.Authentication;
using Portal.Core.AppCode.Services.WebgisApi;
using Portal.Core.Models.Auth;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal.Core.Controllers;

public class AuthController : PortalBaseController
{
    private readonly IEnumerable<IPortalAuthenticationService> _authenticationServices;
    private readonly WebgisCookieService _webgisCookie;
    private readonly UrlHelperService _urlHelper;
    private readonly ConfigurationService _config;
    private readonly ApplicationSecurityConfig _appSecurityConfig;
    private readonly WebgisApiService _api;
    private readonly ICryptoService _crypto;
    private readonly IEnumerable<ICustomPortalSecurityService> _customSecurity;
    private readonly ILogger<AuthController> _logger;
    private readonly JwtAccessTokenService _jwtAccessTokenService;

    public AuthController(WebgisCookieService webgisCookie,
                          IEnumerable<IPortalAuthenticationService> authentication,
                          UrlHelperService urlHelper,
                          ConfigurationService config,
                          WebgisApiService api,
                          ICryptoService crypto,
                          IOptions<ApplicationSecurityConfig> appSecurityConfig,
                          ILogger<AuthController> logger,
                          JwtAccessTokenService jwtAccessTokenService,
                          IEnumerable<ICustomPortalSecurityService> customSecurity = null)
        : base(logger, urlHelper, appSecurityConfig, customSecurity, crypto)
    {
        _webgisCookie = webgisCookie;
        _authenticationServices = authentication;
        _urlHelper = urlHelper;
        _config = config;
        _api = api;
        _crypto = crypto;
        _appSecurityConfig = appSecurityConfig.Value;
        _customSecurity = customSecurity;
        _logger = logger;
        _jwtAccessTokenService = jwtAccessTokenService;
    }

    public IActionResult Index()
    {
        return ViewResult();
    }

    async public Task<IActionResult> Login(string id)
    {
        try
        {
            var portalUser = base.CurrentPortalUser();

            var portals = await _api.GetApiPortalPagesAsync(this.HttpContext);

            List<string> allowedAuthenticationMethods = new List<string>();
            if (_appSecurityConfig?.IdentityType == ApplicationSecurityIdentityTypes.OpenIdConnection ||
                _appSecurityConfig?.IdentityType == ApplicationSecurityIdentityTypes.AzureAD)
            {
                allowedAuthenticationMethods.Add("oidc");
            }
            else
            {
                allowedAuthenticationMethods.AddRange(_config.AllowedSecurityMethods());
            }

            List<AuthPortalModel> authPortals = new List<AuthPortalModel>();
            if (portals != null)
            {
                foreach (var portal in portals)
                {
                    authPortals.Add(new AuthPortalModel(portal,
                                                        IsAuthorizedPortalUser(portal, portalUser, false),
                                                        allowedAuthenticationMethods,
                                                        _customSecurity));
                }
            }

            string subscriberUrl = _urlHelper.ApiUrl(this.Request, HttpSchema.Default);
            if (!String.IsNullOrWhiteSpace(subscriberUrl))
            {
                subscriberUrl += "/subscribers/login/~~portalid~~?redirect={redirect}";
            }

            var loginButtons = new List<CustomPortalLoginButton>();
            foreach (var allowedInstanceMethod in _config.AllowedSecurityMethods())
            {
                switch (allowedInstanceMethod)
                {
                    case "anonym":
                        break;
                    case "oidc":
                        loginButtons.Add(new CustomPortalLoginButton()
                        {
                            Title = "Anmelden (Login)",
                            Description = "Mit OpenId Connect anmelden",
                            RelativeImagePath = "/content/img/login/openid-100-w.png",
                            RedirectAction = "LoginOidc",
                            Method = "oidc"
                        });
                        break;
                    default:
                        var loginButton = _customSecurity.LoginButton(this.HttpContext, allowedInstanceMethod);
                        if (loginButton != null)
                        {
                            loginButtons.Add(loginButton);
                        }
                        break;
                }
            }

            return ViewResult(new AuthLoginModel()
            {
                PortalId = id != null ? id : String.Empty,
                SubscriberLoginUrl = subscriberUrl,
                Portals = authPortals.ToArray(),
                CurrentUsername = portalUser != null ? portalUser.DisplayName : String.Empty,
                LoginButtons = loginButtons
            });
        }
        catch (Exception ex)
        {
            return JsonViewSuccess(false, ex.Message);
        }
    }

    public IActionResult Logout(string id)
    {
        var portalUser = base.CurrentPortalUser();

        if (_appSecurityConfig.UseOpenIdConnect()
            && portalUser?.Username?.StartsWith("subscriber::") == false)
        {
            base.SignOut();

            return base.SignOutSchemes("Cookies", "oidc");
        }
        else
        {
            base.SignOut();

            var actionResult = _customSecurity.LogoutRedirectAction(id);
            if (!String.IsNullOrEmpty(actionResult?.action))
            {
                return RedirectToActionResult(actionResult.Value.action,
                                              actionResult.Value.controller,
                                              actionResult.Value.parameters);
            }
            else
            {
                return RedirectToActionResult("Login");
            }
        }
    }

    public IActionResult LoginAsAdmin(string id, string credential_token)
    {
        try
        {
            var subscriberName = _jwtAccessTokenService.ValidatedName(credential_token);

            if (!String.IsNullOrEmpty(subscriberName))
            {
                _webgisCookie.SetAuthCookie(this.HttpContext, false, subscriberName, UserType.ApiSubscriber);

                return RedirectToActionResult("Index", "Home", new { id = id });
            }
        }
        catch
        {

        }

        return RedirectToActionResult("Login", "Auth", new { id = id });
    }

    //[Authorize(Microsoft.AspNetCore.Server.IISIntegration.IISDefaults.AuthenticationScheme)]
    [Authorize]
    async public Task<IActionResult> LoginAD(string id)
    {
        var windowsUser = await _authenticationServices
                                    .GetService(UserType.WindowsUser)?
                                    .TryAuthenticationServiceUser(this.HttpContext, this.User.Identity.Name, true);

        if (windowsUser != null)
        {
            _webgisCookie.SetAuthCookie(this.HttpContext, true, windowsUser.Username, UserType.WindowsUser);
        }

        return RedirectToActionResult("Index", "Home", new { id = id });
    }

    [Authorize]
    public IActionResult LoginOidc(string id)
    {
        if (_appSecurityConfig?.OpenIdConnectConfiguration?.ExtendedRolesFrom == "windows")
        {
            _webgisCookie.SetAuthCookie(
                this.HttpContext,
                true,
                this.HttpContext.User.Identity.Name,
                E.Standard.WebGIS.Core.UserType.WindowsUser);
        }

        _logger.LogInformation("User {username} logged in with OIDC", Request.HttpContext.User.Identity.Name);
        _logger.LogClaims(LogLevel.Debug, Request.HttpContext.User);

        // eg. request to authenticate a datalinq report
        // => hmac response will get an "authEndpoint" attribute
        // => datalinq.js this endpoint (/auth/loginOidc) to authenticate
        // => redirect back to the datalinq page
        if (!String.IsNullOrEmpty(this.Request.Query["webgis-redirect"]))
        {
            var redirectUrl = _crypto.DecryptTextDefault(this.Request.Query["webgis-redirect"]);

            return Redirect(redirectUrl);
        }

        return RedirectToActionResult("Index", "Home", new { id = id });
    }

    public IActionResult LogoutOidc(string id)
    {
        base.SignOut();

        return base.SignOutSchemesAndRedirect(
            Url.Action("Index", "Home", new { id = id }),
            "Cookies", "oidc");
    }

    public IActionResult ChangeAccountOidc(string id)
    {
        ViewData["portal-id"] = id;
        return View();
    }

    public IActionResult InvalidAccessToken(string id)
    {
        ViewData["portal-id"] = id;
        return View();
    }
}