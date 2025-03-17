using E.Standard.Extensions.Text;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Exceptions;
using E.Standard.Web.Extensions;
using E.Standard.Web.Models;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace E.Standard.Web.Services;

public class HttpService : IHttpService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly HttpServiceOptions _options;

    public HttpService(IHttpClientFactory httpClientFactory,
                       IHttpContextAccessor contextAccessor,
                       IOptionsMonitor<HttpServiceOptions> optionsMonitor)
    {
        _httpClientFactory = httpClientFactory;
        _contextAccessor = contextAccessor;
        _options = optionsMonitor?.CurrentValue ?? new HttpServiceOptions();
    }

    #region Get

    async public Task<byte[]> GetDataAsync(string url,
                                           RequestAuthorization? authorization = null,
                                           int timeOutSeconds = 20)
    {
        url = PrepareUrl(url);

        using (var request = new HttpRequestMessage(HttpMethod.Get, url))
        {
            // ToDo: brauch man, wenn man Google Tiles downloade möchte...
            //request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Mozilla/5.0 (Windows NT 6.1; WOW64; rv:34.0) Gecko/20100101 Firefox/34.0"));

            request.AddAuthentication(authorization);

            using (var cts = new CancellationTokenSource(timeOutSeconds * 1000))
            {
                HttpResponseMessage? responseMessage = null;
                try
                {

                    if (authorization?.ClientCerticate == null &&
                        authorization?.UseDefaultCredentials != true)
                    {
                        var client = Create(url);
                        responseMessage = await client.SendAsync(request, cts.Token);
                    }
                    else
                    {
                        using (var clientHandler = new HttpClientHandler())
                        {
                            if (authorization.ClientCerticate != null)
                            {
                                clientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
                                clientHandler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                                clientHandler.ClientCertificates.Add(authorization.ClientCerticate);
                            }
                            else
                            {
#pragma warning disable SYSLIB0039 // allow old protocols (tls, tls11)
                                clientHandler.SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13 | SslProtocols.Tls;
#pragma warning restore SYSLIB0039 // allow old protocols (tls, tls11)
                                clientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                            }
                            clientHandler.UseDefaultCredentials = authorization.UseDefaultCredentials;

                            using (var client = new HttpClient(clientHandler))
                            {
                                responseMessage = await client.SendAsync(request, cts.Token);
                            }
                        }
                    }
                    if (responseMessage.IsSuccessStatusCode)
                    {
                        var bytes = await responseMessage.Content.ReadAsByteArrayAsync();

                        return bytes;
                    }
                    else
                    {
                        throw new HttpServiceException(responseMessage.StatusCode);
                    }
                }
                //catch /*(TaskCanceledException ex)*/
                //{
                //    //if (ex.CancellationToken == cts.Token)
                //    {
                //        throw new System.Exception("The http operation is canceled (timed out)!");
                //    }
                //}
                finally
                {
                    if (responseMessage != null)
                    {
                        responseMessage.Dispose();
                    }
                }
            }
        }
    }

    async public Task<string> GetStringAsync(string url,
                                             RequestAuthorization? authorization = null,
                                             int timeOutSeconds = 20,
                                             Encoding? encoding = null)
    {
        var bytes = await GetDataAsync(url, authorization, timeOutSeconds);

        return EncodeBytes(bytes, encoding);
    }

    async public Task<IBitmap> GetImageAsync(string url,
                                            RequestAuthorization? authentication = null,
                                            int timeOutSeconds = 20)
    {
        using (var ms = new MemoryStream(await GetDataAsync(url)))
        {
            return Current.Engine.CreateBitmap(ms);
        }
    }

    public Task<IEnumerable<T>> GetDataAsync<T>(IEnumerable<T> data,
                                                RequestAuthorization? authorization = null,
                                                int timeOutSeconds = 20)
        where T : IUrlData
    {
        var tasks = data.Select(d =>
            Task.Run(async () =>
            {
                try
                {
                    d.Exception = null;
                    d.Data = await GetDataAsync(d.Url, authorization, timeOutSeconds);
                }
                catch (Exception ex)
                {
                    d.Exception = ex;
                    d.Data = null;
                }
            })
        );

        Task.WaitAll(tasks.ToArray());

        return Task.FromResult(data);
    }

    #endregion

    #region Post

    async public Task<string> PostFormUrlEncodedStringAsync(string url,
                                                            string postData,
                                                            RequestAuthorization? authorization = null,
                                                            int timeOutSeconds = 20)
    {
        url = PrepareUrl(url);

        using (var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded")
        })
        {
            request.AddAuthentication(authorization);

            using (var cts = new CancellationTokenSource(timeOutSeconds * 1000))
            {
                HttpResponseMessage? responseMessage = null;

                try
                {
                    var client = Create(url);
                    responseMessage = await client.SendAsync(request, cts.Token);

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        var bytes = await responseMessage.Content.ReadAsByteArrayAsync();

                        return Encoding.UTF8.GetString(bytes);
                    }
                    else
                    {
                        throw new HttpServiceException(responseMessage.StatusCode);
                    }
                }
                //catch /*(TaskCanceledException ex)*/
                //{
                //    //if (ex.CancellationToken == cts.Token)
                //    {
                //        throw new System.Exception("The http operation is canceled (timed out)!");
                //    }
                //}
                finally
                {
                    if (responseMessage != null)
                    {
                        responseMessage.Dispose();
                    }
                }
            }
        }
    }

    async public Task<(byte[] data, string contentType)> PostFormUrlEncodedAsync(string url,
                                                      byte[] postData,
                                                      RequestAuthorization? authorization = null,
                                                      int timeOutSeconds = 20)
    {
        var dataString = Encoding.UTF8.GetString(postData);

        url = PrepareUrl(url);

        using (var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(dataString, Encoding.UTF8, "application/x-www-form-urlencoded")
        })
        {
            request.AddAuthentication(authorization);

            using (var cts = new CancellationTokenSource(timeOutSeconds * 1000))
            {
                HttpResponseMessage? responseMessage = null;

                try
                {
                    var client = Create(url);

                    responseMessage = await client.SendAsync(request, cts.Token);

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        var bytes = await responseMessage.Content.ReadAsByteArrayAsync();

                        string contentType = "";
                        if (responseMessage.Content.Headers.TryGetValues("Content-Type", out IEnumerable<string>? values))
                        {
                            contentType = String.Join(";", values);
                        }

                        return (bytes, contentType);
                    }
                    else
                    {
                        throw new HttpServiceException(responseMessage.StatusCode);
                    }
                }
                //catch /*(TaskCanceledException ex)*/
                //{
                //    //if (ex.CancellationToken == cts.Token)
                //    {
                //        throw new System.Exception("The http operation is canceled (timed out)!");
                //    }
                //}
                finally
                {
                    if (responseMessage != null)
                    {
                        responseMessage.Dispose();
                    }
                }
            }
        }
    }

    async public Task<(byte[] data, string contentType)> PostDataAsync(string url,
                                            byte[] postData,
                                            RequestAuthorization? authorization = null,
                                            int timeOutSeconds = 20)
    {
        url = PrepareUrl(url);

        using (var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new ByteArrayContent(postData, 0, postData.Length)
        })
        {
            request.AddAuthentication(authorization);

            using (var cts = new CancellationTokenSource(timeOutSeconds * 1000))
            {
                HttpResponseMessage? responseMessage = null;

                try
                {
                    var client = Create(url);
                    responseMessage = await client.SendAsync(request, cts.Token);

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        var bytes = await responseMessage.Content.ReadAsByteArrayAsync();

                        string contentType = "";
                        if (responseMessage.Content.Headers.TryGetValues("Content-Type", out IEnumerable<string>? values))
                        {
                            contentType = String.Join(";", values);
                        }

                        return (bytes, contentType);
                    }
                    else
                    {
                        throw new HttpServiceException(responseMessage.StatusCode);
                    }
                }
                //catch /*(TaskCanceledException ex)*/
                //{
                //    //if (ex.CancellationToken == cts.Token)
                //    {
                //        throw new System.Exception("The http operation is canceled (timed out)!");
                //    }
                //}
                finally
                {
                    if (responseMessage != null)
                    {
                        responseMessage.Dispose();
                    }
                }
            }
        }
    }

    async public Task<string> PostJsonAsync(string url,
                                            string json,
                                            RequestAuthorization? authorization = null,
                                            int timeOutSeconds = 20)
    {
        url = PrepareUrl(url);

        using (var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        })
        {
            request.AddAuthentication(authorization);

            using (var cts = new CancellationTokenSource(timeOutSeconds * 1000))
            {
                HttpResponseMessage? responseMessage = null;

                try
                {
                    var client = Create(url);
                    responseMessage = await client.SendAsync(request, cts.Token);

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        var response = await responseMessage.Content.ReadAsStringAsync();

                        return response;
                    }
                    else
                    {
                        throw new HttpServiceException(responseMessage.StatusCode);
                    }
                }
                //catch /*(TaskCanceledException ex)*/
                //{
                //    //if (ex.CancellationToken == cts.Token)
                //    {
                //        throw new System.Exception("The http operation is canceled (timed out)!");
                //    }
                //}
                finally
                {
                    if (responseMessage != null)
                    {
                        responseMessage.Dispose();
                    }
                }
            }
        }
    }

    async public Task<string> PostXmlAsync(string url,
                                           string xml,
                                           RequestAuthorization? authorization = null,
                                           int timeOutSeconds = 20,
                                           Encoding? encoding = null)
    {
        var xmlData = await PostDataAsync(url, (encoding ?? Encoding.UTF8).GetBytes(xml), authorization, timeOutSeconds);

        return EncodeBytes(xmlData.data, encoding);
    }

    async public Task<string> PostValues(string url,
                                         IEnumerable<KeyValuePair<string, string>> values,
                                         RequestAuthorization? authorization = null,
                                         int timeOutSeconds = 20,
                                         Encoding? encoding = null)
    {
        url = PrepareUrl(url);

        using (var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(values)
        })
        {
            request.AddAuthentication(authorization);

            using (var cts = new CancellationTokenSource(timeOutSeconds * 1000))
            {
                HttpResponseMessage? responseMessage = null;

                try
                {
                    if (authorization?.ClientCerticate == null &&
                        authorization?.UseDefaultCredentials != true)
                    {
                        var client = Create(url);
                        responseMessage = await client.SendAsync(request, cts.Token);
                    }
                    else
                    {
                        using (var clientHandler = new HttpClientHandler())
                        {
                            if (authorization.ClientCerticate != null)
                            {
                                clientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
                                clientHandler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                                clientHandler.ClientCertificates.Add(authorization.ClientCerticate);
                            }
                            else
                            {
#pragma warning disable SYSLIB0039 // allow old protocols (tls, tls11)
                                clientHandler.SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13 | SslProtocols.Tls;
#pragma warning restore SYSLIB0039 // allow old protocols (tls, tls11)
                                clientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                            }
                            clientHandler.UseDefaultCredentials = authorization.UseDefaultCredentials;

                            using (var client = new HttpClient(clientHandler))
                            {
                                responseMessage = await client.SendAsync(request, cts.Token);
                            }
                        }
                    }

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        var bytes = await responseMessage.Content.ReadAsByteArrayAsync();

                        return Encoding.UTF8.GetString(bytes);
                    }
                    else
                    {
                        throw new HttpServiceException(responseMessage.StatusCode);
                    }
                }
                //catch /*(TaskCanceledException ex)*/
                //{
                //    //if (ex.CancellationToken == cts.Token)
                //    {
                //        throw new System.Exception("The http operation is canceled (timed out)!");
                //    }
                //}
                finally
                {
                    if (responseMessage != null)
                    {
                        responseMessage.Dispose();
                    }
                }
            }
        }
    }

    async public Task<bool> UploadFileAsync(string url,
                                         byte[] fileData,
                                         string fileName,
                                         string formFieldName = "file",
                                         RequestAuthorization? authorization = null,
                                         int timeOutSeconds = 20)
    {
        url = PrepareUrl(url);

        using (var content = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(fileData);
            content.Add(fileContent, formFieldName, fileName);

            using (var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            })
            {
                request.AddAuthentication(authorization);

                using (var cts = new CancellationTokenSource(timeOutSeconds * 1000))
                {
                    HttpResponseMessage? responseMessage = null;

                    try
                    {
                        var client = Create(url);
                        responseMessage = await client.SendAsync(request, cts.Token);

                        return responseMessage.IsSuccessStatusCode;
                    }
                    //catch (TaskCanceledException)
                    //{
                    //    return false;
                    //}
                    //catch (Exception)
                    //{
                    //    return false;
                    //}
                    finally
                    {
                        responseMessage?.Dispose();
                    }
                }
            }
        }
    }


    #endregion

    #region Proxy

    public WebProxy? GetProxy(string server)
    {
        if (_options.UseProxy && !IgnorProxy(server))
        {
            return _options.WebProxyInstance;
        }

        return null;
    }

    #endregion

    #region Url

    public string AppendParametersToUrl(string url, string parameters)
    {
        string c = "?";
        if (url.EndsWith("?") || url.EndsWith("&"))
        {
            c = "";
        }
        else if (url.Contains("?"))
        {
            c = "&";
        }

        return $"{url}{c}{parameters}";
    }

    public string ApplyUrlOutputRedirection(string url)
    {
        if (_options?.UrlOutputRedirections == null
            || _options.UrlOutputRedirections.Count == 0
            || String.IsNullOrEmpty(url))
        {
            return url;
        }

        foreach (string from in _options.UrlOutputRedirections.Keys)
        {
            if (String.IsNullOrEmpty(from) ||
                String.IsNullOrEmpty(_options.UrlOutputRedirections[from]))
            {
                continue;
            }

            if (url.IndexOf(from, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                url = url.ReplacePro(from, _options.UrlOutputRedirections[from], StringComparison.OrdinalIgnoreCase);
            }
        }

        // Replace, eg HEADER Placeholders
        url = url.ReplaceUrlPlaceholders(_contextAccessor?.HttpContext?.Request,
                                         (placeholder) => placeholder);

        return url;
    }

    public string ApplyUrlInputRedirections(string url)
    {
        if (_options?.UrlInputRedirections == null
            || _options.UrlInputRedirections.Count == 0
            || String.IsNullOrEmpty(url))
        {
            return url;
        }

        foreach (string from in _options.UrlInputRedirections.Keys)
        {
            if (String.IsNullOrEmpty(from) ||
                String.IsNullOrEmpty(_options.UrlInputRedirections[from]))
            {
                continue;
            }

            if (url.IndexOf(from, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                url = url.ReplacePro(from, _options.UrlInputRedirections[from], StringComparison.OrdinalIgnoreCase);
            }
        }

        return url;
    }

    #region Legacy

    public bool Legacy_AlwaysDownloadFrom(string filename)  // From good old ArcIMS ...
    {
        var alwaysdownloadfrom = _options?.Legacy_AlwaysDownloadFrom;

        if (alwaysdownloadfrom == null)
        {
            return false;
        }

        filename = filename.ToLower();
        foreach (string f in alwaysdownloadfrom)
        {
            if (f == "*")
            {
                return true;
            }

            string pattern = f.ToLower();
            if (Regex.IsMatch(filename, f))
            {
                return true;
            }
        }
        return false;
    }

    #endregion

    #endregion

    private static HttpClient? _httpClient;
    public HttpClient Create(string url)
    {
        if (_httpClientFactory == null)
        {
            _httpClient = _httpClient ?? new HttpClient();
            return _httpClient;
        }

        if (_options.UseProxy && !IgnorProxy(url))
        {
            return _httpClientFactory.CreateClient(_options.DefaultProxyClientName);
        }

        return _httpClientFactory.CreateClient(_options.DefaultClientName);
    }

    #region Helper

    private string PrepareUrl(string url)
    {
        if (url.StartsWith("//"))
        {
            url = $"{(_options.ForceHttps ? "https:" : "http:")}{url}";
        }

        return ApplyUrlInputRedirections(url);
    }

    private bool IgnorProxy(string server)
    {
        if (_options.IgnoreProxyServers == null)
        {
            return false;
        }

        server = server.ToLower();
        if (server.StartsWith("#") || server.StartsWith("$") || server.StartsWith("~") || server.StartsWith("&"))
        {
            server = server.Substring(1, server.Length - 1);
        }

        if (server.StartsWith("http://"))
        {
            server = server.Substring(7, server.Length - 7);
        }

        if (server.StartsWith("https://"))
        {
            server = server.Substring(8, server.Length - 8);
        }

        foreach (string iServer in _options.IgnoreProxyServers)
        {
            string pattern = iServer.ToLower();
            if (Regex.IsMatch(server, pattern))
            {
                return true;
            }

            if (server.StartsWith(pattern))
            {
                return true;
            }
        }
        return false;
    }

    private static string EncodeBytes(byte[] bytes, Encoding? encoding)
    {
        string result;

        if (encoding == null)
        {
            encoding = Encoding.UTF8;
            result = encoding.GetString(bytes).Trim(' ', '\0').Trim();

            #region Xml Encoding

            try
            {
                if (result.StartsWith("<?xml "))
                {
                    int index = result.IndexOf(" encoding=");
                    if (index != -1)
                    {
                        int index2 = result.IndexOf(result[index + 10], index + 11);
                        if (index2 != -1)
                        {

                            string encodingString = result.Substring(index + 11, index2 - index - 11);
                            if (encodingString.ToLower() != "utf-8" && encodingString.ToLower() != "utf8")
                            {
                                encoding = Encoding.GetEncoding(encodingString);
                                if (encoding != null)
                                {
                                    result = encoding.GetString(bytes).Trim(' ', '\0').Trim();
                                }
                                else
                                {
                                    encoding = Encoding.UTF8;
                                }
                            }

                        }
                    }
                }
            }
            catch { }

            #endregion
        }
        else
        {
            result = encoding.GetString(bytes).Trim(' ', '\0').Trim();
        }

        return result;
    }

    #endregion
}
