using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Exceptions;
using E.Standard.Json;
using E.Standard.OpenIdConnect.Extensions;
using E.Standard.Security.App.Exceptions;
using E.Standard.Security.App.Extensions;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portal.Core.AppCode.Configuration;
using Portal.Core.AppCode.Exceptions;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Mvc;
using Portal.Core.AppCode.Services;
using Portal.Core.AppCode.Services.Authentication;
using Portal.Core.AppCode.Services.WebgisApi;
using Portal.Core.Models.Portal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Portal.Controllers;

public class HomeController : PortalBaseController
{
    private readonly ILogger<HomeController> _logger;
    private readonly ConfigurationService _config;
    private readonly WebgisCookieService _cookies;
    private readonly UploadFilesService _uploadFiles;
    private readonly WebgisApiService _api;
    private readonly ApplicationSecurityConfig _appSecurityConfig;
    private readonly UrlHelperService _urlHelper;
    private readonly ProxyService _proxy;
    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly IEnumerable<ICustomPortalSecurityService> _customSecurity;

    public HomeController(ILogger<HomeController> logger,
                          ConfigurationService config,
                          WebgisCookieService webgisCookie,
                          WebgisApiService api,
                          UploadFilesService uploadFile,
                          UrlHelperService urlHelper,
                          ProxyService proxy,
                          ICryptoService crypto,
                          SubscriberDatabaseService subscriberDb,
                          IOptions<ApplicationSecurityConfig> appSecurityConfig,
                          IEnumerable<ICustomPortalSecurityService> customSecurity = null)
        : base(logger, urlHelper, appSecurityConfig, customSecurity, crypto)
    {
        _logger = logger;
        _config = config;
        _cookies = webgisCookie;
        _api = api;
        _uploadFiles = uploadFile;
        _urlHelper = urlHelper;
        _proxy = proxy;
        _subscriberDb = subscriberDb;
        _appSecurityConfig = appSecurityConfig.Value;
        _customSecurity = customSecurity;
    }

    async public Task<IActionResult> Index(string id)
    {
        try
        {
            if (String.IsNullOrWhiteSpace(id))
            {
                return Start();
            }

            var portalUser = CurrentPortalUser();
            if (portalUser == null)
            {
                throw new NotAuthorizedException();
            }

            var portal = await _api.GetApiPortalPageAsync(this.HttpContext, id);

            if (!IsAuthorizedPortalUser(portal, portalUser))
            {
                if (_cookies.HasAuthCookie(this.HttpContext))
                {
                    throw new Exception($"Not allowed {this.User.GetUsername()} - {String.Join(",", this.User.GetRoles())}");
                }

                throw new NotAuthorizedException();
            }

            if (_config.UseFavoriteDetection() && !portalUser.IsAnonymous)
            {
                var favItems = await _subscriberDb
                                            .CreateInstance()
                                            .GetFavItemsAsync(portalUser.Username, String.Empty, "webgis.tools.portal.publish.categorymaps");
            }

            var viewName = _config.Get<string>(PortalConfigKeys.PortalHomeViewName, "Cloud");

            string scopeUrl = String.Empty, manifestUrl = String.Empty;

            if (_config.RegisterServiceWorker())
            {
                scopeUrl = $"{_urlHelper.AppRootUrl(this.Request, this).WithoutEndingSlashes()}/{id}";
                manifestUrl = String.IsNullOrEmpty(_config.ManifestRootUrl()) ?
                    $"{_urlHelper.PortalUrl()}/content/manifests/{id}/manifest.json" :
                    $"{_config.ManifestRootUrl().WithoutEndingSlashes()}/{id}/manifest.json";
            }

            return ViewResult(viewName, new PortalModel()
            {
                PortalPageId = id,
                PortalPageName = portal.Name,
                PortalPageDescription = portal.Description,
                IsMapAuthor = UserManagement.IsAllowed(portalUser.Username, portal.MapAuthors) ||
                                    UserManagement.IsAllowed(portalUser.UserRoles, portal.MapAuthors),
                IsContentAuthor = UserManagement.IsAllowed(portalUser.Username, portal.ContentAuthors) ||
                                    UserManagement.IsAllowed(portalUser.UserRoles, portal.ContentAuthors),
                IsPortalPageOwner = portalUser.Username.Equals(portal.Subscriber, StringComparison.OrdinalIgnoreCase),
                AllowUserAccessSettings = _config.Get<bool>(PortalConfigKeys.AllowSubscriberAccessPageSettings) &&
                                          portalUser.Username.Equals(portal.Subscriber, StringComparison.OrdinalIgnoreCase),
                BannerId = portal.BannerId,
                CurrentUsername = portalUser.DisplayName,
                AllowLogout = !String.IsNullOrWhiteSpace(portalUser?.Username) &&
                                (_cookies.HasAuthCookie(HttpContext) ||                                                        // Über (WebGIS) Cookie angemeldet
                                 _appSecurityConfig?.IdentityType == ApplicationSecurityIdentityTypes.OpenIdConnection ||      // Über OpenIdConnect angemeldet
                                 !String.IsNullOrEmpty(Request.Query["credentials"])),                                         // Als Admin/Subsriber angemeldet

                AllowLogin = false,                                                                                            // not support (login => with startpage or redirect)

                ScopeUrl = scopeUrl,
                ManifestUrl = manifestUrl,

                ShowOptimizationFilter = _config.PortalPageShowOptimizationFilter(),

                HtmlMetaTags = portal.HtmlMetaTags,

                ConfigBranches = await _api.GetBranches(HttpContext.Request)
            });
        }
        catch (RedirectException rde)
        {
            return RedirectResult(rde.RedirectUrl);
        }
        catch (NotAuthorizedException)
        {
            if (_appSecurityConfig.UseOpenIdConnect() ||
                _appSecurityConfig.UseAzureAD()
               )
            {
                return RedirectToAction("LoginOidc", "Auth", new { id = id });
            }
            return RedirectToActionResult("Login", "Auth", new { id = id });
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

    public IActionResult Start()
    {
        string defaultPageId = _config.DefaultPortalPageId();
        if (!String.IsNullOrWhiteSpace(defaultPageId))
        {
            return RedirectToActionResult("Index", parameters: new { id = defaultPageId });
        }

        return RedirectToActionResult("Login", "Auth");
    }

    async public Task<IActionResult> SortItems(string id, string sortingMethod, string items, string currentCategory = null)
    {
        return JsonViewSuccess(
            await _api.SortPortalItems(this.HttpContext, id, sortingMethod, items, currentCategory)
            );
    }

    //[ValidateInput(false)]
    [HttpPost]
    async public Task<IActionResult> EditContent(string id, string contentId, string content, string sorting)
    {
        return JsonObject(new
        {
            success = await _api.UpdatePortalPageContentAsync(this.HttpContext, id, contentId, content, sorting)
        });
    }

    //[ValidateInput(false)]
    [HttpPost]
    async public Task<IActionResult> RemoveContent(string id, string contentId)
    {
        return JsonObject(new
        {
            success = await _api.RemovePortalPageContentAsync(this.HttpContext, id, contentId)
        });
    }

    [HttpPost]
    async public Task<IActionResult> SortContent(string id, string sorting)
    {
        return JsonObject(new
        {
            success = await _api.UpdatePortalPageContentSortingAsync(this.HttpContext, id, sorting)
        });
    }

    async public Task<IActionResult> UploadContentImage(string id, IFormCollection form)
    {
        try
        {
            string contentId = form["contentId"];
            if (String.IsNullOrWhiteSpace(contentId))
            {
                throw new Exception("Unknown content id");
            }

            var file = _uploadFiles.GetFiles(this.Request)["content-image"];
            if (file == null)
            {
                throw new Exception("No file uploaded");
            }

            byte[] data = file.Data;

            string storageName = await _api.UploadPortalPageContentImageAsync(this.HttpContext, id, contentId, data);

            string url = "./" + id + "/ContentImage/?format=image/png&iid=" + storageName.Trim();

            return ViewResult(new PortalUploadContentModel()
            {
                ImageUrl = url
            });
        }
        catch (Exception ex)
        {
            return ExceptionView(ex);
        }
    }

    async public Task<IActionResult> ContentImage(string id, string iid)
    {
        var result = await _api.CallToolMethodBytesAsync(this.HttpContext, "WebGIS.Tools.Portal.Portal", "content-image",
            new Dictionary<string, string>()
            {
                {"id",iid}
            });

        return RawResponse(result.data, result.contentType);
    }

    [HttpPost]
    public IActionResult ChangeStyles(string id, string styles)
    {
        var styleTypes = JSerializer.Deserialize<Styles[]>(styles);

        StringBuilder sb = new StringBuilder();
        foreach (var styleType in styleTypes)
        {
            sb.Append(styleType.Style + "{");
            sb.Append(styleType.PropertyName + ":");
            sb.Append(styleType.PropertyValue + ";");
            sb.Append("}\n");
        }

        FileInfo fi = new FileInfo($"{_urlHelper.AppRootPath()}/content/portals/{id}/portal.css");
        if (!fi.Directory.Exists)
        {
            fi.Directory.Create();
        }

        System.IO.File.WriteAllText(fi.FullName, sb.ToString());

        #region Map Styles

        sb.Clear();

        foreach (string key in _mapStyles.Keys)
        {
            foreach (var portalStyleType in styleTypes.Where(m => m.Style.ToLower() == key.ToLower()))
            {
                foreach (string mapStyle in _mapStyles[key].Split(';'))
                {
                    sb.Append(mapStyle + "{");
                    sb.Append(portalStyleType.PropertyName + ":");
                    sb.Append(portalStyleType.PropertyValue + ";");
                    sb.Append("}\n");
                }
            }
        }

        fi = new FileInfo($"{_urlHelper.AppRootPath()}/content/portals/{id}/map-default.css");
        if (!fi.Directory.Exists)
        {
            fi.Directory.Create();
        }

        System.IO.File.WriteAllText(fi.FullName, sb.ToString());

        #endregion

        #region Map Builder Styles

        sb.Clear();

        foreach (string key in _mapBuilderStyles.Keys)
        {
            foreach (var portalStyleType in styleTypes.Where(m => m.Style.ToLower() == key.ToLower()))
            {
                foreach (string mapBuilderStyle in _mapBuilderStyles[key].Split(';'))
                {
                    sb.Append(mapBuilderStyle + "{");
                    sb.Append(portalStyleType.PropertyName + ":");
                    sb.Append(portalStyleType.PropertyValue + ";");
                    sb.Append("}\n");
                }
            }
        }

        fi = new FileInfo($"{_urlHelper.AppRootPath()}/content/portals/{id}/mapbuilder.css");
        if (!fi.Directory.Exists)
        {
            fi.Directory.Create();
        }

        System.IO.File.WriteAllText(fi.FullName, sb.ToString());

        #endregion

        return JsonViewSuccess(true);
    }

    async public Task<IActionResult> SecurityPrefixes(string id)
    {
        var portalUser = CurrentPortalUser();
        if (portalUser == null)
        {
            throw new NotAuthorizedException();
        }

        var portal = await _api.GetApiPortalPageAsync(this.HttpContext, id);
        if (portalUser.Username == portal.Subscriber && _config.Get<bool>(PortalConfigKeys.AllowSubscriberAccessPageSettings, false))
        {
            return JsonObject(_proxy.SecurityPrefixes(id).Select(m => m.name).ToArray());
        }

        return null;
    }

    async public Task<IActionResult> SecurityAutocomplete(string id)
    {
        var portalUser = CurrentPortalUser();
        if (portalUser == null)
        {
            throw new NotAuthorizedException();
        }

        var portal = await _api.GetApiPortalPageAsync(this.HttpContext, id);
        if (portalUser.Username == portal.Subscriber && _config.Get<bool>(PortalConfigKeys.AllowSubscriberAccessPageSettings, false))
        {
            string term = Request.Query["term"];
            string prefix = Request.Query["prefix"];

            return JsonObject(await _proxy.SecurityAutocomplete(this.Request, term, prefix));
        }

        return null;
    }

    public IActionResult NotLicensed()
    {
        return ViewResult();
    }

    // Not implementet any more
    async public Task<IActionResult> CustomLogin(string id)
    {
        try
        {
            var portal = await _api.GetApiPortalPageAsync(this.HttpContext, id);
            if (portal == null)
            {
                throw new Exception($"Unknown portal id {id}");
            }

            throw new NotImplementedException();
        }
        catch (RedirectException rde)
        {
            return RedirectResult(rde.RedirectUrl);
        }
        catch (Exception ex)
        {
            return ExceptionView(ex);
        }
    }

    async public Task<IActionResult> ServiceWorker(string id)
    {
        var serviceWorkserScript = await System.IO.File.ReadAllTextAsync($"{_urlHelper.WWWRootPath()}/webgis-serviceworker.js");

        serviceWorkserScript = serviceWorkserScript.Replace("{{version}}", WebGISVersion.Version.ToString());

        return File(Encoding.UTF8.GetBytes(serviceWorkserScript), "application/javascript");
    }

    #region Rss Feed

    public IActionResult Rss(string id)
    {
        return null; // Not Implemented
    }

    #endregion

    #region Helper

    private class Styles
    {
        [JsonProperty(PropertyName = "style")]
        [System.Text.Json.Serialization.JsonPropertyName("style")]
        public string Style { get; set; }

        [JsonProperty(PropertyName = "property")]
        [System.Text.Json.Serialization.JsonPropertyName("property")]
        public string PropertyName { get; set; }

        [JsonProperty(PropertyName = "value")]
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string PropertyValue { get; set; }
    }

    private readonly Dictionary<string, string> _mapStyles = new Dictionary<string, string>()
        {
        {".webgis-page-category-item-selected .webgis-page-category-item-title", ".webgis-presentation_toc-title;.webgis-mapcoll-category-item-selected .webgis-mapcoll-category-item-title;.webgis-button;.webgis-topbar-button;.webgis-detail-search-combo-holder;.webgis-addservices_toc-title;.webgis-services_toc-title;.webgis-search-result-header;.webgis-presentation_toc-legend-title:hover,.webgis-presentation_toc-item-group div:hover,.webgis-presentation_toc-item:hover" },
        {".webgis-page-category-item .webgis-page-category-item-title:hover",".webgis-expanded .webgis-presentation_toc-title-text;.webgis-mapcoll-category-item .webgis-mapcoll-category-item-title:hover;.webgis-expanded .webgis-addservices_toc-title-text;.webgis-expanded .webgis-services_toc-title-text;.webgis-button:hover"},
        {".webgis-page-header2",".webgis-mapcoll-map-item;.webgis-tabs-tab-header;.webgis-modal-title" },
        { ".webgis-page-map-item",".webgis-mapcoll-map-item" },
        { ".webgis-page-map-item-image",".webgis-mapcoll-map-item-image" }
        };

    private readonly Dictionary<string, string> _mapBuilderStyles = new Dictionary<string, string>()
        {
        {".webgis-page-category-item-selected .webgis-page-category-item-title", ".webgis-presentation_toc-title;.webgis-toolbox-tool-item-selected;webgis-button" },
        {".webgis-page-category-item .webgis-page-category-item-title:hover",".webgis-expanded .webgis-presentation_toc-title-text;.webgis-toolbox-tool-item-selected:hover;.webgis-botton:hover"},
        {".webgis-page-header2",".webgis-page-header2" },
        };

    #endregion
}
