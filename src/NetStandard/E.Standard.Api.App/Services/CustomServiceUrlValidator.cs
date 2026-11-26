using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using E.Standard.Api.App.Configuration;

using Microsoft.Extensions.Options;

namespace E.Standard.Api.App.Services;

/// <summary>
/// SSRF protection for user-added custom services: the url must be http/https, must not match the
/// host blacklist and its host must resolve to public addresses only (unless listed in
/// <see cref="CustomServicesOptions.AllowHosts"/>).
/// </summary>
/// <remarks>
/// The check is done before the service is created. The service requests themselves resolve the host
/// again (no connect-time check like <see cref="DynamicContentFetcher"/>), so DNS rebinding remains a residual risk.
/// </remarks>
public class CustomServiceUrlValidator
{
    private readonly CustomServicesOptions _options;

    public CustomServiceUrlValidator(IOptions<CustomServicesOptions> options)
    {
        _options = options.Value;
    }

    public bool IsEnabled => _options.Allow && !_options.HostBlacklist.Contains("forbidden");

    async public Task<bool> IsAllowedUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled
            || !Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || String.IsNullOrEmpty(uri.DnsSafeHost))
        {
            return false;
        }

        string host = uri.DnsSafeHost.ToLowerInvariant();

        foreach (var pattern in _options.HostBlacklist)
        {
            try
            {
                if (Regex.IsMatch(host, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)))
                {
                    return false;
                }
            }
            catch (ArgumentException)
            {
                // invalid pattern => fail closed
                return false;
            }
        }

        if (IsAllowedHost(host))
        {
            return true;
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        }
        catch
        {
            return false;
        }

        return addresses.Length > 0 && !addresses.Any(DynamicContentFetcher.IsNonPublicAddress);
    }

    private bool IsAllowedHost(string host)
    {
        foreach (var allowed in _options.AllowHosts)
        {
            if (allowed == "*"
                || allowed == host
                || (allowed.StartsWith("*.") && host.EndsWith(allowed.Substring(1))))
            {
                return true;
            }
        }

        return false;
    }
}
