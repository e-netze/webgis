using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.Json;
using E.Standard.Security.App.Exceptions;
using E.Standard.Security.App.Extensions;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Models;
using E.Standard.WebGIS.Core.Mvc.Wrapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portal.AppCode.Mvc.Wrapper;
using Portal.Core.AppCode.Exceptions;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Services;
using Portal.Core.AppCode.Services.Authentication;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Claims;
using System.Web;

namespace Portal.Core.AppCode.Mvc;

public class PortalBaseController : Controller/*, IPortalBaseController<IActionResult>*/
{
    private readonly ILogger _logger;
    private readonly UrlHelperService _urlHelper;
    private readonly ApplicationSecurityConfig _appSecurityConfig;
    private readonly IEnumerable<ICustomPortalSecurityService> _securityServices;
    private readonly ICryptoService _crypto;

    protected PortalBaseController(ILogger logger,
                                   UrlHelperService urlHelper,
                                   IOptions<ApplicationSecurityConfig> appSecurityConfig,
                                   IEnumerable<ICustomPortalSecurityService> securityServices,
                                   ICryptoService crypto)
    {
        _logger = logger;
        _urlHelper = urlHelper;
        _appSecurityConfig = appSecurityConfig.Value;
        _securityServices = securityServices;
        _crypto = crypto;
    }

    #region

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);

        this.ViewData["portalRootUrl"] = _urlHelper.AppRootUrl(this.Request, this);
        this.ViewData["apiRootUrl"] = _urlHelper.ApiUrl(this.Request);

        var portalContentUrl = _urlHelper.AppRootUrlFromConfig(this.Request, false);
        if (String.IsNullOrWhiteSpace(portalContentUrl))
        {
            portalContentUrl = this.Url.Content("~");
        }
        if (portalContentUrl.EndsWith("/"))
        {
            portalContentUrl = portalContentUrl.Substring(0, portalContentUrl.Length - 1);
        }

        this.ViewData["portalContentUrl"] = portalContentUrl;
    }

    #endregion

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

    #region Auth

    public PortalUser CurrentPortalUser()
    {
        if (this.User.Identity.IsAuthenticated)
        {
            return this.User.ToPortalUser(_appSecurityConfig);
        }
        else
        {
            return new PortalUser(string.Empty, new string[0], null);
        }
    }

    public PortalUser CurrentPortalUserOrThrowIfRequired(bool allowAnonymous)
    {
        var portalUser = CurrentPortalUser();

        if (allowAnonymous == false && portalUser.IsAnonymous && _appSecurityConfig.UseAnyOidcMethod())
        {
            throw new NotAuthorizedException();  // force a redirect to LoginOidc
        }

        return portalUser;
    }

    public bool IsAuthorizedPortalUser(ApiPortalPageDTO portal, PortalUser portalUser, bool redirectIfNecessary = true)
    {
        // Page Subscriber
        if (portal.Subscriber.Equals(portalUser.Username, StringComparison.CurrentCultureIgnoreCase))
        {
            return true;
        }

        // Page is public?
        if (_securityServices.ContainsPublicUserOrClientId(portal.Users))
        {
            return true;
        }

        if (!UserManagement.IsAllowed(portalUser == null ? "" : portalUser.Username, portal.Users) &&
            !UserManagement.IsAllowed(portalUser == null ? null : portalUser.UserRoles, portal.Users))
        {
            if (redirectIfNecessary == true)
            {
                if (_securityServices.AllowAnyUserLogin(portal.Users))
                {
                    if (this.User.Identity.IsAuthenticated)
                    {
                        throw new RedirectException($"{_urlHelper.AppRootUrl(this.Request, this).RemoveEndingSlashes()}/Auth/ChangeAccountOidc?id={portal.Id}&name={HttpUtility.UrlEncode(portal.Name)}");
                    }

                    // OpenIdConnect
                    if (_appSecurityConfig.IdentityType == "oidc")
                    {
                        if (!String.IsNullOrEmpty(Request.Query["access-token"]))
                        {
                            throw new RedirectException($"{_urlHelper.AppRootUrl(this.Request, this).RemoveEndingSlashes()}/Auth/InvalidAccessToken?id={portal.Id}");
                        }
                        throw new RedirectException($"{_urlHelper.AppRootUrl(this.Request, this).RemoveEndingSlashes()}/Auth/LoginOidc?id={portal.Id}&name={HttpUtility.UrlEncode(portal.Name)}");
                    }
                }
            }
            return false;
        }

        return true;
    }

    public bool IsAuthorizedPortalMapAuthor(ApiPortalPageDTO portal, PortalUser portalUser)
    {
        if (!UserManagement.IsAllowed(portalUser.Username, portal.MapAuthors) &&
            !UserManagement.IsAllowed(portalUser.UserRoles, portal.MapAuthors))
        {
            return false;
        }

        return true;
    }

    public bool IsAuthorizedPortalContentAuthor(ApiPortalPageDTO portal, PortalUser portalUser)
    {
        if (!UserManagement.IsAllowed(portalUser.Username, portal.ContentAuthors) &&
            !UserManagement.IsAllowed(portalUser.UserRoles, portal.ContentAuthors))
        {
            return false;
        }

        return true;
    }

    public bool IsPortalOwner(ApiPortalPageDTO portal, PortalUser portalUser)
    {
        return portal.Subscriber == portalUser.Username;
    }

    public new void SignOut()
    {
        Response.Cookies.Delete(WebgisCookieService.AuthCookieName);
    }

    public string CurrentUrl()
    {
        return _urlHelper.PortalUrl() + this.Request.Path +
            (String.IsNullOrWhiteSpace(Request.QueryString.ToString()) ?
            String.Empty :
            "?" + this.Request.QueryString);
    }

    public ClaimsPrincipal ClaimsPrincipalUser => this.User;

    public IActionResult SignOutSchemes(params string[] authenticationSchemes)
    {
        return this.SignOut(authenticationSchemes);
    }

    public IActionResult SignOutSchemesAndRedirect(string redirectTo, params string[] authenticationSchemes)
    {
        return this.SignOut(
            new AuthenticationProperties
            {
                RedirectUri = redirectTo //Url.Action()) 
            },
            authenticationSchemes);
    }

    protected IActionResult HandleNotAuthorizedException(string id)
    {
        if (_appSecurityConfig.UseAnyOidcMethod())
        {
            var portalUser = CurrentPortalUser();

            if (String.IsNullOrEmpty(portalUser?.Username))
            {
                return RedirectToActionResult("LoginOidc", "Auth",
                    new Dictionary<string, object> {
                        {  "webgis-redirect" , _crypto.EncryptTextDefault(this.Request.GetDisplayUrl(), CryptoResultStringType.Hex) }
                    });
            }
        }

        return RedirectToActionResult("Index", "Home", new { id = id });
    }

    #region Class

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

        [JsonProperty(PropertyName = "dn", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("dn")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string Displayname { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsExpired
        {
            get { return DateTime.UtcNow > new DateTime(Expires, DateTimeKind.Utc); }
        }
    }

    #endregion

    #endregion

    #region Render View

    public string RenderRazorTemplate(string viewName, object model)
    {
        return RenderTemplate(viewName, model);
    }

    #region Static 

    // Achtung: auf diese "statischen" Methoden/Properties wird aus den _mapbuilder/*.razortemplates zugegriffen. Nicht ändern!!
    // Muss statis sein, weil aus den Templates im _mapbuilder aufgerufen wird un sonst ein Razor Fehler kommt

    static public bool _razorIsIntialized = false;

    static public string RenderTemplate(string viewName, object model)
    {
        try
        {
            string appRootPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            var fileInfo = new FileInfo($"{appRootPath}/_mapbuilder/{viewName}.razortemplate");
            if (!fileInfo.Exists)
            {
                return String.Empty;
            }

            string razorContent = System.IO.File.ReadAllText(fileInfo.FullName);

            if (!_razorIsIntialized)
            {
                _razorIsIntialized = true;

                var config = new TemplateServiceConfiguration();
                // .. configure your instance
                config.Debug = false;
                config.DisableTempFileLocking = true;
                config.CachingProvider = new DefaultCachingProvider(t =>
                {
                    try
                    {
                        var di = new DirectoryInfo(t);
                        di.Delete(true);

                        Console.Write($"Razor: Deleted temp directory {t}");
                    }
                    catch (Exception ex)
                    {
                        Console.Write($"Razor: Error deleting temp directory ({t}): {ex.Message}");
                    }
                });

                var service = RazorEngineService.Create(config);
                Engine.Razor = service;
            }

            string key = Guid.NewGuid().ToString();
            if (!Engine.Razor.IsTemplateCached(key, model.GetType()))
            {
                Engine.Razor.Compile(razorContent, key, model.GetType());
            }

            return Engine.Razor.Run(key, model.GetType(), model);
        }
        catch (Exception ex)
        {
            return "Exception: " + ex.Message;
        }
    }

    public static NumberFormatInfo Nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

    #endregion

    #endregion

    private void LogException(Exception ex)
    {
        int loops = 0;
        while (ex != null)
        {
            _logger.LogError(ex, "Critical Error");

            ex = ex.InnerException;
            if (loops++ > 5)
            {
                break;
            }
        }
    }

    public IActionResult ExceptionView(Exception ex)
    {
        LogException(ex);
        return View("_exception", ex);
    }

    public IActionResult JsonObject(object obj)
    {
        var json = JSerializer.Serialize(obj);

        return JsonView(json);

        //using (MemoryStream ms = new MemoryStream())
        //using (StreamWriter sw = new StreamWriter(ms))
        //{
        //    var jw = new Newtonsoft.Json.JsonTextWriter(sw);
        //    jw.Formatting = Newtonsoft.Json.Formatting.Indented;
        //    var serializer = new Newtonsoft.Json.JsonSerializer();
        //    serializer.Serialize(jw, obj);
        //    jw.Flush();
        //    ms.Position = 0;

        //    string json = System.Text.Encoding.UTF8.GetString(ms.GetBuffer());
        //    json = json.Trim('\0');
        //    return JsonView(json);
        //}
    }

    public IActionResult JsonObjectEncrypted(object obj)
    {
        return JsonView(_crypto.EncryptTextDefault(JsonString(obj), CryptoResultStringType.Hex));
    }

    protected string JsonString(object obj)
    {
        var json = JSerializer.Serialize(obj);

        return json;

        //using (MemoryStream ms = new MemoryStream())
        //using (StreamWriter sw = new StreamWriter(ms))
        //{
        //    var jw = new Newtonsoft.Json.JsonTextWriter(sw);
        //    jw.Formatting = Newtonsoft.Json.Formatting.Indented;
        //    var serializer = new Newtonsoft.Json.JsonSerializer();
        //    serializer.Serialize(jw, obj);
        //    jw.Flush();
        //    ms.Position = 0;

        //    string json = System.Text.Encoding.UTF8.GetString(ms.GetBuffer());
        //    json = json.Trim('\0');
        //    return json;
        //}
    }

    public IActionResult JsonView(string json)
    {
        //ViewData["json"] = json;

        //return View("_json");
        return JsonResultStream(json);
    }

    public IActionResult JsonViewSuccess(bool success, string exceptionMessage = "")
    {
        if (!success && !String.IsNullOrEmpty(exceptionMessage))
        {
            return JsonObject(new JsonException()
            {
                success = success,
                exception = exceptionMessage
            });
        }
        return JsonView("{\"success\":" + success.ToString().ToLower() + "}");
    }

    internal IActionResult ThrowJsonException(Exception ex)
    {
        string message = $"{ex.Message}";

        //message += " " + ex.StackTrace;

        return JsonViewSuccess(false, message);
        //throw new NotImplementedException();
    }

    public IActionResult NotlicendedExceptionView()
    {
        return RedirectToAction("NotLicensed", "Home");
    }

    public IActionResult PlainView(string plain, string contentType = "")
    {
        //ViewData["plain"] = plain;
        //ViewData["ContentType"] = contentType;

        //return View("_plain");
        return PlainResultStream(plain, contentType);
    }

    public IActionResult HtlmView(string html)
    {
        //ViewData["html"] = html;

        //return View("_html");
        return PlainResultStream(html, "text/html");
    }

    public IActionResult RawResponse(byte[] responseBytes, string contentType)
    {
        try
        {
            Response.AddPortalCorsHeaders(Request);
        }
        catch { }

        //return View("_binary", new E.Standard.Portal.App.Models.Binary()
        //{
        //    ContentType = contentType,
        //    Data = responseBytes
        //});

        return BinaryResultStream(responseBytes, contentType);
    }


    public IActionResult RedirectResult(string target) => this.Redirect(target);

    public IActionResult RedirectToActionResult(string action, string controller = "", object parameters = null)
    {
        if (string.IsNullOrWhiteSpace(controller))
        {
            return this.RedirectToAction(action, routeValues: parameters);
        }

        return this.RedirectToAction(action, controller, parameters);
    }

    public IActionResult ViewResult(object model = null) => View(model);
    public IActionResult ViewResult(string viewName, object model = null) => View(viewName, model);

    public string ActionUrl(string action, object parameters = null)
    {
        return this.Url.Action(action, values: parameters);
    }
    public string ActionUrl(string controller, string action, object parameters = null)
    {
        return this.Url.Action(controller, action, values: parameters);
    }

    public string Title { get { return ViewBag.Title; } set { ViewBag.Title = value; } }

    #region Result Classes

    private class JsonResponse
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

    private class JsonException : JsonResponse
    {
        public string exception { get; set; }
    }



    #endregion

    #region Return Streams 

    public ActionResult BinaryResultStream(byte[] data, string contentType, string fileName = "")
    {
        if (!String.IsNullOrWhiteSpace(fileName))
        {
            Response.Headers.Append("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
        }

        return File(data, contentType);
    }

    public ActionResult JsonResultStream(string json)
    {
        json = json ?? String.Empty;

        Response
            .AddNoCacheHeaders()
            .AddPortalCorsHeaders(Request);

        return BinaryResultStream(System.Text.Encoding.UTF8.GetBytes(json), "application/json; charset=utf-8");
    }

    public ActionResult PlainResultStream(string text, string contentType)
    {
        text = text ?? String.Empty;

        return BinaryResultStream(System.Text.Encoding.UTF8.GetBytes(text), contentType);
    }

    #endregion
}
