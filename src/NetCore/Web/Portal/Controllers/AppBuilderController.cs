using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Exceptions;
using E.Standard.Json;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Mvc;
using Portal.Core.AppCode.Services;
using Portal.Core.AppCode.Services.Authentication;
using Portal.Core.AppCode.Services.WebgisApi;
using Portal.Core.Models.AppBuilder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Core.Controllers;

public class AppBuilderController : PortalBaseController
{
    private readonly ILogger<AppBuilderController> _logger;
    private readonly ConfigurationService _config;
    private readonly WebgisApiService _api;
    private readonly UrlHelperService _urlHelper;
    private readonly HmacService _hmac;
    private readonly ICryptoService _crypto;

    public AppBuilderController(ILogger<AppBuilderController> logger,
                                ConfigurationService config,
                                WebgisApiService api,
                                UrlHelperService urlHelper,
                                HmacService hmac,
                                ICryptoService crypto,
                                IOptionsMonitor<ApplicationSecurityConfig> appSecurityConfig,
                                IEnumerable<ICustomPortalSecurityService> customSecurity = null)
        : base(logger, urlHelper, appSecurityConfig, customSecurity, crypto)
    {
        _logger = logger;
        _config = config;
        _api = api;
        _urlHelper = urlHelper;
        _hmac = hmac;
        _crypto = crypto;
    }

    async public Task<IActionResult> Index(string id, string clientid = "")
    {
        try
        {
            List<string> templates = new List<string>();

            var portalUser = CurrentPortalUser();

            DirectoryInfo di = new DirectoryInfo($"{_urlHelper.AppRootPath()}/_templates");
            foreach (var fi in di.GetFiles("*.html"))
            {
                templates.Add(fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length));
            }

            if (String.IsNullOrWhiteSpace(clientid))
            {
                clientid = $"'{_urlHelper.AppRootUrl(this.Request, this).RemoveEndingSlashes()}/hmac'"; // Webgis5Globals.AppRootUrl(this.Request) + "hmac";

                if (_config.UseLocalUrlSchema() || Request.Scheme == "https")
                {
                    var hmac = await _hmac.CreateHmacObjectAsync(CurrentPortalUser());
                    clientid = hmac.ToJs();
                }
            }
            else
            {
                //clientid = "'" + clientid + "'";
            }

            bool isPortalOwner = false;

            if (String.IsNullOrWhiteSpace(id) || id.IsNonePortalId())
            {
                id = String.Empty;
            }
            else if (id.Length == 1)
            {
                throw new Exception("Invalid Portal Id");
            }
            else
            {
                var portal = await _api.GetApiPortalPageAsync(HttpContext, id);
                if (portal == null)
                {
                    throw new Exception("Unknown Poral");
                }

                if (!IsAuthorizedPortalMapAuthor(portal, portalUser))
                {
                    throw new Exception("Map authering is not allowed for this portal");
                }

                isPortalOwner = IsPortalOwner(portal, portalUser);
            }

            ApiAppDTO apiApp = null;
            string appDescription = String.Empty;

            if (!String.IsNullOrWhiteSpace(Request.Query["category"]) && !String.IsNullOrWhiteSpace(Request.Query["app"]))
            {
                apiApp = await _api.GetApiPortalAppAsync(HttpContext, id, Request.Query["category"], Request.Query["app"]);
                appDescription = await _api.GetApiPortalAppDescriptionAsync(HttpContext, id, Request.Query["category"], Request.Query["app"]);

                if (apiApp.Creator.ToLower() != portalUser.Username.ToLower())
                {
                    throw new Exception("Not allowed. You are not the creator of this app!");
                }
            }

            #region Parameter from original template

            //try
            //{
            if (apiApp != null && new FileInfo($"{_urlHelper.AppRootPath()}/_templates/{apiApp.Template}.mta").Exists)
            {
                List<ApiAppDTO.Parameter> parameters = new List<ApiAppDTO.Parameter>();
                foreach (var parameter in JSerializer.Deserialize<ApiAppDTO>(
                    System.IO.File.ReadAllText($"{_urlHelper.AppRootPath()}/_templates/{apiApp.Template}.mta")).TemplateParameters ?? new ApiAppDTO.Parameter[0])
                {
                    var appParameter = apiApp.TemplateParameters.Where(m => m.Name == parameter.Name).FirstOrDefault();
                    parameters.Add(parameter);
                    if (appParameter != null)
                    {
                        parameters.Last().Value = appParameter.Value;
                    }
                }
                apiApp.TemplateParameters = parameters.ToArray();
            }
            //} catch { }

            #endregion

            return ViewResult(new AppBuilderInit(_crypto)
            {
                Templates = templates.ToArray(),
                HMACObject = clientid,
                PortalId = id,
                IsPortalOwner = isPortalOwner,
                PortalCategory = Request.Query["category"],
                AppName = Request.Query["app"],
                AppMetadataJson = JSerializer.Serialize(new { description = appDescription }),
                AppJson = apiApp != null ? JSerializer.Serialize(apiApp) : "null",
                CurrentUsername = portalUser?.Username ?? String.Empty
            });
        }
        catch (NotLicensedException)
        {
            return NotlicendedExceptionView();
        }
        catch (Exception ex)
        {
            return ExceptionView(ex);
        }
    }

    public IActionResult TemplateMeta(string id, string template)
    {
        FileInfo fi = new FileInfo($"{_urlHelper.AppRootPath()}/_templates/{template}.mta");

        if (fi.Exists)
        {
            return PlainView(System.IO.File.ReadAllText(fi.FullName), "application/json; charset=utf-8");
        }

        return PlainView("{}", "application/json; charset=utf-8");
    }
}