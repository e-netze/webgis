using Cms.AppCode.Mvc;
using Cms.AppCode.Services;
using Cms.Models;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.App;
using E.Standard.Security.App.Exceptions;
using E.Standard.Security.App.Json;
using E.Standard.Security.App.Services;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Cms.Controllers;

public class LoginController : ApplicationSecurityController
{
    readonly LoginFailsManager failsManager = new LoginFailsManager(3, 30);

    private readonly ApplicationSecurityUserManager _applicationSecurityUserManager;
    private readonly ICryptoService _crypto;
    private readonly SubscriberDatabaseService _subscriberDb;

    public LoginController(
            CmsConfigurationService ccs,
            UrlHelperService urlHelperService,
            ApplicationSecurityUserManager applicationSecurityUserManager,
            ICryptoService crypto,
            CmsItemInjectionPackService instanceService,
            SubscriberDatabaseService subscriberDb = null,
            IEnumerable<ICustomCmsPageSecurityService> customSecurity = null)
        : base(ccs, urlHelperService, applicationSecurityUserManager, customSecurity, crypto, instanceService)
    {
        _applicationSecurityUserManager = applicationSecurityUserManager;
        _crypto = crypto;
        _subscriberDb = subscriberDb;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(string cms_login_user, string cms_login_password)
    {
        try
        {
            failsManager.CheckFails(cms_login_user);

            try
            {
                _applicationSecurityUserManager.ValidateUserPassword(_crypto, cms_login_user, cms_login_password);
            }
            catch (UnknownUsernameException)
            {
                if (_subscriberDb != null)
                {
                    var subscriber = _subscriberDb.CreateInstance().GetSubscriberByName(cms_login_user);

                    if (subscriber == null)
                    {
                        throw;
                    }

                    if (subscriber?.VerifyPassword(cms_login_password) != true)
                    {
                        throw new WrongPasswordException();
                    }

                    cms_login_user = $"subscriber::{cms_login_user}";
                }
                else
                {
                    throw;
                }
            }

            base.SetAuthCookie(cms_login_user, false);

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            if (ex is WrongPasswordException)
            {
                failsManager.AddFail(cms_login_user);
            }

            return View(new LoginModel()
            {
                Username = cms_login_user,
                ErrorMessage = ex.Message
            });
        }
    }

    public IActionResult Logout()
    {
        switch (_applicationSecurityUserManager.ApplicationSecurity?.IdentityType)
        {
            case ApplicationSecurityIdentityTypes.OpenIdConnection:
            case ApplicationSecurityIdentityTypes.AzureAD:
                return this.SignOut("Cookies", "oidc");
        }

        base.SignOut();

        return RedirectToAction("Index", "Home");
    }
}