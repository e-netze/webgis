using Cms.AppCode.Mvc;
using Cms.AppCode.Services;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Services;
using E.Standard.CMS.Core;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.App.Services;
using E.Standard.Security.Cryptography.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Cms.Controllers;

public class SetupController : ApplicationSecurityController
{
    private readonly CmsConfigurationService _ccs;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public SetupController(
                CmsConfigurationService ccs,
                UrlHelperService urlHelperService,
                ApplicationSecurityUserManager applicationSecurityUserManager,
                ICryptoService crypto,
                CmsItemInjectionPackService instanceService,
                IEnumerable<ICustomCmsPageSecurityService> customSecurity = null)
        : base(ccs, urlHelperService, applicationSecurityUserManager, customSecurity, crypto, instanceService)
    {
        _ccs = ccs;
        _servicePack = instanceService.ServicePack;
    }

    public IActionResult Index()
    {
        return View();
    }

    #region Reload Root

    public IActionResult ReloadRoot(string id = "")
    {
        try
        {
            var cms = _ccs.CMS[id];

            cms.OnParseSchemaNode += Cms_OnParseSchemaNode;
            cms.Reload(_servicePack, true);
            cms.OnParseSchemaNode -= Cms_OnParseSchemaNode;

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    private void Cms_OnParseSchemaNode(object sender, EventArgs e)
    {
        if (e is CMSManager.ParseEventArgs)
        {

        }
    }

    #endregion
}