using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Json;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode.Mvc;
using Portal.Core.AppCode.Services;
using Portal.Core.AppCode.Services.Authentication;
using Portal.Core.AppCode.Services.WebgisApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal.Core.Controllers;

public class ProxyController : PortalBaseController
{
    private readonly ILogger<ProxyController> _logger;
    private readonly ConfigurationService _config;
    private readonly WebgisApiService _api;
    private readonly WebgisCookieService _cookies;
    private readonly HmacService _hmac;
    private readonly ProxyService _proxy;
    private readonly ICryptoService _crypto;

    public ProxyController(ILogger<ProxyController> logger,
                           ConfigurationService config,
                           WebgisApiService api,
                           WebgisCookieService cookies,
                           HmacService hmac,
                           ProxyService proxy,
                           UrlHelperService urlHelper,
                           ICryptoService crypto,
                           IOptionsMonitor<ApplicationSecurityConfig> appSecurityConfig,
                           IEnumerable<ICustomPortalSecurityService> customSecurity = null)
        : base(logger, urlHelper, appSecurityConfig, customSecurity, crypto)
    {
        _logger = logger;
        _config = config;
        _api = api;
        _cookies = cookies;
        _hmac = hmac;
        _proxy = proxy;
        _crypto = crypto;
    }

    public IActionResult Index()
    {
        return ViewResult();
    }

    public IActionResult Headers()
    {
        return ViewResult();
    }

    #region ToolMethod

    //[ValidateInput(false)]
    async public Task<IActionResult> ToolMethod(string id, string method)
    {
        try
        {
            var result = await _api.CallToolMethodBytesAsync(HttpContext, id, method);

            string contentType = result.contentType?.ToLower() ?? String.Empty;
            if (contentType.Contains("application/json") || contentType.Contains("text") || contentType.Contains("html"))
            {
                return PlainView(System.Text.Encoding.UTF8.GetString(result.data), contentType);
            }

            return RawResponse(result.data, contentType);
        }
        catch (Exception ex)
        {
            return JsonObject(
                new
                {
                    success = false,
                    message = ex.Message
                });
        }
    }

    #endregion

    #region Query

    async public Task<IActionResult> Query(string id, bool encrypt = true)
    {
        if (id == "security-prefixes")
        {
            return JsonObjectEncrypted(new { value = _proxy.SecurityPrefixes(id) });
        }

        if (id == "security-autocomplete")
        {
            string term = Request.Query["term"];
            string prefix = Request.Query["prefix"];
            string cmsId = Request.Query["cmsid"];
            string subscriberId = Request.Query["userid"];

            return JsonObjectEncrypted(new { value = await _proxy.SecurityAutocomplete(this.Request, term, prefix, cmsId, subscriberId) });
        }

        if (id == "webgis-portal-auth2")
        {
            string cookievalue = _crypto.DecryptTextDefault(Request.Form["cookievalue"]);

            return JsonObjectEncrypted(
                new
                {
                    value = JSerializer.Serialize(
                        await _hmac.CreateHmacObjectAsync(await _cookies.TryGetCookieUser(HttpContext, cookievalue)))
                });
        }

        return JsonObjectEncrypted(new { });
    }

    #region Helper


    #endregion

    #endregion
}