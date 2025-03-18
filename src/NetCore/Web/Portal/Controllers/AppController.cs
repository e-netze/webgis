using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Exceptions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.Platform;
using E.Standard.Security.App.Exceptions;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Models;
using E.Standard.WebGIS.Core.Services;
using E.Standard.WebMapping.Core.Geometry;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Mvc;
using Portal.Core.AppCode.Services;
using Portal.Core.AppCode.Services.Authentication;
using Portal.Core.AppCode.Services.WebgisApi;
using Portal.Core.Models.App;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Portal.Core.Controllers;

public class AppController : PortalBaseController
{
    private readonly ILogger<AppController> _logger;
    private readonly ConfigurationService _config;
    private readonly UrlHelperService _urlHelper;
    private readonly UploadFilesService _uploadFiles;
    private readonly WebgisApiService _api;
    private readonly HmacService _hmac;
    private readonly IEnumerable<ICustomPortalService> _customServices;
    private readonly SpatialReferenceService _spatialReferences;
    private readonly GlobalReplacementsService _globalReplacements;

    public AppController(ILogger<AppController> logger,
                         ConfigurationService config,
                         UrlHelperService urlHelper,
                         UploadFilesService uploadFiles,
                         WebgisApiService api,
                         HmacService hmac,
                         SpatialReferenceService spatialReference,
                         IOptions<ApplicationSecurityConfig> appSecurityConfig,
                         ICryptoService crypto,
                         GlobalReplacementsService globalReplacements,
                         IEnumerable<ICustomPortalService> customServices = null,
                         IEnumerable<ICustomPortalSecurityService> customSecurity = null)
        : base(logger, urlHelper, appSecurityConfig, customSecurity, crypto)
    {
        _logger = logger;
        _config = config;
        _urlHelper = urlHelper;
        _uploadFiles = uploadFiles;
        _api = api;
        _hmac = hmac;
        _spatialReferences = spatialReference;
        _customServices = customServices;
        _globalReplacements = globalReplacements;
    }

    async public Task<IActionResult> Index(string id, string template)
    {
        return await RenderTemplate(id, template);
    }

    async public Task<IActionResult> PortalApp(string id, string category, string app)
    {
        try
        {
            var portalUser = base.CurrentPortalUserOrThrowIfRequired(_config.AllowAnonymousSecurityMethod());

            var portal = await _api.GetApiPortalPageAsync(HttpContext, id);
            if (!base.IsAuthorizedPortalUser(portal, portalUser))
            {
                throw new NotAuthorizedException();
            }

            var apiApp = await _api.GetApiPortalAppAsync(HttpContext, id, category, app);
            if (apiApp is null)
            {
                throw new Exception($"Can't find requested app '{app}'");
            }

            await _customServices.LogMapRequest(id, category, app, portalUser?.Username);

            return await RenderTemplate(id, apiApp.Template, apiApp.TemplateParameters, portalUser.Username == apiApp.Creator, app, category);
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

    [HttpPost]
    async public Task<IActionResult> UploadAppImage(string id, IFormCollection form)
    {
        try
        {
            string app = form["app"];
            if (String.IsNullOrWhiteSpace(app))
            {
                throw new Exception("Unknown map");
            }

            string category = form["category"];
            if (String.IsNullOrWhiteSpace(category))
            {
                throw new Exception("Unknown category");
            }

            var file = _uploadFiles.GetFiles(Request)["app-image"];
            if (file == null)
            {
                throw new Exception("No file uploaded");
            }

            byte[] data = file.Data;

            string storageName = await _api.UploadAppImageAsync(HttpContext, id, category, app, data);

            return default(IActionResult);
        }
        catch (Exception ex)
        {
            return ExceptionView(ex);
        }
    }

    [HttpGet]
    async public Task<IActionResult> DeleteApp(string id, string app, string category)
    {
        try
        {
            var portalUser = base.CurrentPortalUser();

            var portal = await _api.GetApiPortalPageAsync(HttpContext, id);
            if (!base.IsAuthorizedPortalUser(portal, portalUser))
            {
                throw new NotAuthorizedException();
            }

            var apiApp = await _api.GetApiPortalAppAsync(HttpContext, id, category, app);

            if (apiApp == null || apiApp.Creator != portalUser.Username)
            {
                throw new NotAuthorizedException();
            }

            return View(new DeleteAppModel() { AppName = app, AppCategory = category });
        }
        catch (Exception ex)
        {
            return ExceptionView(ex);
        }
    }

    async public Task<IActionResult> DeleteApp(string id, DeleteAppModel model)
    {
        try
        {
            var portalUser = base.CurrentPortalUser();

            var portal = await _api.GetApiPortalPageAsync(HttpContext, id);
            if (!base.IsAuthorizedPortalUser(portal, portalUser))
            {
                throw new NotAuthorizedException();
            }

            var apiApp = await _api.GetApiPortalAppAsync(HttpContext, id, model.AppCategory, model.AppName);

            if (apiApp == null || apiApp.Creator != portalUser.Username)
            {
                throw new NotAuthorizedException();
            }

            await _api.DeleteAppAsync(HttpContext, id, model.AppCategory, model.AppName);

            return Redirect($"{_urlHelper.PortalUrl()}/{id}");
        }
        catch (Exception ex)
        {
            return ExceptionView(ex);
        }
    }

    #region Helpers

    async private Task<IActionResult> RenderTemplate(string id, string template, ApiAppDTO.Parameter[] parameters = null, bool isCreator = false, string appName = "", string appCategory = "")
    {
        FileInfo fi = new FileInfo($"{_urlHelper.AppRootPath()}/_templates/{template}.html");

        NameValueCollection replacers = new NameValueCollection();

        if (!String.IsNullOrWhiteSpace(Request.Query["parameters"]))
        {
            foreach (var parameter in Request.Query["parameters"].ToString().Split('|'))
            {
                int pos = parameter.IndexOf('=');
                if (pos > 0)
                {
                    replacers.Add(parameter.Substring(0, pos), parameter.Substring(pos + 1, parameter.Length - pos - 1));
                }
            }
        }
        else if (parameters != null)
        {
            foreach (var parameter in parameters)
            {
                replacers.Add(parameter.Name, parameter.Value);
            }
        }

        replacers.Add("portal-url", _urlHelper.PortalUrl());
        replacers.Add("portal-page-id", id);

        string html = String.Empty;

        using (StreamReader sr = new StreamReader(fi.FullName))
        {
            html = sr.ReadToEnd();

            var security = _config.DefaultSecurityMethod();
            if (!String.IsNullOrWhiteSpace(Request.Query["security"]))
            {
                security = Request.Query["security"].ToString().ToLower();
            }

            var allowedSecurity = _config.AllowedSecurityMethods();
            if (!allowedSecurity.Contains(security))
            {
                throw new ArgumentException("Authentification Method: '" + security + "' is not allowed!");
            }
            if (security == "clientid")
            {
                html = html.Replace("{api-hmac}", $"webgis.clientid='{_config.SecurityClientId()}';");
            }
            else
            {
                var hmac = (await _hmac.CreateHmacObjectAsync(CurrentPortalUser())).ToJs();
                html = html.Replace("{api-hmac}", "webgis.hmac=new webgis.hmacController(" + hmac + ");");
            }

            #region JavaScript

            StringBuilder js = new StringBuilder();

            #region Arguments

            var arguments = new NameValueCollection();

            foreach (var key in this.Request.Query.Keys)
            {
                if (key == null || String.IsNullOrEmpty(this.Request.Query[key]))
                {
                    continue;
                }

                switch (key.ToLower())
                {
                    //case "service":
                    //case "request":
                    case "version":
                        string[] wmsArg = this.Request.Query[key].ToString().ToLower().Split(',');
                        arguments[key.ToLower()] = wmsArg[wmsArg.Length - 1];
                        break;
                    default:
                        arguments[key.ToLower()] = this.Request.Query[key];
                        break;
                }

            }

            #endregion

            #region CRS/SRS

            SpatialReference sRef = null;
            if (arguments["srs"] != null || arguments["crs"] != null)
            {
                string crsId = !String.IsNullOrWhiteSpace(arguments["srs"]) ? arguments["srs"] : arguments["crs"];

                if (!String.IsNullOrEmpty(crsId))
                {
                    if (crsId.ToLower().StartsWith("epsg:"))
                    {
                        crsId = crsId.Split(':')[1];
                    }

                    int epsgCode = int.Parse(crsId);

                    sRef = _spatialReferences.GetSpatialReference(epsgCode);
                }
            }

            #endregion

            #region Box

            if (arguments["bbox"] != null)
            {
                Envelope extent = null;

                if (arguments["bbox"] == "default")
                {
                }
                else
                {
                    bool ignorAxes = arguments["version"] == "1.3.0" && sRef != null ? false : true;

                    string[] bbox = arguments["bbox"].Split(',');
                    if (!ignorAxes)
                    {
                        if ((sRef.AxisX == AxisDirection.North || sRef.AxisX == AxisDirection.South) &&
                            (sRef.AxisY == AxisDirection.West || sRef.AxisY == AxisDirection.East))
                        {
                            extent = new Envelope(bbox[1].ToPlatformDouble(),
                                                                           bbox[0].ToPlatformDouble(),
                                                                           bbox[3].ToPlatformDouble(),
                                                                           bbox[2].ToPlatformDouble());
                        }
                        else
                        {
                            extent = new Envelope(bbox[0].ToPlatformDouble(),
                                                                           bbox[1].ToPlatformDouble(),
                                                                           bbox[2].ToPlatformDouble(),
                                                                           bbox[3].ToPlatformDouble());
                        }
                    }
                    else
                    {

                        extent = new Envelope(bbox[0].ToPlatformDouble(),
                                                                       bbox[1].ToPlatformDouble(),
                                                                       bbox[2].ToPlatformDouble(),
                                                                       bbox[3].ToPlatformDouble());
                    }
                }

                if (sRef != null && sRef.Id != 4326)
                {
                    using (var transformer = new GeometricTransformerPro(_spatialReferences.Collection, sRef.Id, 4326))
                    {
                        transformer.Transform(extent);
                    }
                }

                js.Append("map.zoomTo([" +
                    extent.MinX.ToPlatformNumberString() + "," +
                    extent.MinY.ToPlatformNumberString() + "," +
                    extent.MaxX.ToPlatformNumberString() + "," +
                    extent.MaxY.ToPlatformNumberString() +
                                        "]);");
            }
            else if (arguments["scale"] != null && arguments["center"] != null)
            {
                js.Append($"map.setScale({arguments["scale"]},[{arguments["center"]} ]);");
            }

            #endregion

            #region Coord

            if (arguments["coord"] != null)
            {
                if (arguments["coord"].ToLower() == "current")
                {
                    js.Append("webgis.tryDelayed(function(){return map.executeTool('webgis.tools.navigation.currentPos');},500,10);");
                }
                else
                {
                    Point point = null;
                    bool ignorAxes = arguments["version"] == "1.3.0" && sRef != null ? false : true;

                    string[] coord = arguments["coord"].Split(',');
                    if (!ignorAxes)
                    {
                        if ((sRef.AxisX == AxisDirection.North || sRef.AxisX == AxisDirection.South) &&
                            (sRef.AxisY == AxisDirection.West || sRef.AxisY == AxisDirection.East))
                        {
                            point = new Point(coord[1].ToPlatformDouble(),
                                                                       coord[0].ToPlatformDouble());
                        }
                        else
                        {
                            point = new Point(coord[0].ToPlatformDouble(),
                                                                       coord[1].ToPlatformDouble());
                        }
                    }
                    else
                    {

                        point = new Point(coord[0].ToPlatformDouble(),
                                                                   coord[1].ToPlatformDouble());
                    }

                    if (sRef != null && sRef.Id != 4326)
                    {
                        using (var transformer = new GeometricTransformerPro(_spatialReferences.Collection, sRef.Id, 4326))
                        {
                            transformer.Transform(point);
                        }
                    }

                    js.Append("map.zoomTo([" +
                        point.X.ToPlatformNumberString() + "," +
                        point.Y.ToPlatformNumberString() + "," +
                        point.X.ToPlatformNumberString() + "," +
                        point.Y.ToPlatformNumberString() +
                                            "]);");
                }
            }

            #endregion

            #endregion

            string apiUrl = _urlHelper.ApiUrl(Request, HttpSchema.Current);
            string portalUrl = _urlHelper.AppRootUrl(Request, this, false).RemoveEndingSlashes();

            html = html.Replace("{api}", apiUrl);
            html = html.Replace("{portal}", portalUrl);
            html = html.Replace("{company}", _config.Company());
            html = html.Replace("{map-script}", js.ToString());
            html = html.Replace("{app-object}", $"var app = {{ is_creator: {isCreator.ToString().ToLower()} }};");

            foreach (var key in replacers.AllKeys)
            {
                html = html.Replace("{{" + key + "}}", _globalReplacements.Apply(replacers[key]));
            }

            if (isCreator)
            {
                StringBuilder menu = new StringBuilder();
                menu.Append("<div id='app-creator-menu' style='z-index:9999999;position:absolute;left:0px;bottom:0px;width:30px;height:26px;background:#eee;border:1px solid #aaa; border-radius:4px 14px 14px 4px;overflow:hidden'>");

                menu.Append("<table style='padding:0px;margin:0px;position:relative;top:-2px;left:-2px'><tr>");

                menu.Append("<td style='white-space:nowrap'>");
                menu.Append("<div style='width:26px;height:26px;background:url(\"../../Content/img/user-24.png\") no-repeat center center;cursor:pointer' ");
                menu.Append(" onclick =\"document.getElementById('app-creator-menu').style.width=(document.getElementById('app-creator-menu').style.width=='30px' ? '360px' : '30px')\" ></div>");
                menu.Append("</td>");

                #region AppBuilder ...

                string appBuilderUrl = ActionUrl("Index", "AppBuilder", new { id = id, app = appName, category = appCategory });
                menu.Append("<td style='white-space:nowrap'>");
                menu.Append("<div style='padding:4px;'><a href='" + appBuilderUrl + "' target='_blank'>App Builder...</a></div>");
                menu.Append("</td>");

                #endregion

                #region App Image

                menu.Append("<td style='white-space:nowrap'>");
                menu.Append("<iframe id='app-frame-upload-image' name='app-frame-upload-image' style='display:none'></iframe>");
                menu.Append("<form id='app-upload-image-form' action='" + ActionUrl("UploadAppImage", new { id = id }) + "' target='app-frame-upload-image' method='post' enctype='multipart/form-data' style='width: 0px; height: 0; overflow: hidden'>");
                menu.Append("<input id='app-upload-image-input' name='app-image' type='file' onchange=\"this.parentNode.submit(); this.value = ''; \" />");
                menu.Append("<input name='app' type='hidden' value='" + appName + "' />");
                menu.Append("<input name='category' type='hidden' value='" + appCategory + "' />");
                menu.Append("</form>");

                menu.Append("<div style='padding:4px;'><a href='' onclick=\"document.getElementById('app-upload-image-input').click();return false;\">App Image</a></div>");
                menu.Append("</td>");

                #endregion

                #region App Löschen

                menu.Append("<td style='white-space:nowrap'>");
                menu.Append("<div style='padding:4px;'><a href='" + ActionUrl("DeleteApp", new { id = id, app = appName, category = appCategory }) + "' target='self'>App L&ouml;schen...</a></div>");
                menu.Append("</td>");

                #endregion

                menu.Append("</tr></table>");

                menu.Append("</div>");
                html = html.Replace("</body>", menu.ToString() + "</body>");
            }

            sr.Close();
        }

        return HtlmView(html);
    }

    #endregion
}