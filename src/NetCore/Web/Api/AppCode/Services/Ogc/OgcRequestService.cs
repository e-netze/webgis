using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Mvc;
using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Ogc;
using E.Standard.CMS.Core;
using E.Standard.Configuration.Services;
using E.Standard.Json;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.ThreadSafe;
using E.Standard.WebGIS.Core.Services;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace Api.Core.AppCode.Services.Ogc;

public class OgcRequestService
{
    private readonly ConfigurationService _config;
    private readonly UrlHelperService _urlHelper;
    private readonly SpatialReferenceService _sRefs;
    private readonly ICryptoService _crypto;

    public OgcRequestService(ConfigurationService config,
                             UrlHelperService urlHelper,
                             SpatialReferenceService sRefs,
                             ICryptoService crypto)
    {
        _config = config;
        _urlHelper = urlHelper;
        _sRefs = sRefs;
        _crypto = crypto;
    }

    #region Online Resource

    public string OgcOnlineResouce(HttpRequest request, bool forceHttps = false)
    {
        string onlineResource = _config.OgcOnlineResource();
        if (String.IsNullOrWhiteSpace(onlineResource) || onlineResource == "~")
        {
            var uri = request.Uri();

            onlineResource = $"{(forceHttps == true ? "https" : uri.Scheme)}://{uri.Authority}{uri.AbsolutePath}";

            if (!onlineResource.EndsWith("/"))
            {
                onlineResource += "/";
            }
        }

        return onlineResource;
    }

    #endregion

    #region Login/Logout Service

    public string OgcLoginService(HttpRequest request)
    {
        string service = _config.OgcLoginService();
        string onlineResource = OgcOnlineResouce(request) + "?ogc_ticket={token}";

        return service.Replace("{redirect}", onlineResource);
    }

    public string OgcLogoutService(HttpRequest request, string ogcTicket)
    {
        string service = _config.OgcLogoutService();
        string onlineResource = OgcOnlineResouce(request) + "?ogc_ticket_logout=" + _crypto.EncryptTextDefault(ogcTicket, CryptoResultStringType.Hex);

        return service.Replace("{redirect}", onlineResource).Replace("{ogc_ticket}", ogcTicket);
    }

    #endregion

    #region Restrictions

    private static ThreadSafeDictionary<string, Polygon> _restrictionCache = new ThreadSafeDictionary<string, Polygon>();

    public Polygon GeometricRestrictions(HttpContext context, ServiceInfoDTO service, CmsDocument.UserIdentification ui)
    {
        if (ui == null || String.IsNullOrWhiteSpace(ui.Username))
        {
            return null;
        }

        var request = context.Request;

        if (service?.properties is ServiceInfoDTO.TileProperties)
        {
            int srs = service.supportedCrs != null && service.supportedCrs.Length > 0 ? service.supportedCrs[0] : 0;

            string srsParameter = String.IsNullOrWhiteSpace(request.Query["srs"]) ? request.Query["crs"] : request.Query["srs"];
            if (!String.IsNullOrWhiteSpace(srsParameter))
            {
                int srsFromParameter;
                if (int.TryParse(srsParameter.Contains(":") ? srsParameter.Split(':')[1] : srsParameter, out srsFromParameter))
                {
                    srs = srsFromParameter;
                }
            }

            if (_restrictionCache.ContainsKey($"{service.id}:{ui.Username}:{srs}"))
            {
                return _restrictionCache[service.id + ":" + ui.Username + ":" + srs];
            }

            string restrictionPath = $"{_urlHelper.AppEtcPath()}/ogc/wmts/{service.id}/geometric-restrictions";

            DirectoryInfo di = new DirectoryInfo(restrictionPath);
            if (!di.Exists)
            {
                return null;
            }

            FileInfo fi;
            FeaturesDTO features = null;

            if (ui.UserrolesParameters != null)
            {
                foreach (var roleParameter in ui.UserrolesParameters)
                {
                    if (features != null)
                    {
                        break;
                    }

                    int pos = roleParameter.IndexOf("=");
                    if (pos <= 0)
                    {
                        continue;
                    }

                    string key = roleParameter.Substring(0, pos);
                    string val = roleParameter.Substring(pos + 1);

                    fi = new FileInfo(restrictionPath + @"\role-parameter\" + key + @"\" + val + ".json");
                    if (fi.Exists)
                    {
                        features = JSerializer.Deserialize<FeaturesDTO>(System.IO.File.ReadAllText(fi.FullName));
                    }
                }
            }

            if (features == null && ui.Userroles != null)
            {
                foreach (var role in ui.Userroles)
                {
                    if (features != null)
                    {
                        break;
                    }

                    fi = new FileInfo(restrictionPath + @"\role\" + role.Replace(":", "_") + ".json");
                    if (fi.Exists)
                    {
                        features = JSerializer.Deserialize<FeaturesDTO>(System.IO.File.ReadAllText(fi.FullName));
                    }
                }
            }

            if (features == null && !String.IsNullOrWhiteSpace(ui.Username))
            {
                fi = new FileInfo(restrictionPath + @"\user\" + ui.Username.Replace(":", "_") + ".json");
                if (fi.Exists)
                {
                    features = JSerializer.Deserialize<FeaturesDTO>(System.IO.File.ReadAllText(fi.FullName));
                }
            }

            if (features != null && features.features != null && features.features.Length > 0)
            {
                var polygon = features.features[0].ToShape() as Polygon;

                if (srs != 0)
                {
                    using (GeometricTransformerPro transform = new GeometricTransformerPro(_sRefs.Collection, 4326, srs))
                    {
                        transform.Transform(polygon);
                    }
                }

                _restrictionCache.Add(service.id + ":" + ui.Username + ":" + srs, polygon);

                return polygon;
            }
        }

        return null;
    }

    public string TransformationResrictions(ServiceInfoDTO service, string srs)
    {
        srs = srs.Split(':')[0];

        FileInfo fi = new FileInfo($"{_urlHelper.AppEtcPath()}/ogc/wmts/{service.id}/transformations/{srs}.p4");
        if (!fi.Exists)
        {
            fi = new FileInfo($"{_urlHelper.AppEtcPath()}/ogc/wmts/{service.id.Split('@')[0]}/transformations/{srs}.p4");
        }

        if (!fi.Exists)
        {
            return null;
        }

        return System.IO.File.ReadAllText(fi.FullName);
    }

    public void ClearCache()
    {
        _restrictionCache.Clear();
    }

    #endregion

    #region Exception Handling

    public IActionResult HandleOgcException(ApiBaseController controller,
                                            Exception ex,
                                            string serviceId,
                                            NameValueCollection arguments,
                                            CmsDocument.UserIdentification ui)
    {
        string message = OgcExceptionMessage(ex, serviceId, arguments, ui);

        if (arguments["request"] == "getmap" && !String.IsNullOrWhiteSpace(arguments["width"]) && !String.IsNullOrWhiteSpace(arguments["height"]))
        {
            int iWidth = int.Parse(arguments["width"]);
            int iHeight = int.Parse(arguments["height"]);

            using (var bitmap = Current.Engine.CreateBitmap(iWidth, iHeight, PixelFormat.Rgba32))
            using (var camvas = bitmap.CreateCanvas())
            using (var font = Current.Engine.CreateFont("Verdana", 10f, FontStyle.Regular))
            using (var pen = Current.Engine.CreatePen(ArgbColor.Red, 2f))
            using (var brush = Current.Engine.CreateSolidBrush(ArgbColor.FromArgb(255, 255, 222, 222)))
            using (var blackBrush = Current.Engine.CreateSolidBrush(ArgbColor.Black))
            {
                bitmap.MakeTransparent();

                var sizeF = camvas.MeasureText(message, font);
                var rect = new CanvasRectangle(iWidth / 2 - (int)sizeF.Width / 2 - 10, iHeight / 2 - (int)sizeF.Height / 2 - 10, (int)sizeF.Width + 20, (int)sizeF.Height + 20);
                camvas.FillRectangle(brush, rect);
                camvas.DrawRectangle(pen, rect);
                camvas.DrawText(message, font, blackBrush, new CanvasPointF(rect.Left + 10f, rect.Top + 10f));

                MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);

                return controller.RawResponse(ms.ToArray(), "image/png", null);
            }
        }

        var wmsHelper = new WmsHelper(OgcOnlineResouce(controller.Request),
                                      _config.OgcDefaultSupportedCrs());

        if (arguments["request"] == "getfeatureinfo")
        {
            return controller.PlainView("<div style='font-family:verdana;font-size:8.25pt;background-color:#ffe0e0;border:2px solid red;padding:10px'>" + message.Replace("\n", "<br/>") + "</div>", "text/html");
        }
        return controller.PlainView(wmsHelper.WmsException(new Exception(message), arguments), "text/xml");
    }

    public string OgcExceptionMessage(Exception ex, string serviceId, NameValueCollection arguments, CmsDocument.UserIdentification ui)
    {
        string message = ex.Message;

        string exType = ex.GetType().ToString().Split('.')[ex.GetType().ToString().Split('.').Length - 1];

        foreach (string filePath in new string[]{
            @"ogc/exceptions/" + serviceId + @"/" + exType + ".txt",
            @"ogc/exceptions/" + serviceId + @"/default.txt",
            @"ogc/exceptions/default/" + exType + ".txt",
            @"ogc/exceptions/default/default.txt"
        })
        {
            FileInfo fi = new FileInfo($"{_urlHelper.AppEtcPath()}/{filePath}");
            if (fi.Exists)
            {
                message = System.IO.File.ReadAllText(fi.FullName);

                message = message.Replace("[username]", ui != null ? ui.Username : "anonymous")
                                 .Replace("[servicename]", serviceId)
                                 .Replace("[original-message]", FullExceptionMessage(ex))
                                 .Replace("[stack-trace]", FullExceptionStacktrace(ex));
                break;
            }
        }

        return message;
    }

    private string FullExceptionMessage(Exception ex)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(ex.Message);
        while (ex.InnerException != null)
        {
            ex = ex.InnerException;
            sb.Append(": ");
            sb.Append(ex.Message);
        }

        return sb.ToString();
    }

    private string FullExceptionStacktrace(Exception ex)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(ex.StackTrace);
        while (ex.InnerException != null)
        {
            ex = ex.InnerException;
            sb.Append("\nInner:\n ");
            sb.Append(ex.StackTrace);
        }

        return sb.ToString();
    }

    #endregion
}
