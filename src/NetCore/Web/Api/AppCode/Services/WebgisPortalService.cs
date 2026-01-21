using E.Standard.Extensions.Collections;
using E.Standard.Extensions.ErrorHandling;
using E.Standard.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services;

public class WebgisPortalService
{
    public readonly UrlHelperService _urlHelper;
    public readonly ICryptoService _crypto;
    public readonly IHttpService _http;

    public WebgisPortalService(UrlHelperService urlHelper,
                               ICryptoService crypto,
                               IHttpService http)
    {
        _urlHelper = urlHelper;
        _crypto = crypto;
        _http = http;
    }

    async public Task<string[]> SecurityPrefixes(HttpContext context)
    {
        try
        {
            var prefixes = await QueryPortalValue<JsonSecurityPrefix[]>(context, "security-prefixes");

            List<string> ret = new List<string>();
            foreach (var prefix in prefixes)
            {
                ret.Add(prefix.name);
            }
            return ret.ToArray();
        }
        catch (Exception ex)
        {
            return new string[] { "ERROR:" + ex.SecureMessage() };
        }
    }

    async public Task<string[]> SecurityAutocomplete(HttpContext context, string term, string prefix = "")
    {
        try
        {
            var autocomplete = await QueryPortalValue<string[]>(context, "security-autocomplete?term=" + term + "&prefix=" + prefix);
            return autocomplete;
        }
        catch (Exception ex)
        {
            return new string[] { "ERROR:" + ex.SecureMessage() };
        }
    }

    async public Task<string> PortalAuth2CookieUser(HttpContext context, string cookieData)
    {
        try
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc["cookievalue"] = cookieData;

            return await QueryPortalValue<string>(context, "webgis-portal-auth2", nvc);
        }
        catch (Exception /*ex*/)
        {
            return null;
        }
    }

    async public Task<string> GetPortalAuth2CookieUser(HttpContext context)
    {
        var cookieValue = context.Request.Cookies[E.Standard.WebMapping.Core.Web.Const.WebgisPortalCookieName];
        if (!String.IsNullOrWhiteSpace(cookieValue))
        {
            return await PortalAuth2CookieUser(context, cookieValue);
        }

        return null;
    }

    #region Helper

    async private Task<T> QueryPortalValue<T>(HttpContext context, string method, NameValueCollection nvc = null)
    {
        string portalQueryUrl = _urlHelper.PortalInternalUrl();

        string jsonString = String.Empty;
        if (nvc == null)
        {
            var response = await _http.GetStringAsync($"{portalQueryUrl}/proxy/query/{method}",
                new RequestAuthorization()
                {
                    UseDefaultCredentials = true
                });
            jsonString = _crypto.DecryptTextDefault(response.Trim());
        }
        else
        {
            var nvcEnc = new NameValueCollection();
            foreach (string key in nvc.Keys)
            {
                nvcEnc[key] = _crypto.EncryptTextDefault(nvc[key]);
            }

            var response = await _http.PostValues($"{portalQueryUrl}/proxy/query/{method}",
                nvcEnc.ToKeyValuePairs(),
                new RequestAuthorization()
                {
                    UseDefaultCredentials = true
                });
            jsonString = _crypto.DecryptTextDefault(response.Trim());
        }

        var jsonObject = JSerializer.Deserialize<JsonValueResponse<T>>(jsonString);
        return jsonObject.value;
    }

    #endregion

    #region Helper Classes

    private class JsonValueResponse<T>
    {
        public T value { get; set; }
    }

    private class JsonSecurityPrefix
    {
        public JsonSecurityPrefix() { }
        public JsonSecurityPrefix(string n, string t)
        {
            this.name = n;
            this.type = t;
        }

        public string name { get; set; }
        public string type { get; set; }
    }

    #endregion
}
