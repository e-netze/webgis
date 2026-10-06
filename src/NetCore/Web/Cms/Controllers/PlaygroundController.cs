using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

using Cms.AppCode.Mvc;
using Cms.AppCode.Services;
using Cms.Models;

using E.Standard.Cms.Configuration.Models;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Extensions;
using E.Standard.Cms.Services;
using E.Standard.CMS.Core;
using E.Standard.CMS.Schema;
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
    private readonly CmsConfigurationService _cmsConfiguration;
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public PlaygroundController(
        CmsConfigurationService ccs,
        UrlHelperService urlHelperService,
        ApplicationSecurityUserManager applicationSecurityUserManager,
        ICryptoService crypto,
        CmsItemInjectionPackService instanceService,
        IEnumerable<ICustomCmsPageSecurityService> customSecurity = null)
        : base(ccs, urlHelperService, applicationSecurityUserManager, customSecurity, crypto, instanceService)
    {
        _cmsConfiguration = ccs;
        _servicePack = instanceService.ServicePack;
    }

    public IActionResult Index()
        => View();

    public IActionResult Expressions()
        => View();

    public IActionResult Regex()
        => View();

    public IActionResult EditForm()
    {
        return View(new EditFormPlaygroundViewModel { CmsItems = GetPlaygroundCmsItems() });
    }

    [HttpGet]
    public IActionResult EditFormThemes(string cmsId)
    {
        if (!TryGetCmsDocument(cmsId, out var cmsDocument))
        {
            return BadRequest(new { success = false, message = "Unknown CMS configuration." });
        }

        var themes = EditFormPlaygroundService.GetThemes(
            cmsDocument.ToXml(_servicePack, appendConfigs: true, recursive: true));

        return Json(new
        {
            success = true,
            themes = themes.Select(theme => new
            {
                path = theme.Path,
                name = theme.Name,
                serviceTheme = theme.ServiceTheme,
                fields = theme.Fields.Select(field => new
                {
                    id = field.Id,
                    name = field.Name,
                    fieldName = field.FieldName,
                    category = field.Category,
                    fieldType = field.FieldType,
                    visible = field.Visible,
                    readOnly = field.Readonly,
                    required = field.Required,
                    autoValue = field.AutoValue
                })
            })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public IActionResult EvaluateEditForm([FromBody] EditFormPlaygroundRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { success = false, message = "A request body is required." });
        }

        if (!TryGetCmsDocument(request.CmsId, out var cmsDocument))
        {
            return BadRequest(new { success = false, message = "Unknown CMS configuration." });
        }

        try
        {
            var results = EditFormPlaygroundService.Evaluate(
                GetResolvedCmsXml(request.CmsId, cmsDocument, request.Deployment),
                request);

            return Json(new
            {
                success = true,
                results
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(512 * 1024)]
    public IActionResult EvaluateRegex([FromBody] RegexPlaygroundRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { success = false, message = "A request body is required." });
        }

        try
        {
            return Json(new { success = true, result = RegexPlaygroundService.Evaluate(request) });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { success = false, message = exception.Message });
        }
        catch (TimeoutException exception)
        {
            return BadRequest(new { success = false, code = "timeout", message = exception.Message });
        }
    }

    // Resolves CMS secrets and replacement files like a deploy would, so AutoValues
    // using "{{secret-...}}" placeholders can be simulated for a chosen deployment.
    // The resolved document is only used server-side and never returned to the client.
    private XmlDocument GetResolvedCmsXml(string cmsId, CMSManager cmsDocument, string deploymentName)
    {
        var xml = cmsDocument.ToXml(_servicePack, appendConfigs: true, recursive: true);
        var cmsItem = _cmsConfiguration.Instance.CmsItems?.FirstOrDefault(item => item.Id == cmsId);
        if (cmsItem is null)
        {
            return xml;
        }

        CmsConfig.DeployItem deploy;
        if (String.IsNullOrWhiteSpace(deploymentName))
        {
            deploy = new CmsConfig.DeployItem { Environment = DeployEnvironment.Default };
        }
        else
        {
            deploy = cmsItem.Deployments?.FirstOrDefault(item => item.Name == deploymentName)
                ?? throw new ArgumentException("Unknown deployment.");
        }

        var replace = new CmsReplace();
        var replaceActions = new List<Action> { () => replace.AddCmsSecrets(cmsItem, deploy) };
        if (!String.IsNullOrEmpty(deploy.ReplacementFile))
        {
            replaceActions.Insert(
                deploy.ReplceSecretsFirst ? 1 : 0,
                () => replace.AddReplacementFile(deploy.ReplacementFile));
        }
        foreach (var replaceAction in replaceActions)
        {
            replaceAction();
        }

        if (replace.HasItems)
        {
            replace.ReplaceInXmlDocument(xml);
        }

        return xml;
    }

    private bool TryGetCmsDocument(
        string cmsId,
        out CMSManager cmsDocument)
    {
        cmsDocument = null;
        return !String.IsNullOrWhiteSpace(cmsId)
            && _cmsConfiguration.CMS.TryGetValue(cmsId, out cmsDocument);
    }

    private EditFormPlaygroundCmsItem[] GetPlaygroundCmsItems()
        => _cmsConfiguration.Instance.CmsItems?
            .Where(item => !String.IsNullOrWhiteSpace(item.Id)
                && _cmsConfiguration.CMS.ContainsKey(item.Id))
            .Select(item => new EditFormPlaygroundCmsItem(
                item.Id,
                item.Name,
                item.Deployments?
                    .Select(deploy => deploy.Name)
                    .Where(name => !String.IsNullOrWhiteSpace(name))
                    .ToArray() ?? Array.Empty<string>()))
            .ToArray()
            ?? Array.Empty<EditFormPlaygroundCmsItem>();

}
