using Cms.AppCode.Mvc;
using Cms.AppCode.Services;
using Cms.Models;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.App.Reflection;
using E.Standard.Security.App.Services;
using E.Standard.Security.Cryptography.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cms.Controllers;

public class HomeController : ApplicationSecurityController
{
    public HomeController(
                CmsConfigurationService ccs,
                UrlHelperService urlHelperService,
                ApplicationSecurityUserManager applicationSecurityUserManager,
                ICryptoService crypto,
                CmsItemInjectionPackService instanceService,
                IEnumerable<ICustomCmsPageSecurityService> customSecurity = null)
        : base(ccs, urlHelperService, applicationSecurityUserManager, customSecurity, crypto, instanceService)
    {
    }

    [ApplicationSecurity]
    public IActionResult Index(string id = "")
    {
        try
        {
            return View();
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
