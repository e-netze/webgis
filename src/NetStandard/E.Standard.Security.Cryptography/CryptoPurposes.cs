namespace E.Standard.Security.Cryptography;

/// <summary>
/// Purposes for <c>ICryptoService.SignText</c>/<c>EncryptAuthenticated</c> that are shared between Portal and API.
/// </summary>
public static class CryptoPurposes
{
    /// <summary>
    /// Signed "clientid|unix-seconds" the MapBuilder/AppBuilder passes instead of an http referer when requesting a hmac.
    /// </summary>
    public const string HmacRefererSubstitution = "hmac-referer-substitution";

    /// <summary>
    /// Signed "clientid|unix-seconds" the API (subscriber clients page) passes to the portal MapBuilder/AppBuilder
    /// as <c>clientid-sig</c>, to proof that the caller owns the clientid.
    /// </summary>
    public const string BuilderClientId = "builder-clientid";

    /// <summary>
    /// Deterministically encrypted "serviceId|tileUrl" in <c>/tilecache/redirect/{serviceId}/{token}/...</c> urls
    /// (secured WMTS tiles). Deterministic, so the tile urls stay browser-cacheable.
    /// </summary>
    public const string TileCacheRedirect = "tilecache-redirect";

    /// <summary>
    /// Encrypted hotlink url in <c>payload:...</c> links of query results, resolved by <c>rest/resolve-url-payload</c>.
    /// </summary>
    public const string HotlinkUrlPayload = "hotlink-url-payload";

    /// <summary>
    /// Encrypted output file name (<c>downloadid</c>) for <c>rest/download</c> (prints, exports, tool downloads).
    /// </summary>
    public const string DownloadId = "download-id";

    /// <summary>
    /// Encrypted OGC ticket in the <c>ogc_ticket_logout</c> parameter of the OGC logout url.
    /// </summary>
    public const string OgcLogoutTicket = "ogc-logout-ticket";
}
