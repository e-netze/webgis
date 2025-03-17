using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Exceptions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.Security.App.Exceptions;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Mvc;
using Portal.Core.AppCode.Services;
using Portal.Core.AppCode.Services.Authentication;
using Portal.Core.AppCode.Services.WebgisApi;
using Portal.Core.Models.Map;
using Portal.Core.Models.Portal;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal.Core.Controllers;

public class MapController : PortalBaseController
{
    private readonly ILogger<MapController> _logger;
    private readonly ConfigurationService _config;
    private readonly WebgisApiService _api;
    private readonly UrlHelperService _urlHelper;
    private readonly HmacService _hmac;
    private readonly UploadFilesService _uploadFiles;
    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly ViewerLayoutService _viewerLayout;
    private readonly IEnumerable<ICustomPortalService> _customServices;
    private readonly IEnumerable<ICustomPortalSecurityService> _customSecurity;

    public MapController(ILogger<MapController> logger,
                         ConfigurationService config,
                         WebgisApiService apiService,
                         UrlHelperService urlHelper,
                         HmacService hmac,
                         UploadFilesService uploadFiles,
                         SubscriberDatabaseService subscriberDb,
                         ViewerLayoutService viewerLayout,
                         ICryptoService crypto,
                         IOptionsMonitor<ApplicationSecurityConfig> appSecurityConfig,
                         IEnumerable<ICustomPortalService> customServices = null,
                         IEnumerable<ICustomPortalSecurityService> customSecurity = null)
        : base(logger, urlHelper, appSecurityConfig, customSecurity, crypto)
    {
        _logger = logger;
        _config = config;
        _api = apiService;
        _urlHelper = urlHelper;
        _hmac = hmac;
        _uploadFiles = uploadFiles;
        _viewerLayout = viewerLayout;
        _subscriberDb = subscriberDb;
        _customServices = customServices;
        _customSecurity = customSecurity;
    }

    //[ValidateInput(false)]
    async public Task<IActionResult> Index(
            string id,
            string category = "",
            string map = "",
            string collection = "")
    {
        try
        {
            if (map.StartsWith("app@"))
            {
                return RedirectToActionResult("PortalApp", "App", new { id = id, app = map.Substring(4), category = category });
            }

            category = category.Replace("~", " ");
            map = map.Replace("~", " ");  // Anstatt von leerzeichen können in der Url auch ~ übergeben werden...

            var portalUser = CurrentPortalUserOrThrowIfRequired(_config.AllowAnonymousSecurityMethod());

            var portalPage = await _api.GetApiPortalPageAsync(this.HttpContext, id);

            if (!IsAuthorizedPortalUser(portalPage, portalUser))
            {
                throw new NotAuthorizedException();
            }

            #region Hmac Object

            string hmacRequestObject = $"'{_urlHelper.AppRootUrl(this.Request, this).WithoutEndingSlashes()}/hmac'";

            if (portalUser.Username == portalPage.Subscriber && !String.IsNullOrWhiteSpace(portalPage.SubscriberClientId))
            {
                hmacRequestObject = $"'{portalPage.SubscriberClientId}'";
            }
            else if (!String.IsNullOrEmpty(_customSecurity.UsernameToHmacClientId(portalUser.Username)))
            {
                hmacRequestObject = _customSecurity.UsernameToHmacClientId(portalUser.Username);
            }
            else if (_config.UseLocalUrlSchema() || new Uri(Request.GetDisplayUrl()).Scheme.ToLower() == "https")
            {
                var hmac = await _hmac.CreateHmacObjectAsync(portalUser);
                hmacRequestObject = hmac.ToJs();
            }

            #endregion

            if (!portalUser.IsAnonymous &&
                _config.UseFavoriteDetection() &&
                !String.IsNullOrWhiteSpace(map) &&
                map != "undefined")
            {
                var subscriberDb = _subscriberDb.CreateInstance();
                await subscriberDb.SetFavItemAsync(portalUser.Username, Const.DefaultFavoriteTask, "WebGIS.Tools.Portal.Publish.CategoryMaps", category + "/" + map);
            }

            String credits = String.Empty;
            try
            {
                credits = System.IO.File.ReadAllText($"{_urlHelper.AppRootPath()}/credits.html").Replace("{api}", _urlHelper.ApiUrl(this.Request, HttpSchema.Current));
            }
            catch { }

            this.Title = map;

            await _customServices.LogMapRequest(id, category, map, portalUser?.Username);

            string serializtaionMap = null, serializationCategory = null, mapMessage = null;
            if (category == E.Standard.WebGIS.Core.Serialization.SharedMapMeta.SharedMapsCategory)
            {
                var sharedMapMetadata = await _api.GetApiSharedMapMetadata(this.HttpContext, String.IsNullOrWhiteSpace(id) ? Request.Query["page"].ToString() : id, map);
                if (sharedMapMetadata == null || sharedMapMetadata.IsExpired)
                {
                    throw new Exception("Der Link für diesen Kartenaufruf ist leider abgelaufen...");
                }

                serializationCategory = category;
                serializtaionMap = map;

                category = sharedMapMetadata.MapCategory;
                map = sharedMapMetadata.MapName;

                if (sharedMapMetadata.UserMessageRecommended)
                {
                    var timeSpan = sharedMapMetadata.Expires - DateTime.UtcNow;
                    mapMessage = String.Format("Hinweis: Der Kartenaufruf über diesen Link ist noch {0:%d} Tag(e), {1:%h} Stunde(n) und {2:%m} Minute(n) gültig", timeSpan, timeSpan, timeSpan);
                }
            }

            bool queryLayout = _config.QueryCustomMapLayout() || Request.Query["querylayout"] == "true";
            if (Request.Query["querylayout"] == "false")
            {
                queryLayout = false;
            }

            return ViewResult(new MapModel()
            {
                HMACObject = hmacRequestObject,  //Webgis5Globals.UrlScheme(this.Request) + "localhost/webgis5/hmac",
                PageId = String.IsNullOrWhiteSpace(id) ? Request.Query["page"].ToString() : id,
                PageName = portalPage.Name,
                Category = category,
                MapName = map,
                IsPortalMapAuthor = IsAuthorizedPortalMapAuthor(portalPage, portalUser),
                Description = await GetMapDescription(id, category, map, portalUser.Username == portalPage.Subscriber),
                ProjectName = Request.Query["project"],
                CalcCrs = _config.ConfigCalcCrs(),
                Parameters = await MapParameters.CreateAsync(HttpContext, _api),
                Credits = credits,
                QueryLayout = queryLayout,
                QueryMaster = _config.AllowMapUIMaster(),
                PortalUrl = _urlHelper.AppRootUrl(this.Request, this).WithoutEndingSlashes(),
                GdiCustomScheme = !String.IsNullOrWhiteSpace(Request.Query["gdischeme"]) ? Request.Query["gdischeme"].ToString() : String.Empty,
                CurrentUsername = portalUser?.Username ?? String.Empty,
                SerializationMapName = serializtaionMap,
                SerializationCategory = serializationCategory,
                MapMessage = mapMessage,
                ShowNewsTipsSinceVesion = _config.ShowNewsTipsSinceVersion(),
                AddCustomCss = _config.AddCss(map),
                AddCustomJavascript = _config.AddJs(map),
                Language = Request.Query["language"]
                //,HtmlMetaTags = await _api.GetMapHtmlMetaTags(this.HttpContext, id, category, map) => wird zur zZ nicht verwendet. Ist einmal vorbereitet, die Frage ist, ob das wer braucht
            });
        }
        catch (NotAuthorizedException)
        {
            return base.HandleNotAuthorizedException(id);
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

    #region MapLayout

    async public Task<IActionResult> Layout(string id, int width, int height, string template)
    {
        return JsonObject(new
        {
            html = await _viewerLayout.GetLayoutAsync(id, width, height, template),
            succeeded = true
        });
    }

    async public Task<IActionResult> LayoutTemplates(string id, int width)
    {
        var templates = await _viewerLayout.GetLayoutTemplatesAsync(id, width);

        //return File(System.Text.Encoding.UTF8.GetBytes(
        //    System.Text.Json.JsonSerializer.Serialize(templates)), "application/json");

        return JsonObject(templates);
    }

    #endregion

    [HttpPost]
    async public Task<IActionResult> UploadMapImage(string id, IFormCollection form)
    {
        try
        {
            string map = form["map"];
            if (String.IsNullOrWhiteSpace(map))
            {
                throw new Exception("Unknown map");
            }

            string category = form["category"];
            if (String.IsNullOrWhiteSpace(category))
            {
                throw new Exception("Unknown category");
            }

            var file = _uploadFiles.GetFiles(Request)["map-image"];
            if (file == null)
            {
                throw new Exception("No file uploaded");
            }

            byte[] data = file.Data;

            string storageName = await _api.UploadMapImageAsync(HttpContext, id, category, map, data);

            string url = "../../mapImage/?category=" + category + "&map=" + map + "&t=" + DateTime.UtcNow.Ticks;

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

    async public Task<IActionResult> MapImage(string id, string category, string map)
    {
        var result = await _api.CallToolMethodBytesAsync(HttpContext, "WebGIS.Tools.Portal.Publish", "map-image",
            new Dictionary<string, string>()
            {
                {"page-publish-page-id", id},
                {"page-publish-category", category},
                {"map",map}
            });

        return RawResponse(result.data, result.contentType);
    }

    [HttpPost]
    async public Task<IActionResult> UpdateMapDescription(string id, string map, string category, string description)
    {
        string result = await _api.CallToolMethodAsync(HttpContext, "WebGIS.Tools.Portal.Publish", "map-update-description",
           new Dictionary<string, string>()
           {
                {"page-publish-page-id", id},
                {"page-publish-category", category},
                {"map",map},
                {"description", description }
           });

        if (!String.IsNullOrWhiteSpace(result) && result.Trim().StartsWith("{") && result.Contains("\"exception\":"))
        {
            return JsonViewSuccess(false);
        }

        return JsonObject(new
        {
            success = true,
            description = await GetMapDescription(id, category, map, true)
        });
    }

    [HttpPost]
    async public Task<IActionResult> DeleteMap(string id, string map, string category)
    {
        string result = await _api.CallToolMethodAsync(HttpContext, "WebGIS.Tools.Portal.Publish", "delete-map",
           new Dictionary<string, string>()
           {
                {"page-publish-page-id", id},
                {"page-publish-category", category},
                {"map",map},
           });

        if (!String.IsNullOrWhiteSpace(result) && result.Trim().StartsWith("{") && result.Contains("\"exception\":"))
        {
            return JsonViewSuccess(false);
        }

        return JsonViewSuccess(true);
    }

    #region Helper

    async private Task<string> GetMapDescription(string id, string category, string map, bool raw)
    {
        string description = await _api.CallToolMethodAsync(this.HttpContext, "WebGIS.Tools.Portal.Publish", "map-description",
           new Dictionary<string, string>()
           {
                {"page-publish-page-id", id},
                {"page-publish-category", category},
                {"map",map},
                {"raw",raw.ToString() }
           });

        if (String.IsNullOrWhiteSpace(description))
        {
            return String.Empty;
        }

        foreach (char c in "'\r")
        {
            description = description.Replace(c.ToString(), "");
        }

        return description.Replace("\n", "\\n");
    }

    #endregion
}