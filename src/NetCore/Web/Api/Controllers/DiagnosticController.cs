using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Services;
using Api.Core.Models.Diagnostic;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.Api.App.Services.Cms;
using E.Standard.CMS.Core;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Extensions.Compare;
using E.Standard.Web.Abstractions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core.Abstraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Core.Controllers;

public class DiagnosticController : ApiBaseController
{
    private readonly ILogger<DiagnosticController> _logger;
    private readonly CacheService _cache;
    private readonly ConfigurationService _config;
    private readonly MapServiceInitializerService _mapServiceInitializer;
    private readonly UrlHelperService _urlHelper;
    private readonly CmsDocumentsService _cmsDocuments;
    private readonly IRequestContext _requestContext;

    public DiagnosticController(ILogger<DiagnosticController> logger,
                                CacheService cache,
                                ConfigurationService config,
                                MapServiceInitializerService mapServiceInitializer,
                                UrlHelperService urlHelper,
                                CmsDocumentsService cmsDocuments,
                                IHttpService http,
                                IRequestContext requestContext,
                                IEnumerable<ICustomApiService> customServices = null)
        : base(logger, urlHelper, http, customServices)
    {
        _logger = logger;
        _cache = cache;
        _config = config;
        _mapServiceInitializer = mapServiceInitializer;
        _urlHelper = urlHelper;
        _requestContext = requestContext;
        _cmsDocuments = cmsDocuments;
    }

    public IActionResult Index()
    {
        return ViewResult();
    }

    async public Task<IActionResult> Services(string pwd)
    {
        var appCachePassword = _config.AppCacheListPassword();
        if (String.IsNullOrWhiteSpace(appCachePassword) || pwd != appCachePassword)
        {
            return await JsonViewSuccess(false, "not allowed");
        }

        try
        {
            Dictionary<string, CmsDocument> cmsDocumentCache = new Dictionary<string, CmsDocument>();

            string[] serviceUrls = _cache.GetServices(null).Select(m => m.Url).ToArray();

            var map = _mapServiceInitializer.Map(_requestContext, null);

            var rootElement = new DiagnosticElement() { Name = "Services" };

            foreach (var serviceUrl in serviceUrls)
            {
                IMapService service = null;
                try
                {
                    service = await _cache.GetService(serviceUrl, map, null, _urlHelper);
                }
                catch (Exception ex)
                {
                    rootElement.AddElement(new DiagnosticElement() { Name = serviceUrl, Text = ex.Message, Status = DiagnosticElementStatus.Missing });
                    continue;
                }

                if (service == null)
                {
                    rootElement.AddElement(new DiagnosticElement() { Name = serviceUrl, Status = DiagnosticElementStatus.Missing });
                    continue;
                }

                var serviceElement = new DiagnosticElement() { Name = service.Name + " (" + service.Url + ")" };

                string cmsName = serviceUrl.Contains("@") ? serviceUrl.Split('@')[1] : "";
                CmsDocument cmsDocument = null;

                if (cmsDocumentCache.ContainsKey(cmsName))
                {
                    cmsDocument = cmsDocumentCache[cmsName];
                }
                else
                {
                    cmsDocument = _cmsDocuments.GetCmsDocument(cmsName);

                    if (cmsDocument == null)
                    {
                        serviceElement.Status = DiagnosticElementStatus.CmsMissing;
                        continue;
                    }

                    cmsDocumentCache[cmsName] = cmsDocument;
                }

                CmsHlp cmsHlp = new CmsHlp(cmsDocument, map);

                #region Chech Themes

                var themeNodes = cmsHlp.GetServiceThemes(service.Url.Split('@')[0]);

                if (themeNodes != null && service.Layers != null)
                {
                    foreach (var themeNode in themeNodes)
                    {
                        var layer = service.Layers.Where(m => m.ID == themeNode.Id).FirstOrDefault();

                        if (layer == null)
                        {
                            serviceElement.AddElement(new DiagnosticElement()
                            {
                                Name = themeNode.Name,
                                Status = DiagnosticElementStatus.Missing
                            });
                        }
                        else if (layer.Name != themeNode.Name)
                        {
                            serviceElement.AddElement(new DiagnosticElement()
                            {
                                Name = themeNode.Name,
                                Text = themeNode.Name + " <> " + layer.Name,
                                Status = DiagnosticElementStatus.NameConfusion
                            });
                        }
                    }
                }

                #endregion

                rootElement.AddElement(serviceElement);
            }

            rootElement.RemoveElements(DiagnosticElementStatus.Ok);
            return await JsonObject(rootElement);
        }
        catch (Exception ex)
        {
            return await ThrowJsonException(ex);
        }
    }

    async public Task<IActionResult> CmsTree(string pwd, string username = "", string roles = "", string instanceRoles = "")
    {
        var appCachePassword = _config.AppCacheListPassword();
        if (String.IsNullOrWhiteSpace(appCachePassword) || pwd != appCachePassword)
        {
            return await JsonViewSuccess(false, "not allowed");
        }

        try
        {
            CmsDocument.UserIdentification ui = new CmsDocument.UserIdentification(
                    username,
                    roles?.Split(','), null,
                    instanceRoles?.Split(','));

            string[] serviceUrls = _cache.GetServices(ui).Select(m => m.Url).ToArray();

            var result = new DiagnosticTreeElement()
            {
                //Name = "AuthTree",
                Elements = new Dictionary<string, Dictionary<string, DiagnosticTreeElement>>()
            };

            foreach (var serviceUrl in serviceUrls)
            {
                var service = _cache.GetOriginalIgnoreInitialisation(serviceUrl, ui);
                if (service != null)
                {
                    var cmsName = GetCmsName(serviceUrl);

                    if (!result.Elements.ContainsKey($"CMS: {cmsName}"))
                    {
                        result.Elements[$"CMS: {cmsName}"] = new Dictionary<string, DiagnosticTreeElement>();
                    }
                    var cmsDiagnosticElement = result.Elements[$"CMS: {cmsName}"];

                    var serviceDiagnosticElement = new DiagnosticTreeElement();
                    cmsDiagnosticElement[$"SERVICE: {service.Name}"] = serviceDiagnosticElement;

                    _cache.GetEditThemes(serviceUrl, ui).editthemes.EachIfNotNull((theme) =>
                    {
                        serviceDiagnosticElement.GetElements().Add($"EDITTHEME: {theme.Name}", null);
                    });
                    _cache.GetQueries(serviceUrl, ui).queries.EachIfNotNull((query) =>
                    {
                        serviceDiagnosticElement.GetElements().Add($"QUERY: {query.Name}", null);
                    });
                }
            }

            return await JsonObject(result);

        }
        catch (Exception ex)
        {
            return await ThrowJsonException(ex);
        }
    }

    private string GetCmsName(string id)
    {
        if (!id.Contains("@"))
        {
            return String.Empty;
        }

        return id.Split('@')[1];
    }
}