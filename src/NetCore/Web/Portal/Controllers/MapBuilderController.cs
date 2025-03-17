using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Exceptions;
using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Mvc;
using Portal.Core.AppCode.Services;
using Portal.Core.AppCode.Services.Authentication;
using Portal.Core.AppCode.Services.WebgisApi;
using Portal.Core.Models.MapBuilder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Portal.Core.Controllers;

public class MapBuilderController : PortalBaseController
{
    private readonly ILogger<MapBuilderController> _logger;
    private readonly ConfigurationService _config;
    private readonly WebgisApiService _api;
    private readonly UrlHelperService _urlHelper;
    private readonly HmacService _hmac;
    private readonly CustomContentService _customContent;
    private readonly ICryptoService _crypto;

    public MapBuilderController(ILogger<MapBuilderController> logger,
                                ConfigurationService config,
                                WebgisApiService api,
                                UrlHelperService urlHelper,
                                HmacService hmac,
                                CustomContentService customContent,
                                IOptionsMonitor<ApplicationSecurityConfig> appSecurityConfig,
                                ICryptoService crypto,
                                IEnumerable<ICustomPortalSecurityService> customSecurity = null)
        : base(logger, urlHelper, appSecurityConfig, customSecurity, crypto)
    {
        _logger = logger;
        _config = config;
        _api = api;
        _urlHelper = urlHelper;
        _hmac = hmac;
        _customContent = customContent;
        _crypto = crypto;
    }

    async public Task<IActionResult> Index(string id, string clientid = "")
    {
        try
        {
            List<string> templates = new List<string>();

            var portalUser = CurrentPortalUser();

            DirectoryInfo di = new DirectoryInfo($"{_urlHelper.AppRootPath()}/_mapbuilder");
            if (di.Exists)
            {
                foreach (var fi in di.GetFiles("template-*.razortemplate"))
                {
                    templates.Add(fi.Name.Substring(9, fi.Name.Length - fi.Extension.Length - 9));
                }
            }

            if (String.IsNullOrWhiteSpace(clientid))
            {
                clientid = $"'{_urlHelper.AppRootUrl(Request, this).RemoveEndingSlashes()}/hmac'"; // Webgis5Globals.AppRootUrl(this.Request) + "hmac";

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
                var portalPage = await _api.GetApiPortalPageAsync(HttpContext, id);
                if (portalPage == null)
                {
                    throw new Exception("Unknown Poral");
                }

                if (!IsAuthorizedPortalMapAuthor(portalPage, portalUser))
                {
                    throw new Exception("Map authering is not allowed for this portal");
                }

                isPortalOwner = IsPortalOwner(portalPage, portalUser);

                if (portalUser.Username == portalPage.Subscriber && !String.IsNullOrWhiteSpace(portalPage.SubscriberClientId))
                {
                    clientid = portalPage.SubscriberClientId;
                }
            }

            return ViewResult(new MapBuilderInit(_crypto)
            {
                Templates = templates.ToArray(),
                HMACObject = clientid,
                PortalId = id,
                IsPortalOwner = isPortalOwner,
                PortalCategory = Request.Query["category"],
                MapName = Request.Query["map"],
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

    #region MapBuilder Defaults

    public IActionResult Defaults(string id)
    {
        if (String.IsNullOrWhiteSpace(id))
        {
            id = Const.NonePortalId;
        }

        foreach (string portalId in new string[] { id, Const.NonePortalId })
        {
            FileInfo fi = new FileInfo(DefaultsFilePath(portalId));
            if (fi.Exists)
            {
                return PlainView(System.IO.File.ReadAllText(fi.FullName), "application/json; charset=utf-8");
            }
        }

        return PlainView("{}", "application/json; charset=utf-8");
    }

    [HttpPost]
    async public Task<IActionResult> SetDefaults(string id, string defaults)
    {
        try
        {
            var portalUser = CurrentPortalUser();
            if (portalUser == null)
            {
                throw new Exception("Not authorized");
            }

            var portal = await _api.GetApiPortalPageAsync(HttpContext, id);
            if (portal == null)
            {
                throw new Exception("Unknown Poral");
            }

            if (!IsPortalOwner(portal, portalUser))
            {
                throw new Exception("You (" + portalUser.Username + ") are not the portal owner");
            }

            FileInfo fi = new FileInfo(DefaultsFilePath(id));
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            System.IO.File.WriteAllText(fi.FullName, defaults);

            return JsonObject(new
            {
                success = true
            });
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    #region Helper

    private string DefaultsFilePath(string id)
    {
        string root = !String.IsNullOrWhiteSpace(_customContent.PortalCustomContentRootPath) ?
            _customContent.PortalCustomContentRootPath :
            _urlHelper.AppRootPath();

        return $"{root}/mapbuilder/{id}/defaults.json";
    }

    #endregion

    #endregion

    #region Master

    async public Task<IActionResult> Master(string id, string category)
    {
        if (String.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("portal id is empty");
        }

        var nvc = new Dictionary<string, string>();
        nvc["page-id"] = id;
        nvc["category"] = category;

        string result1 = await _api.CallToolMethodAsync(HttpContext, "WebGIS.Tools.Portal.Master", "get-master", nvc);

        nvc["category"] = "";

        string result0 = await _api.CallToolMethodAsync(HttpContext, "WebGIS.Tools.Portal.Master", "get-master", nvc);

        return JsonObject(new
        {
            master1 = result1,
            master0 = result0
        });
    }

    async public Task<IActionResult> SetMaster(string id, string category, string master)
    {
        try
        {
            var portalUser = CurrentPortalUser();
            if (portalUser == null)
            {
                throw new Exception("Not authorized");
            }

            var portal = await _api.GetApiPortalPageAsync(HttpContext, id);
            if (portal == null)
            {
                throw new Exception("Unknown Poral");
            }

            if (!IsPortalOwner(portal, portalUser))
            {
                throw new Exception("You (" + portalUser.Username + ") are not the portal owner");
            }

            var nvc = new Dictionary<string, string>();
            nvc["page-id"] = id;
            nvc["category"] = category;
            nvc["master"] = master;

            await _api.CallToolMethodAsync(HttpContext, "WebGIS.Tools.Portal.Master", "set-master", nvc);

            return JsonObject(new
            {
                success = true
            });
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    #endregion

    public IActionResult CreateTemplate(string id, string template)
    {
        var model = new MapBuilderTemplateModel()
        {
            ClientId = this.Request.Form["clientid"].ToString().Split(':')[0],
            Extent = this.Request.Form["extent"],
            Services = this.Request.Form["services"],
            UI = this.Request.Form["ui"],
            ToolsQuickAccess = this.Request.Form["tools_quick_access"],
            MapScale = this.Request.Form["scale"].ToPlatformDouble(),
            MapCenter = new double[] {
                                        this.Request.Form["centerLng"].ToPlatformDouble(),
                                        this.Request.Form["centerLat"].ToPlatformDouble()
                                     },
            Queries = String.IsNullOrWhiteSpace(this.Request.Form["queries"]) ? null : this.Request.Form["queries"].ToString().Replace("\"service\":", "service:").Replace("\"query\":", "query:").Replace("\"visible\":", "visible:"),
            Tools = this.Request.Form["tools"].ToString().Replace("\"name\":", "name:").Replace("\"tools\":", "tools:"),
            ApiUrl = _urlHelper.ApiUrl(this.Request),
            ApiUrlHttps = _urlHelper.ApiUrl(this.Request, HttpSchema.Https),
            DynamicContent = this.Request.Form["dynamiccontent"],
            Graphics = this.Request.Form.FormatGraphicsForHtmlTemplate(),
            Visibilities = String.IsNullOrWhiteSpace(this.Request.Form["visibility"]) ? null : JSerializer.Deserialize<MapBuilderTemplateModel.ServiceLayerVisibility[]>(this.Request.Form["visibility"])
        };

        string html = RenderRazorTemplate($"template-{template}", model);

        return JsonObject(new
        {
            html = html
        });
    }
}