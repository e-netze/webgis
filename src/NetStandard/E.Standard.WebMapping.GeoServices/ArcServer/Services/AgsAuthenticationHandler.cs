#nullable enable

using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.ThreadsafeClasses;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Exceptions;
using E.Standard.Web.Models;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Exceptions;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Services;

internal class AgsAuthenticationHandler
{
    private readonly ILogger<AgsAuthenticationHandler> _logger;
    private readonly AgsTokenStore _tokenStore;
    private readonly IHttpService _httpService;

    public AgsAuthenticationHandler(
            ILogger<AgsAuthenticationHandler> logger,
            AgsTokenStore tokenStore,
            IHttpService httpService
        )
    {
        _logger = logger;
        _tokenStore = tokenStore;
        _httpService = httpService;
    }

    async internal Task<string> TryGetAsync(
                IMapServiceAuthentication mapService,
                string requestUrl)
    {
        int i = 0;

        while (true)
        {
            try
            {
                var token = _tokenStore.GetToken(mapService);

                if (!String.IsNullOrWhiteSpace(token))
                {
                    requestUrl += $"{(requestUrl.Contains("?") ? "&" : "?")}token={token}";
                }
                string ret = String.Empty;
                try
                {
                    ret = await _httpService.GetStringAsync(requestUrl,
                        new RequestAuthorization() { Credentials = mapService.HttpCredentials },
                        timeOutSeconds: mapService.Timeout.ToTimeoutSecondOrDefault());
                }
                catch (WebException ex)
                {
                    ex.ThrowIfTokenRequired();

                    throw;
                }
                catch (HttpServiceException httpEx)
                {
                    httpEx.ThrowIfTokenRequired();

                    throw;
                }
                if (ret.Contains("\"error\":"))
                {
                    JsonError? error = JSerializer.Deserialize<JsonError>(ret);
                    if (error?.error == null)
                    {
                        throw new Exception("Unknown error");
                    }
                    if (error.error.code == 499 || error.error.code == 498 || error.error.code == 403) // Token Required (499), Invalid Token (498), No user Persmissions (403)
                    {
                        throw new TokenRequiredException();
                    }
                    throw new Exception("Error:" + error.error.code + "\n" + error.error.message);
                }
                return ret;
            }
            catch (TokenRequiredException ex)
            {
                await HandleTokenExceptionAsync(mapService, i, ex);
            }
            i++;
        }
    }


    async internal Task<byte[]> TryGetRawAsync(
                IMapServiceAuthentication mapService,
                string requestUrl)
    {
        int i = 0;

        while (true)
        {
            try
            {
                var token = _tokenStore.GetToken(mapService);

                if (!String.IsNullOrWhiteSpace(token))
                {
                    requestUrl += $"{(requestUrl.Contains("?") ? "&" : "?")}token={token}";
                }
                byte[] data;
                try
                {
                    data = await _httpService.GetDataAsync(requestUrl,
                        new RequestAuthorization() { Credentials = mapService.HttpCredentials },
                        timeOutSeconds: mapService.Timeout.ToTimeoutSecondOrDefault());
                }
                catch (WebException ex)
                {
                    ex.ThrowIfTokenRequired();

                    throw;
                }
                catch (HttpServiceException httpEx)
                {
                    httpEx.ThrowIfTokenRequired();

                    throw;
                }

                data.ThrowIfTokenRequiredOrError();

                return data;
            }
            catch (TokenRequiredException ex)
            {
                await HandleTokenExceptionAsync(mapService, i, ex);
            }
            i++;
        }
    }

    async internal Task<string> TryPostAsync(
                IMapServiceAuthentication mapService,
                string requestUrl,
                string postBodyData)
    {
        int i = 0;

        while (true)
        {
            try
            {
                var token = _tokenStore.GetToken(mapService);
                string? reverseProxyToken = null;

                var tokenParameter = String.Empty;

                if (!String.IsNullOrWhiteSpace(token))
                {
                    tokenParameter = $"{(String.IsNullOrWhiteSpace(postBodyData) ? "" : "&")}token={token}";
                }
                else if (!String.IsNullOrWhiteSpace(reverseProxyToken = _tokenStore.GetToken(mapService)))
                {
                    reverseProxyToken = reverseProxyToken.Contains("=") ? reverseProxyToken : $"token={reverseProxyToken}";
                    requestUrl += $"{(requestUrl.Contains("?") ? "&" : "?")}{reverseProxyToken}";
                }

                string ret = String.Empty;
                try
                {
                    ret = await _httpService.PostFormUrlEncodedStringAsync(requestUrl,
                                                                          $"{postBodyData}{tokenParameter}",
                                                                          new RequestAuthorization() { Credentials = mapService.HttpCredentials },
                                                                          timeOutSeconds: mapService.Timeout.ToTimeoutSecondOrDefault());
                }
                catch (WebException ex)
                {
                    _logger.LogWarning("WebException: {message}", ex.Message);

                    ex.ThrowIfTokenRequired();

                    throw;
                }
                catch (HttpServiceException httpEx)
                {
                    _logger.LogWarning("HttpServiceException: {message}", httpEx.Message);

                    httpEx.ThrowIfTokenRequired();

                    throw;
                }
                ret.ThrowIfTokenRequiredOrError();

                return ret;
            }
            catch (TokenRequiredException ex)
            {
                await HandleTokenExceptionAsync(mapService, i, ex);
            }

            i++;
        }
    }

    async internal Task<byte[]> TryPostRawAsync(
                IMapServiceAuthentication mapService,
                string requestUrl,
                string postBodyData)
    {
        int i = 0;

        while (true)
        {
            try
            {
                var token = _tokenStore.GetToken(mapService);

                var tokenParameter = String.Empty;

                if (!String.IsNullOrWhiteSpace(token))
                {
                    tokenParameter = $"{(String.IsNullOrWhiteSpace(postBodyData) ? "" : "&")}token={token}";
                }
                else if (!String.IsNullOrWhiteSpace(mapService.StaticToken))
                {
                    var staticToken = mapService.StaticToken.Contains("=")
                        ? mapService.StaticToken
                        : $"token={mapService.StaticToken}";
                    requestUrl += $"{(requestUrl.Contains("?") ? "&" : "?")}{staticToken}";
                }

                (byte[] data, string contentType) result;
                try
                {
                    result = await _httpService.PostFormUrlEncodedAsync(requestUrl,
                        Encoding.UTF8.GetBytes($"{postBodyData}{tokenParameter}"),
                        new RequestAuthorization() { Credentials = mapService.HttpCredentials },
                        timeOutSeconds: mapService.Timeout.ToTimeoutSecondOrDefault());
                }
                catch (WebException ex)
                {
                    ex.ThrowIfTokenRequired();

                    throw;
                }
                catch (HttpServiceException httpEx)
                {
                    httpEx.ThrowIfTokenRequired();

                    throw;
                }

                if (result.contentType?.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var responseString = Encoding.UTF8.GetString(result.data);
                    responseString.ThrowIfTokenRequiredOrError();
                }

                return result.data;
            }
            catch (TokenRequiredException ex)
            {
                await HandleTokenExceptionAsync(mapService, i, ex);
            }
            i++;
        }
    }


    async private Task HandleTokenExceptionAsync(
                IMapServiceAuthentication mapService,
                int i,
                TokenRequiredException ex)
    {
        if (i < 3)  // try it three times
        {
            await RefreshTokenAsync(mapService);
        }
        else
        {
            throw ex;
        }
    }

    async private Task RefreshTokenAsync(
                IMapServiceAuthentication mapService
        )
    {
        bool tokenRequired = false;

        string serviceKey = _tokenStore.ServiceKey(mapService);
        string currentParameter = _tokenStore.GetToken(serviceKey);

        using (var mutex = await FuzzyMutexAsync.LockAsync(serviceKey))
        {
            if (_tokenStore.ContainsKey(serviceKey) &&
                _tokenStore.GetToken(serviceKey) != currentParameter)  // a new token is already requested
            {
                return;
            }

            var serviceUrl = mapService.Service.OrTake(mapService.Server);
            int pos = serviceUrl.IndexOf("/rest/", StringComparison.OrdinalIgnoreCase);
            if (pos < 0)
            {
                _logger.LogWarning("Can't determine generateToken url for this service: {serviceUrl}", serviceUrl);
                throw new Exception("Can't determine generateToken url for this service");
            }

            string tokenServiceUrl = $"{serviceUrl.Substring(0, pos)}/tokens/generateToken";
            string tokenParams = $"request=gettoken&username={mapService.Username}&password={mapService.Password.UrlEncodePassword()}&expiration={mapService.TokenExpiration}&f=json";
            
            string tokenResponse = String.Empty;

            while (true)
            {
                try
                {
                    _logger.LogInformation("Requesting AGS Token from {tokenService} for user {username}...", tokenServiceUrl, mapService.Username);

                    tokenResponse = await _httpService.PostFormUrlEncodedStringAsync(tokenServiceUrl, tokenParams);

                    break;
                }
                catch (WebException we)
                {
                    if (we.Message.Contains("(502)") && tokenServiceUrl.StartsWith("http://"))
                    {
                        tokenServiceUrl = $"https:{tokenServiceUrl.Substring(5)}";
                        continue;
                    }
                    throw;
                }
            }
            if (tokenResponse.Contains("\"error\":"))
            {
                JsonError? error = JSerializer.Deserialize<JsonError>(tokenResponse);

                throw new Exception($"GetToken-Error:{error?.error?.code}\n{error?.error?.message}\n{error?.error?.details?.ToString()}");
            }
            else
            {
                JsonSecurityToken? jsonToken = JSerializer.Deserialize<JsonSecurityToken>(tokenResponse);
                if (jsonToken?.token != null)
                {
                    _logger.LogInformation("Requesting AGS Token from {tokenService} for user {username} succeeded", tokenServiceUrl, mapService.Username);

                    var token = jsonToken.token;

                    _tokenStore.SetToken(mapService, jsonToken.token);
                }

                tokenRequired = true;
            }
        }

        if (!String.IsNullOrEmpty(mapService.Username) && tokenRequired == false)
        {
            mapService.HttpCredentials = new NetworkCredential(mapService.Username, mapService.Password);
        }
        else
        {
            mapService.HttpCredentials = System.Net.CredentialCache.DefaultNetworkCredentials;
        }
    }
}
