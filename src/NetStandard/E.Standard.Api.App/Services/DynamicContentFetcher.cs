using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace E.Standard.Api.App.Services;

/// <summary>
/// Downloads user-supplied URLs (dynamic content) with SSRF protection: only http/https,
/// no loopback/private/link-local targets (checked on the connected IP address, so DNS rebinding
/// does not help), redirects are followed manually and validated per hop, with timeout and size limit.
/// </summary>
public static class DynamicContentFetcher
{
    private static readonly HttpClient _directClient = new HttpClient(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseProxy = false,
        ConnectCallback = ConnectToPublicAddressAsync
    })
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    private static WebProxy _proxyClientProxy;
    private static HttpClient _proxyClient;
    private static readonly object _proxyClientLock = new object();

    async public static Task<string> GetStringAsync(string url, WebProxy proxy = null, CancellationToken cancellationToken = default)
    {
        var (data, charset) = await GetAsync(url, proxy, cancellationToken);

        Encoding encoding = Encoding.UTF8;
        if (!String.IsNullOrEmpty(charset))
        {
            try { encoding = Encoding.GetEncoding(charset.Trim('"')); } catch { }
        }

        using var reader = new StreamReader(new MemoryStream(data), encoding, true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    async public static Task<byte[]> GetBytesAsync(string url, WebProxy proxy = null, CancellationToken cancellationToken = default)
        => (await GetAsync(url, proxy, cancellationToken)).data;

    async private static Task<(byte[] data, string charset)> GetAsync(string url, WebProxy proxy, CancellationToken cancellationToken)
    {
        var uri = ParseUri(url);
        var client = proxy == null ? _directClient : GetProxyClient(proxy);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, DynamicContentSettings.TimeoutSeconds)));

        for (int redirects = 0; ; redirects++)
        {
            if (proxy != null)
            {
                // the connect callback is not used with a proxy: check the host before the request
                await EnsurePublicHostAsync(uri.Host, cts.Token);
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400 && response.Headers.Location != null)
            {
                if (redirects >= DynamicContentSettings.MaxRedirects)
                {
                    throw new InvalidOperationException("Dynamic content: too many redirects");
                }

                uri = ParseUri(new Uri(uri, response.Headers.Location).ToString());
                continue;
            }

            response.EnsureSuccessStatusCode();

            long maxBytes = DynamicContentSettings.MaxResponseBytes;
            if (maxBytes > 0 && response.Content.Headers.ContentLength > maxBytes)
            {
                throw new InvalidOperationException("Dynamic content: response is too large");
            }

            using var ms = new MemoryStream();
            await using (var stream = await response.Content.ReadAsStreamAsync(cts.Token))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await stream.ReadAsync(buffer, cts.Token)) > 0)
                {
                    if (maxBytes > 0 && ms.Length + read > maxBytes)
                    {
                        throw new InvalidOperationException("Dynamic content: response is too large");
                    }

                    ms.Write(buffer, 0, read);
                }
            }

            return (ms.ToArray(), response.Content.Headers.ContentType?.CharSet);
        }
    }

    private static Uri ParseUri(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !String.IsNullOrEmpty(uri.UserInfo))
        {
            throw new InvalidOperationException("Dynamic content: only absolute http/https urls without credentials are allowed");
        }

        return uri;
    }

    private static HttpClient GetProxyClient(WebProxy proxy)
    {
        lock (_proxyClientLock)
        {
            if (_proxyClient == null || !ReferenceEquals(_proxyClientProxy, proxy))
            {
                _proxyClientProxy = proxy;
                _proxyClient = new HttpClient(new SocketsHttpHandler
                {
                    AllowAutoRedirect = false,
                    Proxy = proxy,
                    UseProxy = true
                })
                {
                    Timeout = Timeout.InfiniteTimeSpan
                };
            }

            return _proxyClient;
        }
    }

    async private static Task EnsurePublicHostAsync(string host, CancellationToken cancellationToken)
    {
        await ResolvePublicAddressesAsync(host, cancellationToken);
    }

    async private static Task<IPAddress[]> ResolvePublicAddressesAsync(string host, CancellationToken cancellationToken)
    {
        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);

        if (!DynamicContentSettings.IsAllowedHost(host))
        {
            addresses = addresses.Where(a => !IsNonPublicAddress(a)).ToArray();
        }

        if (addresses.Length == 0)
        {
            throw new InvalidOperationException("Dynamic content: host is not allowed");
        }

        return addresses;
    }

    async private static ValueTask<Stream> ConnectToPublicAddressAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var addresses = await ResolvePublicAddressesAsync(context.DnsEndPoint.Host, cancellationToken);

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    internal static bool IsNonPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return true;
        }

        var b = address.GetAddressBytes();

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return b[0] == 0                                    // 0.0.0.0/8
                || b[0] == 10                                   // 10.0.0.0/8
                || (b[0] == 100 && (b[1] & 0xC0) == 64)         // 100.64.0.0/10 (CGNAT)
                || (b[0] == 169 && b[1] == 254)                 // 169.254.0.0/16 (link-local, cloud metadata)
                || (b[0] == 172 && (b[1] & 0xF0) == 16)         // 172.16.0.0/12
                || (b[0] == 192 && b[1] == 168)                 // 192.168.0.0/16
                || (b[0] == 192 && b[1] == 0 && b[2] == 0)      // 192.0.0.0/24
                || (b[0] == 198 && (b[1] & 0xFE) == 18)         // 198.18.0.0/15
                || b[0] >= 224;                                 // multicast, reserved, broadcast
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal
                || address.IsIPv6Multicast
                || (b[0] & 0xFE) == 0xFC;                       // fc00::/7 (unique local)
        }

        return true;
    }
}
