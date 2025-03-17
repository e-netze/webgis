using Cms.AppCode;
using Cms.AppCode.Mvc;
using Cms.AppCode.Services;
using Cms.Models;
using E.Standard.Cms.Abstraction;
using E.Standard.Cms.Configuration.Models;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Services;
using E.Standard.CMS.Core;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.App.Reflection;
using E.Standard.Security.App.Services;
using E.Standard.Security.Cryptography.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cms.Controllers;

[ApplicationSecurity]
public class DeployController : ApplicationSecurityController
{
    private readonly CmsConfigurationService _ccs;
    private readonly string _applicationContentRootPath;
    private readonly ICmsLogger _cmsLogger;
    private readonly DeployService _deployService;
    private readonly SolveWaringsService _solveWarningsService;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public DeployController(
            CmsConfigurationService ccs,
            UrlHelperService urlHelperService,
            ApplicationSecurityUserManager applicationSecurityUserManager,
            IWebHostEnvironment environment,
            ICryptoService crypto,
            CmsItemInjectionPackService instanceService,
            ICmsLogger cmsLogger,
            DeployService deployService,
            SolveWaringsService solveWarningsService,
            IEnumerable<ICustomCmsPageSecurityService> customSecurity = null)
        : base(ccs, urlHelperService, applicationSecurityUserManager, customSecurity, crypto, instanceService)
    {
        _ccs = ccs;
        _applicationContentRootPath = environment.ContentRootPath;
        _cmsLogger = cmsLogger;
        _deployService = deployService;
        _solveWarningsService = solveWarningsService;

        _servicePack = instanceService.ServicePack;
    }

    public IActionResult Index(string id = "")
    {
        try
        {
            if (_ccs.IsCustomCms(id))
            {
                return View(new DeployModel()
                {
                    CmsItem = DynamicCmsItem(id),
                    IsIFramed = Request.Query["iframe"] == "true"
                });
            }
            else
            {
                var cmsItem = _ccs.Instance.CmsItems.Where(i => i.Id == id).FirstOrDefault();
                if (cmsItem == null)
                {
                    throw new Exception("Unknown Cms-Item-Id: " + id);
                }

                return View(new DeployModel()
                {
                    CmsItem = cmsItem,
                    IsIFramed = Request.Query["iframe"] == "true"
                });
            }
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    public IActionResult Deploy(string id, string name)
    {
        try
        {
            _cmsLogger.Log(this.GetCurrentUsername(),
                           "Deploy", "Start", id, name);

            var backgroundProcess = new BackgroundProcess(id, this.GetCurrentUsername(), DeployCms, name);

            return OpenConsole(backgroundProcess, $"Deploying: {name}", id);
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    #region Background Process

    private void DeployCms(object arg)
    {
        BackgroundProcess process = (BackgroundProcess)arg;

        var context = new CmsToolContext()
        {
            CmsId = process.CmsId,
            Deployment = process.UserData,
            ContentRootPath = _applicationContentRootPath,
            Username = process.UserName
        };

        _deployService.Init(context);
        _deployService.Run(context, process);
    }

    #endregion Background Process

    public IActionResult SolveWarnings(string id, string name)
    {
        try
        {
            _cmsLogger.Log(this.GetCurrentUsername(),
                           "Warnings", "Solve_Start", id, name);

            var backgroundProcess = new BackgroundProcess(id, this.GetCurrentUsername(), SolveCmsWarnings, name);

            return OpenConsole(backgroundProcess, "Solving: " + name, id);
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    #region Background Process

    private void SolveCmsWarnings(object arg)
    {
        BackgroundProcess process = (BackgroundProcess)arg;

        var context = new CmsToolContext()
        {
            CmsId = process.CmsId,
            Deployment = process.UserData,
            ContentRootPath = _applicationContentRootPath,
            Username = process.UserName
        };

        _solveWarningsService.Run(context, process);
    }

    #endregion Background Process

    #region Helper

    private CmsConfig.CmsItem DynamicCmsItem(string id)
    {
        _ccs.InitCustomCms(_servicePack, id);

        var cmsItem = new CmsConfig.CmsItem()
        {
            Id = id,
            Name = _ccs.CMS[id].CmsDisplayName,
            Scheme = _ccs.Instance.CustomCms.Scheme,
            Path = _ccs.CMS[id].ConnectionString,
            Deployments = new CmsConfig.DeployItem[]
            {
                            new CmsConfig.DeployItem()
                            {
                                Name=_ccs.CMS[id].CmsDisplayName,
                                Target=_ccs.Instance.CustomCms.RootUrl+"/"+id+"/cms.xml",
                                PostEvents=new CmsConfig.Events()
                                {
                                    HttpGet= new string[]
                                    {
                                        _ccs.Instance.CustomCms.RootTemplate
                                    }
                                }
                            }
            }
        };

        if (_ccs.Instance?.CustomCms?.HttpPostEvents != null)
        {
            var deployment = cmsItem.Deployments.First();
            deployment.PostEvents = new CmsConfig.Events()
            {
                HttpGet = _ccs.Instance.CustomCms.HttpPostEvents
                                .Select(h => h.Replace("{cmsid}", id))
                                .ToArray()
            };
        }

        return cmsItem;
    }

    private string PrintableUrl(string url, bool isDynamic)
    {
        try
        {
            if (isDynamic)
            {
                var uri = new Uri(url);
                url = uri.PathAndQuery;
            }
        }
        catch { }

        return url;
    }

    #endregion Helper
}