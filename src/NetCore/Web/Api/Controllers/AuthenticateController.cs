using E.Standard.OpenIdConnect.Extensions;
using E.Standard.Security.App.Extensions;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Services;
using E.Standard.WebGIS.SubscriberDatabase;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text;

namespace Api.Core.Controllers;

public class AuthenticateController : Controller
{
    private readonly ApplicationSecurityConfig _appSecurityConfig;
    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly ICryptoService _crypto;
    private readonly JwtAccessTokenService _jwtAccessTokenService;

    public AuthenticateController(IOptionsMonitor<ApplicationSecurityConfig> appSecurityConfig,
                                  SubscriberDatabaseService subscriberDb,
                                  ICryptoService crypto,
                                  JwtAccessTokenService jwtAccessTokenService)
    {
        _appSecurityConfig = appSecurityConfig.CurrentValue;
        _subscriberDb = subscriberDb;
        _crypto = crypto;
        _jwtAccessTokenService = jwtAccessTokenService;
    }

    [Authorize]
    public IActionResult Index(string pageId, string redirect)
    {
        switch (_appSecurityConfig?.IdentityType)
        {
            case ApplicationSecurityIdentityTypes.OpenIdConnection:
                if (_appSecurityConfig.ConfirmSecurity(
                        this.User.GetUsername(),
                        this.User.GetRoles(_appSecurityConfig)) == false)
                {
                    return RedirectToAction("Forbidden");
                }

                break;
        }

        var subscriber = new SubscriberDb.Subscriber()
        {
            Name = this.User.GetUsername(),
            Id = this.User.GetUserId()
        };

        #region Map user to an fixed username => different users can use same username (admins)

        var mappedUser = _appSecurityConfig?
                .MapUsers?
                .Where(u => u.MapUsernames != null && u.MapUsernames.Contains(subscriber.Name))
                .FirstOrDefault();

        if (mappedUser != null)
        {
            subscriber.Id = mappedUser.UserId;
            subscriber.Name = mappedUser.Name;
        }

        #endregion

        #region Check if User realy exists in SubscriberDatabase

        var subscriberDb = _subscriberDb.CreateInstance();
        if (subscriberDb.GetSubscriberById(subscriber.Id) == null)
        {
            return RedirectToAction("UnknownSubscriber");
        }

        #endregion

        if (!String.IsNullOrWhiteSpace(pageId) && !String.IsNullOrWhiteSpace(redirect))
        {
            //string credentials = pageId + "|" + subscriber.FullName + "|" + DateTime.UtcNow.Ticks;
            //credentials = _crypto.EncryptTextDefault(credentials, CryptoResultStringType.Hex);

            var credentialToken = _jwtAccessTokenService.GenerateToken(subscriber.FullName, 1);

            return Redirect($"{redirect}{(redirect.Contains("?") ? "&" : "?")}credential_token={credentialToken}");
        }

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Forbidden()
    {
        ViewData["Username"] = this.User?.Identity.Name ?? "Unknown";
        ViewData["Roles"] = String.Join(", ", this.User?.GetRoles(_appSecurityConfig));

        StringBuilder claims = new StringBuilder();
        foreach (var claim in this.User.Claims)
        {
            claims.Append($"{claim.Type}={claim.Value}, ");
        }

        ViewData["Claims"] = claims.ToString();

        return View();
    }

    public IActionResult UnknownSubscriber()
    {
        return View();
    }
}