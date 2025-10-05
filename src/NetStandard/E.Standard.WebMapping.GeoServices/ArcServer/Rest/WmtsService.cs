using E.Standard.Configuration.Services;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Web.Exceptions;
using E.Standard.WebGIS.Api.Abstractions;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.GeoServices.ArcServer.Services;
using E.Standard.WebMapping.GeoServices.Tiling;
using E.Standard.WebMapping.GeoServices.Tiling.Models;
using System;
using System.Net;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest;

public class WmtsService : BaseWmtsService, IMapServiceAuthentication
{
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

        this.Username = authUser;
        this.Password = authPwd;

        return true;
    }

    #endregion

    #region IMapServiceAuthentication

    public string Username { get; private set; }
    public string Password { get; private set; }
    public string StaticToken { get; private set; }

    public int TokenExpiration
    {
        get;
        set;
    }

    public ICredentials HttpCredentials { get; set; }

    public string ServiceUrl => this.Server;

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

        clone.Username = this.Username;
        clone.Password = this.Password;
        clone.TokenExpiration = this.TokenExpiration;

        return clone;
    }

    #endregion

    #region Overrides

    // Called from Redirect API Endpoint
    async public override Task<byte[]> GetSecuredData(IRequestContext requestContext, string url)
    {
        var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();

        var responseBytes = await authHandler.TryGetRawAsync(this, url);
        return responseBytes;
    }

    async protected override Task<string> DownloadAsync(IRequestContext requestContext, string url)
    {
        var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();

        string responseString = await authHandler.TryGetAsync(this, url);
        return responseString;
    }

    // Called from TileService.GetPrintMapAsync
    async override internal Task<TileData> DownloadTile(IRequestContext requestContext, TileData tileData)
    {
        try
        {
            tileData.Data = await GetSecuredData(requestContext, tileData.Url);

            return tileData;
        }
        catch (HttpServiceException ex)
        {
            if (ex.StatusCode == HttpStatusCode.BadRequest
                || ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Tile not found, return null
                return null;
            }

            throw;
        }
    }

    public override (string tileUrl, string[] domains) ImageUrlPro(
            IRequestContext requestContext, IMap map)
    {
        var result = base.ImageUrlPro(requestContext, map);

        if (!String.IsNullOrEmpty(this.Username) && !String.IsNullOrEmpty(this.Password))
        {
            var config = requestContext.GetRequiredService<ConfigurationService>();

            if ("true".Equals(config["Api:secured-tiles-redirect:use-with-ogc-wmts"], StringComparison.OrdinalIgnoreCase))
            {
                var crypto = requestContext.GetRequiredService<ICryptoService>();
                var urlHelper = requestContext.GetRequiredService<IUrlHelperService>();

                result.tileUrl = $"{urlHelper.AppRootUrl()}/tilecache/redirect/{this.Url}/{crypto.StaticDefaultEncrypt(result.tileUrl, Security.Cryptography.CryptoResultStringType.Hex)}/[LEVEL]/[ROW]/[COL]";
            }
        }

        return result;
    }

    #endregion
}
