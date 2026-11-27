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

    /// <summary>
    /// Encrypted form values the API posts to the portal <c>proxy/query/{method}</c> endpoint (e.g. <c>cookievalue</c>).
    /// </summary>
    public const string PortalProxyQueryRequest = "portal-proxy-query-request";

    /// <summary>
    /// Encrypted json response of the portal <c>proxy/query/{method}</c> endpoint (read by API and CMS).
    /// </summary>
    public const string PortalProxyQueryResponse = "portal-proxy-query-response";

    /// <summary>
    /// <c>IAppCryptography.SecurityEncryptString/SecurityDecryptString</c> of the API bridge: encrypted tool parameters,
    /// tool urls/ids (mobile files, georef image ids, ...) and form values the portal sends to API tools.
    /// </summary>
    public const string ApiBridge = "api-bridge";

    /// <summary>
    /// Encrypted captcha verification token (<c>CaptchaCodeEncrypted</c>) of the subscriber login.
    /// </summary>
    public const string CaptchaToken = "captcha-token";

    /// <summary>
    /// Encrypted MFA flow token (login/setup stages, may contain the new TOTP secret during setup).
    /// </summary>
    public const string MfaFlowToken = "mfa-flow-token";

    /// <summary>
    /// Encrypted <c>complex#...</c> property values the CMS property grid round-trips through the browser.
    /// </summary>
    public const string CmsComplexPropertyValue = "cms-complex-property-value";

    /// <summary>
    /// Encrypted connection ("url=...;name=...;usr=...;pwd=...") of user-added custom services
    /// (<c>custom_request_parameters.connection</c> / <c>custom.{serviceId}.connection</c>, also stored in saved maps).
    /// </summary>
    public const string CustomServiceConnection = "custom-service-connection";

    /// <summary>
    /// Encrypted storage path of an uploaded portal content image (<c>iid</c> in <c>./{page}/ContentImage/?iid=...</c>,
    /// persisted in the content html). Older content may still contain legacy (default key) iids.
    /// </summary>
    public const string PortalContentImage = "portal-content-image";

    /// <summary>
    /// Encrypted storage path of a georeferenced image (<c>id</c> in <c>rest/tooldata/.../get?id=...</c>, overlay urls).
    /// Only generated per response, never persisted.
    /// </summary>
    public const string GeorefImageUrl = "georef-image-url";

    /// <summary>
    /// HMAC signature of TOC vis filters ("id|args", <c>sig</c> of the filter, also persisted in saved/shared maps).
    /// Legacy signatures (hash of the default key encryption, args only) are still accepted.
    /// </summary>
    public const string VisFilterSignature = "visfilter-signature";

    /// <summary>
    /// HMAC signature of dynamic content urls (<c>url#webgis-sig=...</c>, "type|url"), also persisted in MapBuilder html snippets.
    /// Legacy signatures (hash of the default key encryption) are still accepted.
    /// </summary>
    public const string DynamicContentSignature = "dynamic-content-signature";
}
