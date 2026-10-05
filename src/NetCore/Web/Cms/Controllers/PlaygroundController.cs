using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Cms.AppCode.Mvc;
using Cms.AppCode.Services;
using Cms.Models;

using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.App.Reflection;
using E.Standard.Security.App.Services;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.CMS.Expressions;
using E.Standard.Parsing;

using Microsoft.AspNetCore.Mvc;

namespace Cms.Controllers;

[ApplicationSecurity]
public class PlaygroundController : ApplicationSecurityController
{
    public PlaygroundController(
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
        => View();

    public IActionResult Expressions()
        => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public IActionResult Evaluate([FromBody] ExpressionPlaygroundRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { success = false, message = "A request body is required." });
        }

        if (!Enum.TryParse<CmsExpressionType>(request.ExpressionType, true, out var expressionType)
            || !Enum.IsDefined(expressionType))
        {
            return BadRequest(new { success = false, message = "Unknown expression type." });
        }

        try
        {
            var results = ExpressionPlaygroundService.Evaluate(
                request.GeoJson,
                request.Expression,
                expressionType);
            var syntax = ExpressionPlaygroundService.ClassifySyntax(
                request.Expression,
                expressionType);

            return Json(new
            {
                success = true,
                syntax = syntax == ExpressionSyntax.StructuredExpression
                    ? "StructuredExpression"
                    : "LegacyTemplate",
                results = results.Select(result => new
                {
                    index = result.Index,
                    result = result.Result,
                    error = result.Error
                })
            });
        }
        catch (System.Text.Json.JsonException exception)
        {
            return BadRequest(new { success = false, message = exception.Message });
        }
        catch (Newtonsoft.Json.JsonException exception)
        {
            return BadRequest(new { success = false, message = exception.Message });
        }
        catch (FormatException exception)
        {
            return BadRequest(new { success = false, message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { success = false, message = exception.Message });
        }
    }
}
