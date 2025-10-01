using E.DataLinq.Core;
using E.DataLinq.Web.Services.Abstraction;
using E.Standard.Api.App.Extensions;
using E.Standard.Custom.Core;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.DataLinq;

class DatalinqHostAuthenticationService : IHostAuthenticationService
{
    private readonly WebgisPortalService _portal;
    private readonly UrlHelperService _urlHelper;

    public DatalinqHostAuthenticationService(WebgisPortalService portal,
                                             UrlHelperService urlHelper)
    {
        _portal = portal;
        _urlHelper = urlHelper;
    }

    async public Task<IEnumerable<string>> AuthAutocompleteAsync(HttpContext httpContext, string prefix, string term)
    {
        return await _portal.SecurityAutocomplete(httpContext, term, prefix);
    }

    async public Task<IEnumerable<string>> AuthPrefixesAsync(HttpContext httpContext)
    {
        return await _portal.SecurityPrefixes(httpContext);
    }

    async public Task<string> ClientSideAuthObjectStringAsync(HttpContext httpContext)
    {
        string clientId = await _portal.GetPortalAuth2CookieUser(httpContext);

        return !String.IsNullOrWhiteSpace(clientId) ?
            clientId :
            $"'{_urlHelper.HMacUrl(httpContext)}'";
    }

    public IDataLinqUser GetUser(HttpContext httpContext)
    {
        var ui = httpContext?.User?.ToUserIdentification(ApiAuthenticationTypes.Hmac);

        return new DataLinqUser(ui?.Username, ui?.Userroles, ui?.UserrolesParameters);
    }

    #region Classes

    private class DataLinqUser : IDataLinqUser
    {
        public DataLinqUser(string username,
                            IEnumerable<string> userroles,
                            IEnumerable<string> userrolesParameters)
        {
            this.Username = username ?? string.Empty;
            this.Userroles = userroles ?? new string[0];
            this.Claims = userrolesParameters ?? new string[0];
        }

        public string Username { get; }

        public IEnumerable<string> Userroles { get; }

        public IEnumerable<string> Claims { get; }
    }

    #endregion
}
