#nullable enable

using E.Standard.Security.Cryptography.Abstractions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;

namespace Api.Core.AppCode.Services.Authentication;

public class ApiCookieAuthenticationService
{
    private readonly ICryptoService _crypto;

    public ApiCookieAuthenticationService(ICryptoService crypto)
    {
        _crypto = crypto;
    }

    public void SetAuthCookie(HttpContext context, string cookieValue/*, bool persistentCookie = false*/)
    {
        context.Response.Cookies.Append(AuthCookieName, _crypto.EncryptCookieValue(cookieValue), new Microsoft.AspNetCore.Http.CookieOptions()
        {
            HttpOnly = true
            //Expires=DateTimeOffset.
        });
        context.Response.Headers.Append("P3P", "CP='IDC DSP COR ADM DEVi TAIi PSA PSD IVAi IVDi CONi HIS OUR IND CNT'");
    }

    public void SignOut(HttpContext context)
    {
        context.Response.Cookies.Delete(AuthCookieName);
    }

    public string? TryGetCookieUsername(HttpContext context)
    {
        try
        {
            string cookieValue = _crypto.DecryptCookieValue(context.Request.Cookies[AuthCookieName]) ?? String.Empty;

            return cookieValue;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            context.Response.Cookies.Delete(AuthCookieName);
        }
        catch (E.Standard.Security.Cryptography.Exceptions.CryptographyException)
        {
            context.Response.Cookies.Delete(AuthCookieName);
        }
        catch { }

        return null;
    }

    private const string AuthCookieName = "webgis5core-api-auth";

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
        public string? Value { get; set; }

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
}
