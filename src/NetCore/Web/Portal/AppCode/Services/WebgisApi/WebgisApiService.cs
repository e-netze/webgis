using E.Standard.Configuration.Services;
using E.Standard.Extensions.Collections;
using E.Standard.Json;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Models;
using E.Standard.WebGIS.Core.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Portal.Core.AppCode.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Services.WebgisApi;

public class WebgisApiService
{
    private readonly ILogger<WebgisApiService> _logger;
    private readonly ConfigurationService _config;
    private readonly UrlHelperService _urlHelperService;
    private readonly InMemoryPortalAppCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICryptoService _crypto;

    public WebgisApiService(
        ILogger<WebgisApiService> logger,
        ConfigurationService config,
        UrlHelperService urlHelperService,
        InMemoryPortalAppCache cache,
        IHttpClientFactory httpClientFactory,
        ICryptoService crypto)
    {
        _logger = logger;
        _config = config;
        _urlHelperService = urlHelperService;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _crypto = crypto;
    }

    async public Task<string[]> ApiCmsUserRoles(HttpRequest request)
    {
        if (_cache.AllCmsRoles == null)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("default");
                var httpResponseMessage = await httpClient.GetAsync($"{_urlHelperService.ApiInternalUrl(request)}/rest/AllCmsUserRoles?pwd={_crypto.GetCustomPassword((int)CustomPasswords.ApiAdminQueryPassword)}");
                var data = await httpResponseMessage.Content.ReadAsStringAsync();

                if (String.IsNullOrEmpty(data))
                {
                    return Array.Empty<string>();
                }

                _cache.AllCmsRoles = JSerializer.Deserialize<string[]>(data);
            }
            catch
            {
                _cache.AllCmsRoles = null;
                return Array.Empty<string>();
            }
        }

        return _cache.AllCmsRoles;
    }

    async public Task<ApiInfoDTO> GetApiInfo(HttpRequest request)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("default");
            var httpResponseMessage = await httpClient.GetAsync(_urlHelperService.ApiInternalUrl(request) + "/instance/info?f=json");
            var data = await httpResponseMessage.Content.ReadAsStringAsync();

            if (String.IsNullOrEmpty(data))
            {
                return null;
            }

            return JSerializer.Deserialize<ApiInfoDTO>(data);

        }
        catch (Exception /*ex*/)
        {
            return null;
        }
    }

    async public Task<string[]> GetBranches(HttpRequest request)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("default");
            var httpResponseMessage = await httpClient.GetAsync($"{_urlHelperService.ApiInternalUrl(request)}/rest/branches");
            var data = await httpResponseMessage.Content.ReadAsStringAsync();

            if (String.IsNullOrEmpty(data))
            {
                return Array.Empty<string>();
            }

            return JSerializer.Deserialize<string[]>(data);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    #region Security

    async public Task<ApiSecurityInfo> SecurityInfo(HttpRequest request)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("default");
            var httpResponseMessage = await httpClient.GetAsync(_urlHelperService.ApiInternalUrl(request) + "/instance/secinfo?f=json");
            var data = await httpResponseMessage.Content.ReadAsStringAsync();

            if (String.IsNullOrEmpty(data))
            {
                return null;
            }

            return JSerializer.Deserialize<ApiSecurityInfo>(data);

        }
        catch (Exception /*ex*/)
        {
            return null;
        }
    }

    #endregion

    #region Portal Pages

    async public Task<ApiPortalPageDTO> GetApiPortalPageAsync(HttpContext context, string id)
    {
        string portalJsonString = await CallToolMethodAsync(context, "WebGIS.Tools.Portal.Portal", "page",
            new Dictionary<string, string>()
            {
                {"page-id", id}
            });

        ApiPortalPageDTO portal = JSerializer.Deserialize<ApiPortalPageDTO>(portalJsonString);
        if (portal == null || portal.Id != id)
        {
            throw new ArgumentException("Unknown Portal: " + id);
        }

        return portal;
    }

    async public Task<bool> SortPortalItems(HttpContext context, string id, string sortingMethod, string items, string category)
    {
        var parameters = new Dictionary<string, string>()
            {
                {"page-id",id},
                {"sorting-method", _crypto.EncryptTextDefault(sortingMethod, CryptoResultStringType.Hex) },
                {"sorting-items", _crypto.EncryptTextDefault(items, CryptoResultStringType.Hex) }
            };

        if (!String.IsNullOrEmpty(category))
        {
            parameters.Add("category", category);
        }

        string portalJsonString = await CallToolMethodAsync(context, "WebGIS.Tools.Portal.Publish", "set-item-order", parameters);

        return true;
    }

    async public Task<bool> UpdatePortalPageContentAsync(HttpContext context, string id, string contentId, string content, string sorting)
    {
        string portalJsonString = await CallToolMethodAsync(context, "WebGIS.Tools.Portal.Portal", "update-content",
            new Dictionary<string, string>()
            {
                {"page-id",id},
                {"page-content-id", _crypto.EncryptTextDefault(contentId, CryptoResultStringType.Hex) },
                {"page-content",  _crypto.EncryptTextDefault(content, CryptoResultStringType.Hex) },
                {"page-content-sorting", _crypto.EncryptTextDefault(sorting, CryptoResultStringType.Hex) },
            });

        return true;
    }

    async public Task<bool> RemovePortalPageContentAsync(HttpContext context, string id, string contentId)
    {
        string portalJsonString = await CallToolMethodAsync(context, "WebGIS.Tools.Portal.Portal", "remove-content",
            new Dictionary<string, string>()
            {
                {"page-id",id},
                {"page-content-id", _crypto.EncryptTextDefault(contentId, CryptoResultStringType.Hex) },
            });

        return true;
    }

    async public Task<bool> UpdatePortalPageContentSortingAsync(HttpContext context, string id, string sorting)
    {
        string portalJsonString = await CallToolMethodAsync(context, "WebGIS.Tools.Portal.Portal", "update-content-sorting",
            new Dictionary<string, string>()
            {
                {"page-id",id},
                {"page-content-sorting", _crypto.EncryptTextDefault(sorting, CryptoResultStringType.Hex) },
            });

        return true;
    }

    async public Task<string> UploadPortalPageContentImageAsync(HttpContext context, string id, string contentId, byte[] data)
    {
        string portalString = await CallToolMethodAsync(context, "WebGIS.Tools.Portal.Portal", "upload-content-image",
            new Dictionary<string, string>()
            {
                {"page-id",id},
                {"page-content-id", _crypto.EncryptTextDefault(contentId, CryptoResultStringType.Hex) },
                {"page-content-imagedata",_crypto.EncryptTextDefault( Convert.ToBase64String(data), CryptoResultStringType.Hex) }
            });

        return portalString;
    }

    async public Task<ApiPortalPageDTO[]> GetApiPortalPagesAsync(HttpContext context)
    {
        string portalsJsonString = await CallToolMethodAsync(context, "WebGIS.Tools.Portal.Portal", "pages");

        try
        {
            ApiPortalPageDTO[] portals = JSerializer.Deserialize<ApiPortalPageDTO[]>(portalsJsonString);
            return portals;
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Fehler bei GetApiPortalPages, JSON-Deserialize: " + portalsJsonString, ex);
        }
    }

    #endregion

    #region Map/App

    async public Task<string> UploadMapImageAsync(HttpContext context, string id, string category, string map, byte[] data)
    {
        string portalString = await CallToolMethodAsync(context, "WebGIS.Tools.Portal.Publish", "upload-map-image",
            new Dictionary<string, string>()
            {
                {"page-publish-page-id", id},
                {"page-publish-category", category},
                {"map",map},
                {"page-map-imagedata", _crypto.EncryptTextDefault( Convert.ToBase64String(data), CryptoResultStringType.Hex) }
            });

        return portalString;
    }

    async public Task<ApiAppDTO> GetApiPortalAppAsync(HttpContext context, string id, string category, string app)
    {
        string portalJsonString = await CallToolMethodAsync(context, "WebGIS.Tools.Portal.PublishApp", "app-json",
            new Dictionary<string, string>()
            {
                {"page-id",id},
                {"category",category },
                {"app",app }
            });

        return JSerializer.Deserialize<ApiAppDTO>(portalJsonString);
    }

    async public Task<string> GetApiPortalAppDescriptionAsync(HttpContext context, string id, string category, string app)
    {
        string appDescription = await CallToolMethodAsync(context, "WebGIS.Tools.Portal.PublishApp", "app-description",
            new Dictionary<string, string>()
            {
                {"page-id",id},
                {"category",category },
                {"app",app }
            });

        return appDescription;
    }

    async public Task<string> UploadAppImageAsync(HttpContext context, string id, string category, string app, byte[] data)
    {
        string portalString = await CallToolMethodAsync(context, "WebGIS.Tools.Portal.PublishApp", "upload-app-image",
            new Dictionary<string, string>()
            {
                {"page-publish-page-id", id},
                {"page-publish-category", category},
                {"app",app},
                {"page-app-imagedata", _crypto.EncryptTextDefault( Convert.ToBase64String(data), CryptoResultStringType.Hex) }
            });

        return portalString;
    }

    async public Task<string> DeleteAppAsync(HttpContext context, string id, string category, string app)
    {
        string portalString = await CallToolMethodAsync(context, "WebGIS.Tools.Portal.PublishApp", "delete-app",
            new Dictionary<string, string>()
            {
                {"page-id", id},
                {"category", category},
                {"app",app}
            });

        return portalString;
    }

    async public Task<SharedMapMeta> GetApiSharedMapMetadata(HttpContext context, string pageId, string sharedMapName)
    {
        string metaJsonString = await CallToolMethodAsync(context, "WebGIS.Tools.Serialization.ShareMap", "shared-map-meta",
            new Dictionary<string, string>()
            {
                {"page-id", pageId },
                {"name", sharedMapName }
            });

        if (String.IsNullOrWhiteSpace(metaJsonString))
        {
            //throw new Exception("Unknown shared map");
            return null;
        }

        SharedMapMeta sharedMapMeta = JSerializer.Deserialize<SharedMapMeta>(metaJsonString);
        return sharedMapMeta;
    }

    async public Task<string> GetMapHtmlMetaTags(HttpContext context, string id, string category, string map)
    {
        try
        {
            string htmlTags = await CallToolMethodAsync(context, "WebGIS.Tools.Portal.Publish", "get-map-html-meta-tags",
                new Dictionary<string, string>()
                {
                {"page-id", id},
                {"category", category},
                {"map",map}
                });

            return htmlTags;
        }
        catch { return String.Empty; }
    }

    #endregion

    #region Project 

    async public Task<ProjectionServiceResultDTO> Project(HttpContext context, ProjectionServiceArgumentDTO argument)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("default");

            var httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.RequestUri = new Uri($"{_urlHelperService.ApiInternalUrl(context.Request)}/rest/project?f=json");
            httpRequestMessage.Method = HttpMethod.Post;

            var nvc = new Dictionary<string, string>();
            nvc.Add("proj_arg", JSerializer.Serialize(argument));
            httpRequestMessage.Content = new FormUrlEncodedContent(nvc);

            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
            var data = await httpResponseMessage.Content.ReadAsStringAsync();

            return JSerializer.Deserialize<ProjectionServiceResultDTO>(data);
        }
        catch (Exception /*ex*/)
        {
            return null;
        }
    }

    #endregion

    #region Proxy

    async public Task<string> CallToolMethodAsync(HttpContext context, string id, string method, Dictionary<string, string> methodParameters = null)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("CallToolMethodAsync: tool={id} method={method} data={data}",
                id, method,
                methodParameters == null
                    ? "NULL"
                    : JSerializer.Serialize(methodParameters));
        }

        var result = await CallToolMethodBytesAsync(context, id, method, methodParameters);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("CallToolMethodAsync Result: {result}",
                result.data == null
                ? "NULL"
                : System.Text.Encoding.UTF8.GetString(result.data));
        }

        return result.data == null
            ? null
            : System.Text.Encoding.UTF8.GetString(result.data);
    }

    async public Task<(string data, string contentType)> CallToolMethod2Async(HttpContext context, string id, string method, Dictionary<string, string> methodParameters = null)
    {
        var result = await CallToolMethodBytesAsync(context, id, method, methodParameters);
        if (result.data == null)
        {
            return (null, null);
        }

        return (System.Text.Encoding.UTF8.GetString(result.data), result.contentType);
    }

    async public Task<(byte[] data, string contentType)> CallToolMethodBytesAsync(HttpContext context, string id, string method, Dictionary<string, string> methodParameters = null)
    {
        string contentType = "";

        var request = context.Request;
        //WebAuthitification auth = new WebAuthitification(request, appendPrefixes: true);

        //string portalId = String.Empty;
        //if (methodParameters != null)
        //{
        //    if (methodParameters.ContainsKey("portal-id"))
        //    {
        //        portalId = methodParameters["portal-id"];
        //    }
        //    else if (methodParameters.ContainsKey("portal"))
        //    {
        //        portalId = methodParameters["portal"];
        //    }
        //    else if (methodParameters.ContainsKey("page-id"))
        //    {
        //        portalId = methodParameters["page-id"];
        //    }
        //    else if (methodParameters.ContainsKey("page"))
        //    {
        //        portalId = methodParameters["page"];
        //    }
        //}

        var currentPortalUser = context.User.ToPortalUser();
        //var currentPortalUser = CurrentPortalUser(controller, portalId);

        if (currentPortalUser != null)
        {
            string userName = currentPortalUser.UsernameGroupsString(); // auth.UsernameGroupsString();

            userName = _crypto.EncryptText(userName,
                (int)CustomPasswords.PortalProxyRequests,
                CryptoStrength.AES256, true,
                CryptoResultStringType.Hex);

            string url = $"{_urlHelperService.ApiInternalUrl(request)}/rest/toolmethod";
            string parameters = $"toolId={id.ToLower().Replace("-", ".")}&method={method}";// + "&__ui=" + userName + "&" +
            NameValueCollection nvc = new NameValueCollection(
                request.HasFormData() ?
                request.Form.ToNameValueCollection() :
                request.Query.ToNameValueCollection());

            if (request.HasFormData())  // Zusätzlich alle Url Parameter übergeben...
            {
                foreach (string key in request.Query.Keys)
                {
                    if (nvc[key] == null)
                    {
                        nvc[key] = request.Query[key];
                    }
                }
            }

            if (_config.UseFavoriteDetection() && !currentPortalUser.IsAnonymous)
            {
                nvc.Add("__ft", Const.DefaultFavoriteTask);
            }

            nvc.Add("__ui", userName);
            if (methodParameters != null)
            {
                foreach (string key in methodParameters.Keys)
                {
                    if (nvc.AllKeys.Contains(key))
                    {
                        nvc[key] = methodParameters[key];
                    }
                    else
                    {
                        nvc.Add(key, methodParameters[key]);
                    }
                }
            }

            var httpClient = _httpClientFactory.CreateClient("default");

            var httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.RequestUri = new Uri($"{url}?{parameters}");
            httpRequestMessage.Method = HttpMethod.Post;

            try
            {
                httpRequestMessage.Content = new FormUrlEncodedContent(nvc.ToKeyValuePairs());
            }
            catch  // kann "invlid URI: tool long" werfen, wenn Paramter zu lange werden (beim Upload von Bildern...) Dann sollte probiert werden, den Content 'manuell' zu encoden
            {
                var encodedItems = nvc.ToKeyValuePairs().Select(i => WebUtility.UrlEncode(i.Key) + "=" + WebUtility.UrlEncode(i.Value));
                var encodedContent = new StringContent(String.Join("&", encodedItems), null, "application/x-www-form-urlencoded");

                httpRequestMessage.Content = encodedContent;
            }


            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

            var data = await httpResponseMessage.Content.ReadAsByteArrayAsync();
            contentType = httpResponseMessage.Content.Headers.ContentType?.ToString();

            return (data, contentType);
        }


        return (null, null);
    }

    #endregion
}
