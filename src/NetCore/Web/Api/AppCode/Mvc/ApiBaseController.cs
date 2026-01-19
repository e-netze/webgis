#pragma warning disable CA1416

using Api.AppCode.Mvc.Wrapper;
using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Services;
using E.Standard.Api.App;
using E.Standard.Api.App.Exceptions;
using E.Standard.Api.App.Extensions;
using E.Standard.Configuration.Extensions;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.Extensions.ErrorHandling;
using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.Security.App.Json;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.Core.Models.Abstraction;
using E.Standard.WebGIS.Core.Mvc.Wrapper;
using E.Standard.WebMapping.Core;
using gView.GraphicsEngine;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Mvc;

public class ApiBaseController : Controller
{
    private readonly ILogger _logger;
    private readonly UrlHelperService _urlHelper;
    private readonly IHttpService _http;
    private readonly IEnumerable<ICustomApiService> _customServices;

    public ApiBaseController(ILogger logger,
                             UrlHelperService urlHelper,
                             IHttpService http,
                             IEnumerable<ICustomApiService> customServices)
    {
        _logger = logger;
        _urlHelper = urlHelper;
        _http = http;
        _customServices = customServices;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var descriptor = context.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
        CurrentControllerName = descriptor?.ControllerName ?? "Unknown";
        CurrentActionName = descriptor.ActionName ?? "Unknown";

        string appRootUrl = this.Url.Content("~");
        if (CurrentControllerName.Equals("datalinq", StringComparison.InvariantCultureIgnoreCase))
        {
            // 
            // Erste Hilfe für Kunden ContactTracing Anwendung
            //
            appRootUrl = _urlHelper.AppRootUrl();
        }

        this.ViewData["apiRootUrl"] = appRootUrl;

        base.OnActionExecuting(context);
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
    }

    internal string CurrentControllerName { get; private set; }
    internal string CurrentActionName { get; private set; }

    #region Request Wrapper

    IHttpRequestWrapper _httpRequest = null;
    public IHttpRequestWrapper HttpRequest
    {
        get
        {
            if (_httpRequest == null)
            {
                _httpRequest = new HttpRequestWrapper(this.Request);
            }

            return _httpRequest;
        }
    }

    public string UrlContent(string path) => this.Url.Content(path);

    #endregion

    protected IActionResult ViewResult(object model = null) => View(model);
    protected IActionResult ViewResult(string viewName, object model = null) => View(viewName, model);
    protected IActionResult RedirectResult(string target) => this.Redirect(target);
    protected IActionResult RedirectToActionResult(string action, string controller = "", object parameters = null)
    {
        if (string.IsNullOrWhiteSpace(controller))
        {
            return this.RedirectToAction(action, routeValues: parameters);
        }

        return this.RedirectToAction(action, controller, parameters);
    }

    protected string ActionUrl(string action, object parameters = null)
    {
        return this.Url.Action(action, values: parameters);
    }

    protected string ActionUrl(string controller, string action, object parameters = null)
    {
        return this.Url.Action(controller, action, values: parameters);
    }

    protected void AddViewData(string name, string value) => this.ViewData[name] = value;

    protected Version JavaScriptApiVersion()
    {
        NameValueCollection nvc = this.HasFormData
            ? Request.Form.ToCollection()
            : Request.Query.ToCollection();

        string apiVersion = nvc["_apiv"] ?? Request.Query["_apiv"];

        if (String.IsNullOrWhiteSpace(apiVersion))
        {
            return null;
        }

        return new System.Version(apiVersion);
    }

    internal bool ClientJavascriptVersionOrLater(System.Version version)
    {
        var apiVersion = JavaScriptApiVersion();
        if (apiVersion == null)
        {
            return true;
        }

        return apiVersion >= version;
    }

    async internal Task<IActionResult> ApiObject(object obj)
    {
        NameValueCollection nvc = this.HasFormData ? Request.Form.ToCollection() : Request.Query.ToCollection();
        if (nvc["f"] == "json" || Request.Query["f"] == "json" ||
            nvc["f"] == "pjson" || Request.Query["f"] == "pjson")
        {
            return await JsonObject(obj, nvc["f"] == "pjson" || Request.Query["f"] == "pjson");
        }
        if (nvc["f"] == "bin" || Request.Query["f"] == "bin")
        {
            if (obj is E.Standard.Api.App.DTOs.ImageLocationResponseDTO)
            {
                var ilr = (E.Standard.Api.App.DTOs.ImageLocationResponseDTO)obj;

                byte[] data = new byte[0];
                try
                {
                    if (!String.IsNullOrWhiteSpace(ilr.Path))
                    {
                        data = (await ilr.Path.BytesFromUri(_http)).ToArray();
                    }
                    else if (!String.IsNullOrWhiteSpace(ilr.url))
                    {
                        data = await _http.GetDataAsync(ilr.url);
                    }
                }
                catch (Exception ex)
                {
                    int.TryParse((nvc["width"] ?? Request.Query["width"]).ToString(), out int width);
                    int.TryParse((nvc["height"] ?? Request.Query["height"]).ToString(), out int height);

                    width = width == 0 ? 400 : width;
                    height = height == 0 ? 400 : height;

                    using (var bitmap = Current.Engine.CreateBitmap(width, height))
                    using (var canvas = bitmap.CreateCanvas())
                    using (var font = Current.Engine.CreateFont(SystemInfo.DefaultFontName, 8, FontStyle.Regular))
                    using (var redBrush = Current.Engine.CreateSolidBrush(ArgbColor.Red))
                    {
                        canvas.TextRenderingHint = TextRenderingHint.AntiAlias;
                        canvas.DrawText(ex.Message, font, redBrush, new CanvasPointF(0f, 0f));

                        using (MemoryStream ms = new MemoryStream())
                        {
                            bitmap.Save(ms, ImageFormat.Png);
                            data = ms.ToArray();
                        }
                    }
                }

                await _customServices.HandleApiResultObject(obj as IWatchable, data, this.User?.Identity?.Name);

                //return View("_binary", new E.Standard.Api.App.Models.Binary()
                //{
                //    ContentType = "image/png",
                //    Data = data
                //});
                return BinaryResultStream(data, "image/png");
            }
            if (obj is LayerLegendItem)
            {
                //return View("_binary", new E.Standard.Api.App.Models.Binary()
                //{
                //    ContentType = ((LayerLegendItem)obj).ContentType,
                //    Data = ((LayerLegendItem)obj).Data
                //});
                return BinaryResultStream(((LayerLegendItem)obj).Data, ((LayerLegendItem)obj).ContentType);
            }
        }

        return View("_html", obj as E.Standard.Api.App.Models.Abstractions.IHtml);
    }

    async internal Task<IActionResult> JsonObject(object obj, bool pretty = false)
    {
        var json = JSerializer.Serialize(obj, pretty || ApiGlobals.IsDevelopmentEnvironment);

        await _customServices.HandleApiResultObject(obj as IWatchable, json, this.User?.Identity?.Name);

        return JsonView(json);

        //using (MemoryStream ms = new MemoryStream())
        //using (StreamWriter sw = new StreamWriter(ms))
        //{
        //    var jw = new Newtonsoft.Json.JsonTextWriter(sw);
        //    jw.Formatting = pretty || ApiGlobals.IsDevelopmentEnvironment ?
        //        Newtonsoft.Json.Formatting.Indented :
        //        Newtonsoft.Json.Formatting.None;
        //    var serializer = new Newtonsoft.Json.JsonSerializer();
        //    serializer.Serialize(jw, obj);
        //    jw.Flush();
        //    ms.Position = 0;

        //    string json = System.Text.Encoding.UTF8.GetString(ms.GetBuffer());
        //    json = json.Trim('\0');

        //    await _customServices.HandleApiResultObject(obj as IWatchable, json, this.User?.Identity?.Name);

        //    return JsonView(json);
        //}
    }

    protected IActionResult JsonView(string json)
    {
        //ViewData["json"] = json;

        //return View("_json");
        return JsonResultStream(json);
    }

    internal IActionResult FramedJsonObject(object obj, string callbackChannel = "framed-json-response")
    {
        var json = JSerializer.Serialize(obj);

        ViewData["json"] = json;
        ViewData["callback-channel"] = String.IsNullOrWhiteSpace(callbackChannel) ? "framed-json-response" : callbackChannel;

        return View("_iframe_json");

        //using (MemoryStream ms = new MemoryStream())
        //using (StreamWriter sw = new StreamWriter(ms))
        //{
        //    var jw = new Newtonsoft.Json.JsonTextWriter(sw);
        //    jw.Formatting = Newtonsoft.Json.Formatting.None;  // Newtonsoft.Json.Formatting.Indented;
        //    var serializer = new Newtonsoft.Json.JsonSerializer();
        //    serializer.Serialize(jw, obj);
        //    jw.Flush();
        //    ms.Position = 0;

        //    string json = System.Text.Encoding.UTF8.GetString(ms.GetBuffer());
        //    json = json.Trim('\0');

        //    ViewData["json"] = json;
        //    ViewData["callback-channel"] = String.IsNullOrWhiteSpace(callbackChannel) ? "framed-json-response" : callbackChannel;

        //    return View("_iframe_json");
        //}
    }

    async internal Task<IActionResult> JsonViewSuccess(bool success, string exceptionMessage = "", string exceptionType = "", string requestId = null)
    {
        if (!success && !String.IsNullOrEmpty(exceptionMessage))
        {
            return await JsonObject(new JsonException()
            {
                success = success,
                exception = exceptionMessage,
                exception_type = exceptionType,
                requestid = requestId,
                taskId = Request.FormOrQuery("taskId"),
                toolId = Request.FormOrQuery("toolId")
            });
        }
        return JsonView("{\"success\":" + success.ToString().ToLower() + "}");
    }

    async internal Task<IActionResult> ThrowJsonException(Exception ex, int statusCode = 200)
    {
        //Response.StatusCode = statusCode;

        _logger.LogError(ex, "An json exception is thrown");

        string type = ex.GetType().ToString().ToLower();
        type = type.Substring(type.LastIndexOf(".") + 1);

        return await JsonViewSuccess(false,
                               $"{ex.SecureMessage()}{(ex is NullReferenceException ? $" {ex.StackTrace}" : String.Empty)}",
                               type,
                               ex is ReportWarningException ? ((ReportWarningException)ex).RequestId : null);
    }

    internal IActionResult RawResponse(byte[] responseBytes, string contentType, NameValueCollection headers)
    {
        if (headers != null)
        {
            foreach (string header in headers)
            {
                this.Response.Headers.Append(header, headers[header]);
            }
        }

        try
        {
            Response.AddApiCorsHeaders(Request);
        }
        catch { }

        //return View("_binary", new E.Standard.Api.App.Models.Binary()
        //{
        //    ContentType = contentType,
        //    Data = responseBytes
        //});
        return BinaryResultStream(responseBytes, contentType);
    }

    internal IActionResult PlainView(string plain, string contentType = "")
    {
        //ViewData["plain"] = plain;
        //ViewData["ContentType"] = contentType;

        //return View("_plain");
        return PlainResultStream(plain, contentType);
    }

    protected bool IsJsonRequest
    {
        get
        {
            return Request.Query["f"] == "json";
        }
    }

    public string Title { get { return ViewBag.Title; } set { ViewBag.Title = value; } }

    #region Result Classes

    internal class JsonResponse
    {
        public bool success { get; set; }
    }

    private class JsonJavascriptResponse : JsonResponse
    {
        public object javascript { get; set; }
    }

    private class JsonHtmlResponse : JsonJavascriptResponse
    {
        public string html { get; set; }
    }

    private class JsonHtmlResponse2 : JsonHtmlResponse
    {
        public string title { get; set; }
    }

    internal class JsonException : JsonResponse
    {
        public string exception { get; set; }
        public string exception_type { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string requestid { get; set; }

        public string taskId { get; set; }
        public string toolId { get; set; }  
    }



    #endregion

    #region HMAC

    //public CmsDocument.UserIdentification GetHMACUser(bool allowEncryptedUiParameter = false, bool allowClientIdAndSecret = false)
    //{
    //    NameValueCollection nvc = null;

    //    if (this.HasFormData)
    //    {
    //        nvc = new NameValueCollection(Request.Form.ToCollection());
    //        foreach (var key in Request.Query.Keys)
    //        {
    //            if (nvc.AllKeys.Contains(key))
    //            {
    //                continue;
    //            }

    //            nvc.Add(key, Request.Query[key]);
    //        }
    //    }
    //    else
    //    {
    //        nvc = new NameValueCollection(Request.Query.ToCollection());
    //    }


    //    CmsDocument.UserIdentification ui = null;

    //    try
    //    {
    //        if (allowEncryptedUiParameter && !String.IsNullOrWhiteSpace(nvc["__ui"]))  // for webgis5 proxy requests
    //        {
    //            #region 

    //            string userName = nvc["__ui"];
    //            userName = new E.Standard.Security.Crypto().DecryptText(userName,
    //                "s29mj9ZRcxUrxfJZNt2KrCFLzab7NPXEz5pBsvx4dGK2VRRPxRS9jPHGm8S2SzzaUcHXPE7hck5UXcqt9Gnserbf8WMjRyELfrttxpH8bDeZWtmYN7DrpHTqFUMXbrYbfVzkFvGDmepCprjv6RF6sSwmaaBLQQGsXM8ZpPMP3L7hMjSyeCKkmsjd2eHSE2xACzgngSjusESyHw7nmm4DVw5wcVFxCtMSekrY4UDf93E6JpQAKmDWCDevqjQ6WZYS",
    //                E.Standard.Security.Crypto.Strength.AES256);

    //            string[] keyInfos = userName.Split('|');
    //            string privateKey = keyInfos[0];
    //            string username = keyInfos.Length > 0 ? keyInfos[0] : String.Empty;
    //            string[] userroles = null;
    //            string task = nvc["__ft"];

    //            if (keyInfos.Length > 1)  // Groupnames
    //            {
    //                userroles = new string[keyInfos.Length - 1];
    //                Array.Copy(keyInfos, 1, userroles, 0, keyInfos.Length - 1);
    //            }

    //            ui = String.IsNullOrWhiteSpace(username) ?
    //                CmsDocument.UserIdentification.Anonymous :
    //                new CmsDocument.UserIdentification(username, userroles, null, null, task: task);

    //            #endregion
    //        }

    //        if (nvc["hmac"] == "true")
    //        {
    //            string publicKey = nvc["hmac_pubk"];
    //            string hmacData = nvc["hmac_data"];
    //            long ticks = long.Parse(nvc["hmac_ts"]);
    //            string task = nvc["hmac_ft"];

    //            DateTime requestTime = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds(ticks);
    //            TimeSpan ts = DateTime.UtcNow - requestTime;

    //            if (ts.TotalSeconds > 60)  // use 60, everything else is for testing...
    //            {
    //                throw new Exception("Forbidden: Request expired");
    //            }

    //            string keyInfoString = Cache.GetCacheValue("hmac:" + publicKey);
    //            if (String.IsNullOrEmpty(keyInfoString))
    //            {
    //                throw new Exception("Forbidden: Public-key not valid (" + publicKey + " - Cache:" + Cache.CacheType() + ")");
    //            }

    //            string[] keyInfos = keyInfoString.Split('|');
    //            string privateKey = keyInfos[0];
    //            string username = keyInfos.Length > 1 ? keyInfos[1] : String.Empty;
    //            string[] userroles = null;
    //            if (keyInfos.Length > 2)  // Groupnames
    //            {
    //                userroles = new string[keyInfos.Length - 2];
    //                Array.Copy(keyInfos, 2, userroles, 0, keyInfos.Length - 2);
    //            }

    //            ui = String.IsNullOrWhiteSpace(username) ?
    //                CmsDocument.UserIdentification.Anonymous :
    //                new CmsDocument.UserIdentification(username, userroles, null, ApiGlobals.InstanceRoles, publicKey, task: task);

    //            string hmacHash = nvc["hmac_hash"];
    //            using (var hmacsha1 = new HMACSHA1(Encoding.UTF8.GetBytes(privateKey)))
    //            {
    //                byte[] bytes = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(ticks.ToString() + hmacData));
    //                string hash = Convert.ToBase64String(bytes);

    //                if (!hash.Equals(hmacHash))
    //                {
    //                    throw new Exception("Forbidden: Invalid hash!");
    //                }

    //                return ui;
    //            }
    //        }
    //        else if (allowClientIdAndSecret == true && !String.IsNullOrWhiteSpace(nvc["client_id"]) && !String.IsNullOrWhiteSpace(nvc["client_sec"]))
    //        {
    //            var subscriberDb = ApiGlobals.SubscriberDatabase();

    //            var client = subscriberDb.GetClientByClientId(nvc["client_id"]);
    //            if (client == null)
    //            {
    //                throw new Exception("Unknown cliend_id");
    //            }

    //            if (client.IsClientSecret(nvc["client_sec"]) == false)
    //            {
    //                throw new Exception("Wrong client secret");
    //            }

    //            var subscriber = subscriberDb.GetSubscriberById(client.Subscriber);
    //            if (subscriber == null)
    //            {
    //                throw new Exception("Unknown Subscriber");
    //            }

    //            ui = new CmsDocument.UserIdentification(subscriber.FullName + "@" + client.ClientName, null, null, null);
    //        }
    //        //else if (!String.IsNullOrWhiteSpace(ApiGlobals.CloudWebPortalUrl) && !String.IsNullOrWhiteSpace(nvc["client_id"]) && !String.IsNullOrWhiteSpace(nvc["token"]))
    //        //{
    //        //    var subscriberDb = ApiGlobals.SubscriberDatabase();

    //        //    var client = subscriberDb.GetClientByClientId(nvc["client_id"]);
    //        //    if (client == null)
    //        //    {
    //        //        throw new Exception("Unknown cliend_id");
    //        //    }

    //        //    CloudHelper.VerifyToken(ApiGlobals.CloudWebPortalUrl, nvc["token"], client.ClientSecret);

    //        //    ui = new CmsDocument.UserIdentification("client::" + client.ClientId, client.Roles?.ToArray(), null, null);
    //        //}
    //        else
    //        {
    //            string username = GetCookieUsername();
    //            if (username.StartsWith("clientid:"))
    //            {
    //                // clientid:[clientid]:[username]
    //                ui = new CmsDocument.UserIdentification(username.Split(':')[2], null, null, null);

    //                this.ViewData["append-rest-username"] = "User: " + ui.Username;
    //            }
    //            else if (username.StartsWith("subscriber:"))
    //            {
    //                ui = new CmsDocument.UserIdentification("subscriber::" + username.Split(':')[2], null, null, null);

    //                this.ViewData["append-rest-username"] = "User: " + ui.Username;
    //            }
    //        }

    //        if (ui == null)
    //        {
    //            ui = CmsDocument.UserIdentification.Anonymous;
    //        }

    //        //if (String.IsNullOrWhiteSpace(ui.Username))
    //        //    throw new System.Security.Authentication.AuthenticationException();

    //        return ui;
    //    }
    //    finally
    //    {
    //        if (ui != null && !String.IsNullOrWhiteSpace(ApiGlobals.DynamicCmsPath))
    //        {
    //            #region Apply Public Client Roles

    //            // ToDo: Caching
    //            var publicClient =
    //                LazyCache.Get<SubscriberDb.Client>($"client{E.Standard.WebGIS.Cloud.CloudHelper.PublicClientId}",
    //                () =>
    //                {
    //                    return ApiGlobals.SubscriberDatabase().GetClientByClientId(E.Standard.WebGIS.Cloud.CloudHelper.PublicClientId);
    //                });


    //            if (publicClient?.Roles != null && publicClient.Roles.Count() > 0)
    //            {
    //                var uiRoles = ui.Userroles != null ? new List<string>(ui.Userroles) : new List<string>();
    //                foreach (var publicRole in publicClient.Roles)
    //                {
    //                    uiRoles.Add(publicRole);
    //                }
    //                CmsDocument.UserIdentification.ResetUserroles(ui, uiRoles.Distinct().ToArray());
    //            }

    //            #endregion
    //        }
    //    }
    //}

    #endregion

    #region AuthCookies

    //public void SetAuthCookie(string userName, bool persistentCookie)
    //{
    //    Response.Cookies.Append(AuthCookieName, E.Standard.Security.Crypto.EncryptCookieValue(userName), new Microsoft.AspNetCore.Http.CookieOptions()
    //    {
    //        HttpOnly = true
    //        //Expires=DateTimeOffset.
    //    });
    //    //FormsAuthentication.SetAuthCookie(userName, persistentCookie);
    //    Response.Headers.Add("P3P", "CP='IDC DSP COR ADM DEVi TAIi PSA PSD IVAi IVDi CONi HIS OUR IND CNT'");
    //}

    //public void SignOut()
    //{
    //    Response.Cookies.Delete(AuthCookieName);
    //    //System.Web.Security.FormsAuthentication.SignOut();
    //}

    protected IActionResult SignOutSchemes(params string[] authenticationSchemes)
    {
        return base.SignOut(authenticationSchemes);
    }

    protected ClaimsPrincipal ClaimsPrincipalUser => this.User;

    //public string GetCookieUsername()
    //{
    //    return E.Standard.Security.Crypto.DecryptCookieValue(Request.Cookies[AuthCookieName]) ?? String.Empty;
    //    //if (this.User == null || this.User.Identity == null)
    //    //    return String.Empty;

    //    //if (!this.User.Identity.IsAuthenticated)
    //    //    return String.Empty;

    //    //return this.User.Identity.Name;
    //}

    //private const string AuthCookieName = "webgis5core-api-auth";

    private class CookieData
    {
        public CookieData()
        {
            this.Created = DateTime.UtcNow.Ticks;
            this.Expires = DateTime.UtcNow.AddDays(1).Ticks;
        }

        [JsonProperty(PropertyName = "value")]
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonProperty(PropertyName = "authtype")]
        [System.Text.Json.Serialization.JsonPropertyName("authtype")]
        public int AuthType { get; set; }

        [JsonProperty(PropertyName = "created")]
        [System.Text.Json.Serialization.JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonProperty(PropertyName = "expires")]
        [System.Text.Json.Serialization.JsonPropertyName("expires")]
        public long Expires { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsExpired
        {
            get { return DateTime.UtcNow > new DateTime(Expires, DateTimeKind.Utc); }
        }
    }

    #endregion

    #region ETAG

    public IActionResult NotModified()
    {
        Response.StatusCode = 304;
        return Content(String.Empty);
    }

    protected bool HasIfNonMatch
    {
        get
        {
            return (string)this.Request.Headers["If-None-Match"] != null;
        }
    }

    public bool IfMatch()
    {
        try
        {
            if (HasIfNonMatch == false)
            {
                return false;
            }

            var etag = long.Parse(this.Request.Headers["If-None-Match"]);

            DateTime etagTime = new DateTime(etag, DateTimeKind.Utc);
            if (DateTime.UtcNow > etagTime)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void AppendEtag(DateTime expires)
    {
        this.Response.Headers.Append("ETag", expires.Ticks.ToString());
        this.Response.Headers.Append("Last-Modified", DateTime.UtcNow.ToString("R"));
        this.Response.Headers.Append("Expires", expires.ToString("R"));
        this.Response.Headers.Append("Cache-Control", "private, max-age=" + (int)(new TimeSpan(24, 0, 0)).TotalSeconds);
    }

    #endregion

    async protected Task<IActionResult> HandleAuthenticationException()
    {
        if (Request.Method.ToString() == "POST")
        {
            return await this.ThrowJsonException(new Exception("Not authenticated"), 200);
        }

        var securityConfig = new ApplicationSecurityConfig().LoadFromJsonFile();

        if (securityConfig?.IdentityType == "oidc")
        {
            return RedirectToAction("Forbidden", "Authenticate");
        }

        return RedirectToAction("Login");
    }

    #region Helper

    private bool HasFormData
    {
        get
        {
            return (Request.Method.ToString().ToLower() == "post" && Request.HasFormContentType);
        }
    }

    #endregion

    #region Return Streams 

    protected IActionResult BinaryResultStream(byte[] data, string contentType, string fileName = "")
    {
        if (!String.IsNullOrWhiteSpace(fileName))
        {
            Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
        }

        return File(data, contentType);
    }

    protected IActionResult JsonResultStream(string json)
    {
        json = json ?? String.Empty;

        Response
            .AddNoCacheHeaders()
            .AddApiCorsHeaders(Request);

        return BinaryResultStream(Encoding.UTF8.GetBytes(json), "application/json; charset=utf-8");
    }

    protected IActionResult PlainResultStream(string text, string contantType)
    {
        text = text ?? String.Empty;

        return BinaryResultStream(Encoding.UTF8.GetBytes(text), contantType);
    }

    #endregion
}
