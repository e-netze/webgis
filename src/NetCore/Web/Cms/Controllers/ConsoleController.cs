using Cms.AppCode;
using Cms.AppCode.Extensions;
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
using System.Linq;

namespace Cms.Controllers;

[ApplicationSecurity]
public class ConsoleController : ApplicationSecurityController
{
    public ConsoleController(
            CmsConfigurationService ccs,
            UrlHelperService urlHelperService,
            ApplicationSecurityUserManager applicationSecurityUserManager,
            ICryptoService crypto,
            CmsItemInjectionPackService instanceService,
            IEnumerable<ICustomCmsPageSecurityService> customSecurity = null)
        : base(ccs, urlHelperService, applicationSecurityUserManager, customSecurity, crypto, instanceService)
    {

    }

    public IActionResult Index(string procId, string title, string cmsId)
    {
        try
        {
            return View(new ConsoleModel()
            {
                ProcId = procId,
                Title = title
            });
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    public IActionResult Ping(string procId)
    {
        try
        {
            var process = BackgroundProcess.CurrentProcesses.Where(p => p.ProcId == procId).FirstOrDefault();
            if (process == null)
            {
                throw new Exception("Unknown Process");
            }

            IEnumerable<string> lines = process.ReadLines();

            return Json(new { success = true, lines = lines, hasfile = process.HasFile });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    public IActionResult DownloadFile(string procId)
    {
        try
        {
            var process = BackgroundProcess.CurrentProcesses.Where(p => p.ProcId == procId).FirstOrDefault();
            if (process == null)
            {
                throw new Exception("Unknown Process");
            }

            var file = process.TakeFileAndReleaseProcess();
            if (file == null)
            {
                throw new Exception("File not exists");
            }

            return file.Result(this);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    public IActionResult Cancel(string procId)
    {
        try
        {
            var process = BackgroundProcess.CurrentProcesses.Where(p => p.ProcId == procId).FirstOrDefault();
            if (process == null)
            {
                throw new Exception("Unknown Process");
            }

            process.Cancel();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}