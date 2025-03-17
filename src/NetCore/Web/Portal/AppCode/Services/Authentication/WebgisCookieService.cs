using E.Standard.Custom.Core.Abstractions;
using E.Standard.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.Core;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Portal.Core.AppCode.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Services.Authentication;

public class WebgisCookieService
{
    private readonly IEnumerable<IPortalAuthenticationService> _authenticationServices;
    private readonly UrlHelperService _urlHelper;
    private readonly ICryptoService _crypto;

    protected const string AuthCookieName = "webgis5core-portal-auth2";

    public WebgisCookieService(UrlHelperService urlHelper,
                               IEnumerable<IPortalAuthenticationService> authenticationServices,
                               ICryptoService crypto)
    {
        _authenticationServices = authenticationServices;
        _urlHelper = urlHelper;
        _crypto = crypto;
    }

    async public Task<PortalUser> TryGetCookieUser(HttpContext context, string cookieValue = null)
    {
        try
        {
            var data = GetCookieData(context, cookieValue);
            if (data != null)
            {
                switch ((UserType)data.AuthType)
                {
                    case UserType.ApiSubscriber:
                        return new PortalUser(data.Value, displayName: data.Displayname, userRoles: data.Roles, roleParameters: null);
                    case UserType.CloudPortalLogin:
                        return new PortalUser(data.Value, displayName: data.Displayname, userRoles: data.Roles, roleParameters: null);
                    default:
                        var authenticationService = _authenticationServices
                                                        .Where(a => a.UserType == ((UserType)data.AuthType))
                                                        .FirstOrDefault();

                        if (authenticationService != null)
                        {
                            var authenticationServiceUser = await authenticationService.TryAuthenticationServiceUser(context, data.Value, true);
                            return authenticationServiceUser?.ToPortalUser();
                        }
                        return null;
                }
            }
        }
        catch (System.Security.Cryptography.CryptographicException)  // cookie not valid anymore
        {
            context.Response.Cookies.Delete(AuthCookieName);
        }
        catch (E.Standard.Security.Cryptography.Exceptions.CryptographyException)  // cookie not valid anymore
        {
            context.Response.Cookies.Delete(AuthCookieName);
        }
        catch (System.Runtime.InteropServices.COMException)   // (windows) LDAP: can't query roles, may not a windows user name
        {
            context.Response.Cookies.Delete(AuthCookieName);
        }
        catch { }

        return null;
    }

    public void SetAuthCookie(
        HttpContext context,
        bool persistentCookie,
        string userName,
        UserType authType,
        string displayName = null,
        string[] userRoles = null,
        DateTimeOffset? expires = null)
    {
        var data = new CookieData()
        {
            Value = userName,
            AuthType = (int)authType,
            Displayname = displayName,
            Roles = userRoles
        };

        string dataString = _crypto.EncryptTextDefault(JSerializer.Serialize(data));

        //if (persistentCookie)
        //    cookie.Expires = DateTime.Now.AddDays(7);

        CookieOptions options = null;

        if (expires != null)
        {
            options = new CookieOptions()
            {
                Expires = expires
            };
        }

        if (options == null)
        {
            context.Response.Cookies.Append(AuthCookieName, _crypto.EncryptTextDefault(dataString));
        }
        else
        {
            context.Response.Cookies.Append(AuthCookieName, _crypto.EncryptTextDefault(dataString), options);
        }
        context.Response.Headers.Append("P3P", "CP='IDC DSP COR ADM DEVi TAIi PSA PSD IVAi IVDi CONi HIS OUR IND CNT'");
    }

    public bool HasAuthCookie(HttpContext context)
    {
        return context.Request.Cookies[AuthCookieName] != null;
    }

    //public string GetAuthCookieUsername(HttpContext context)
    //{
    //    var data = GetCookieData(context);

    //    return data?.Value ?? String.Empty;
    //}

    private static readonly Dictionary<string, string> _setAuthCookeiFor = new Dictionary<string, string>()
    {
        { "app","PortalApp" },
        { "auth", "Login" },
        { "home", "Index" },
        //{ "home", "SecurityPrefixes" },
        //{ "home", "SecurityAutocomplete" },
        { "map", "Index" }
    };

    public bool SetAuthCookieFor(string controllerName, string actionName)
    {
        return
            _setAuthCookeiFor.ContainsKey(controllerName.ToLower()) &&
            _setAuthCookeiFor[controllerName.ToLower()].Equals(actionName, StringComparison.InvariantCultureIgnoreCase);
    }

    public bool SetAuthCookieFor(HttpContext context)
    {
        var endPointInfo = _urlHelper.GetEndpointInfo(context);

        return SetAuthCookieFor(endPointInfo.controllerName, endPointInfo.actionName);
    }

    #region Helpers

    private CookieData GetCookieData(HttpContext context, string cookieValue = null)
    {
        cookieValue = cookieValue ?? context.Request.Cookies[AuthCookieName];
        if (cookieValue == null)
        {
            return null;
        }

        var dataString = _crypto.DecryptTextDefault(_crypto.DecryptTextDefault(cookieValue));
        var data = JSerializer.Deserialize<CookieData>(dataString);

        return data;
    }

    #endregion

    #region Classes

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

        [JsonProperty(PropertyName = "roles", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("roles")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string[] Roles { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsExpired
        {
            get { return DateTime.UtcNow > new DateTime(Expires, DateTimeKind.Utc); }
        }
    }

    #endregion
}
