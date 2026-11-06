using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Api.App.Services;

/// <summary>
/// Settings for <see cref="DynamicContentFetcher"/> (server-side download of dynamic content
/// like GeoJSON/GeoRSS, see <c>RestController.DynamicContentProxy</c>).
/// </summary>
/// <remarks>
/// Configurable via <c>api.config</c>:
/// <list type="bullet">
/// <item><c>dynamic-content:allow-hosts</c> → <see cref="AllowedHosts"/></item>
/// <item><c>dynamic-content:timeout-seconds</c> → <see cref="TimeoutSeconds"/></item>
/// <item><c>dynamic-content:max-response-bytes</c> → <see cref="MaxResponseBytes"/></item>
/// <item><c>dynamic-content:max-redirects</c> → <see cref="MaxRedirects"/></item>
/// <item><c>dynamic-content:allow-unsigned</c> → <see cref="AllowUnsigned"/></item>
/// </list>
/// </remarks>
public static class DynamicContentSettings
{
    private static string[] _allowedHosts = Array.Empty<string>();

    /// <summary>
    /// Host names (or <c>*.domain</c> patterns) that may resolve to loopback, private or link-local
    /// addresses. Everything else is only allowed to resolve to public addresses.
    /// </summary>
    public static IReadOnlyCollection<string> AllowedHosts => _allowedHosts;

    public static int TimeoutSeconds { get; set; } = 20;

    public static long MaxResponseBytes { get; set; } = 50L * 1024 * 1024;

    public static int MaxRedirects { get; set; } = 5;

    /// <summary>
    /// If true, dynamic content urls (georss, geojson, api-query, ...) without a valid signature are still downloaded (e.g. maps created
    /// as static html by the MapBuilder or by custom client code).     Default: false (secure).
    /// </summary>
        public static bool AllowUnsigned { get; set; } = false;

    public static void SetAllowedHosts(string hosts)
    {
        _allowedHosts = (hosts ?? String.Empty)
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(h => h.Trim().ToLowerInvariant())
            .Where(h => h.Length > 0)
            .ToArray();
    }

    internal static bool IsAllowedHost(string host)
    {
        host = (host ?? String.Empty).ToLowerInvariant();

        foreach (var allowed in _allowedHosts)
        {
            if (allowed == host
                || (allowed.StartsWith("*.") && host.EndsWith(allowed.Substring(1))))
            {
                return true;
            }
        }

        return false;
    }
}
