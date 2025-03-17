using E.Standard.Configuration.Services;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Web.Models;
using E.Standard.WebGIS.Api.Abstractions;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Proxy;
using E.Standard.WebMapping.GeoServices.Tiling;
using System;
using System.Net;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.OGC.WMTS;

public class WmtsService : BaseWmtsService, IServiceDescription
{
    private string _authUser;
    private string _authPassword;
    private string _token;
    private TicketType _ticket = null;
    private WebProxy _ticketServiceProxy = null;

    public WmtsService(string url,
                       string layer,
                       string tileMatrixSet,
                       string style,
                       string imageFormat,
                       string[] resourceUrls,
                       int maxLevel,
                       bool hideBeyondMaxLevel)
        : base(url, layer, tileMatrixSet, style, imageFormat, resourceUrls, maxLevel, hideBeyondMaxLevel)
    {

    }

    #region IService Member

    public override bool PreInit(string serviceID, string server, string url, string authUser, string authPwd, string token, string appConfigPath, ServiceTheme[] serviceThemes)
    {

        base.PreInit(serviceID, server, url, authUser, authPwd, token, appConfigPath, serviceThemes);

        _authUser = authUser;
        _authPassword = authPwd;
        _token = token;

        return true;
    }

    public override Task<bool> InitAsync(IMap map, IRequestContext requestContext)
    {
        var httpService = requestContext.Http;

        _ticketServiceProxy = String.IsNullOrEmpty(this.TicketServer)
            ? null
            : httpService.GetProxy(this.TicketServer);

        return base.InitAsync(map, requestContext);
    }

    #endregion

    #region IClone Member

    override public IMapService Clone(IMap parent)
    {
        WmtsService clone = new WmtsService(
            base._url,
            base._layer,
            base._tileMatrixSet,
            base._style,
            base._imageFormat,
            base._tileUrls,
            base._maxLevel,
            base.HideBeyondMaxLevel);

        base.Clone(clone, parent);

        clone.TicketServer = TicketServer;
        clone._ticketServiceProxy = _ticketServiceProxy;

        clone._authUser = _authUser;
        clone._authPassword = _authPassword;
        clone._token = _token;
        clone._ticket = _ticket;

        return clone;
    }

    #endregion

    public string TicketServer
    {
        get; set;
    }

    public override Task<byte[]> GetSecuredData(IRequestContext context, string url)
    {
        string tokenParameter = GetTokenAsUrlParameter(context);

        if (!String.IsNullOrEmpty(tokenParameter))
        {
            url = $"{url}{(url.Contains("?") ? "&" : "?")}{tokenParameter}";
        }

        return context.Http.GetDataAsync(url);
    }

    private string GetTokenAsUrlParameter(IRequestContext context)
    {
        string parameter = String.Empty;
        if (!String.IsNullOrWhiteSpace(_token))
        {
            parameter = _token.Contains("=") ? _token : "token=" + _token;
        }
        if (String.IsNullOrWhiteSpace(this.TicketServer) || parameter.StartsWith("ogc_ticket="))
        {
            return parameter;
        }

        try
        {
            if (_ticket == null || _ticket.WillExpired(3600))
            {
                _ticket = TicketClient.GetTicketType(
                        context.Http,
                        this.TicketServer,
                        _authUser, _authPassword,
                        _ticketServiceProxy, 3600 * 8).GetAwaiter().GetResult();
            }

            return parameter + (String.IsNullOrWhiteSpace(parameter) ? "" : "&") + "ogc_ticket=" + _ticket.Token;
        }
        catch (Exception /*ex*/)
        {
            return String.Empty;
        }
    }

    protected override string ImageUrl(IRequestContext requestContext, IMap map, int index)
    {
        var imageUrl = base.ImageUrl(requestContext, map, index);
        var tokenParameter = GetTokenAsUrlParameter(requestContext);

        if (!String.IsNullOrEmpty(tokenParameter))
        {
            imageUrl = $"{imageUrl}{(imageUrl.Contains("?") ? "&" : "?")}{tokenParameter}";
        }

        return imageUrl;
    }

    public override (string tileUrl, string[] domains) ImageUrlPro(
        IRequestContext requestContext, IMap map)
    {
        var result = base.ImageUrlPro(requestContext, map);
        var tokenParameter = GetTokenAsUrlParameter(requestContext);

        if (!String.IsNullOrEmpty(tokenParameter))
        {
            var config = requestContext.GetRequiredService<ConfigurationService>();

            if ("true".Equals(config["Api:secured-tiles-redirect:use-with-ogc-wmts"], StringComparison.OrdinalIgnoreCase))
            {
                var crypto = requestContext.GetRequiredService<ICryptoService>();
                var urlHelper = requestContext.GetRequiredService<IUrlHelperService>();

                result.tileUrl = $"{urlHelper.AppRootUrl()}/tilecache/redirect/{this.Url}/{crypto.StaticDefaultEncrypt(result.tileUrl, Security.Cryptography.CryptoResultStringType.Hex)}/[LEVEL]/[ROW]/[COL]";
            }
            else
            {
                result.tileUrl = $"{result.tileUrl}{(result.tileUrl.Contains("?") ? "&" : "?")}{tokenParameter}";
            }
        }

        return result;
    }

    async protected override Task<string> DownloadAsync(IRequestContext requestContext, string url)
    {
        string tokenParameter = GetTokenAsUrlParameter(requestContext);

        if (!String.IsNullOrEmpty(tokenParameter))
        {
            url = $"{url}{(url.Contains("?") ? "&" : "?")}{tokenParameter}";
        }

        string responseString = await requestContext.Http.GetStringAsync(
            url,
            new RequestAuthorization()
            {
                Username = _authUser,
                Password = _authPassword
            }
        );

        return responseString;
    }
}
