using Cms.AppCode.Mvc;
using Cms.AppCode.Services;
using Cms.Models;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Json;
using E.Standard.Security.App.Reflection;
using E.Standard.Security.App.Services;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Extensions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Cms.Controllers;

[ApplicationSecurity]
public class AdminController : ApplicationSecurityController
{
    public AdminController(
                CmsConfigurationService ccs,
                UrlHelperService urlHelperService,
                ApplicationSecurityUserManager applicationSecurityUserManager,
                ICryptoService crypto,
                CmsItemInjectionPackService instanceService,
                IEnumerable<ICustomCmsPageSecurityService> customSecurity = null)
        : base(ccs, urlHelperService, applicationSecurityUserManager, customSecurity, crypto, instanceService)
    {
    }

    public IActionResult Index()
    {
        return View();
    }

    #region CreateLogin

    [HttpGet]
    public IActionResult CreateLogin()
    {
        try
        {
            return View(new AdminCreateLoginModel());
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateLogin(AdminCreateLoginModel model)
    {
        try
        {
            if (String.IsNullOrWhiteSpace(model.Username))
            {
                throw new Exception("Username is empty");
            }

            if (String.IsNullOrWhiteSpace(model.Password))
            {
                throw new Exception("Password is empty");
            }

            model.Json = JSerializer.Serialize(new
            {
                name = model.Username,
                password = model.Password.Hash64()
            }, pretty: true);
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
        }

        return View(model);
    }

    #endregion
}