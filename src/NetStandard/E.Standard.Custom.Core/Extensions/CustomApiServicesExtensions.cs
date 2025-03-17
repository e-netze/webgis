using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Models;
using E.Standard.WebGIS.Core.Models.Abstraction;
using E.Standard.WebGIS.SubscriberDatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.Custom.Core.Extensions;

static public class CustomApiServicesExtensions
{
    public static void InitGlobals(this IEnumerable<ICustomApiService> customApiServices, ConfigurationService configuration)
    {
        if (customApiServices != null && configuration != null)
        {
            foreach (var customApiService in customApiServices)
            {
                customApiService.InitGlobals(configuration);
            }
        }
    }

    #region Handle Results and Actions

    async static public Task HandleApiResultObject(this IEnumerable<ICustomApiService> customApiServices, IWatchable watchable, byte[] data, string username)
    {
        if (customApiServices != null && watchable != null)
        {
            foreach (var customApiService in customApiServices)
            {
                await customApiService.HandleApiResultObject(watchable, data, username);
            }
        }
    }

    async static public Task HandleApiResultObject(this IEnumerable<ICustomApiService> customApiServices, IWatchable watchable, string json, string username)
    {
        if (customApiServices != null && watchable != null)
        {
            foreach (var customApiService in customApiServices)
            {
                await customApiService.HandleApiResultObject(watchable, json, username);
            }
        }
    }

    async static public Task HandleApiResultObject(this IEnumerable<ICustomApiService> customApiServices, IWatchable watchable, int contentLength, string typeName, string username)
    {
        if (customApiServices != null && watchable != null)
        {
            foreach (var customApiService in customApiServices)
            {
                await customApiService.HandleApiResultObject(watchable, contentLength, typeName, username);
            }
        }
    }

    async static public Task HandleApiClientAction(this IEnumerable<ICustomApiService> customApiServices, string clientId, string action, string username)
    {
        if (customApiServices != null)
        {
            foreach (var customApiService in customApiServices)
            {
                await customApiService.HandleApiClientAction(clientId, action, username);
            }
        }
    }

    async static public Task LogToolRequest(this IEnumerable<ICustomApiService> customServices, string id, string category, string map, string toolId, string username)
    {
        if (customServices != null)
        {
            foreach (var customService in customServices)
            {
                await customService.LogToolRequest(id, category, map, toolId, username);
            }
        }
    }

    #endregion

    #region CMS

    static public string GetCustomCmsDocumentPath(this IEnumerable<ICustomApiCustomCmsService> customApiServices, string cmsId)
    {
        return customApiServices?.Select(c => c.GetCustomCmsDocumentPath(cmsId))
                                 .Where(path => !String.IsNullOrEmpty(path))
                                 .FirstOrDefault();
    }

    static public string GetCustomCmsAccountName(this IEnumerable<ICustomApiCustomCmsService> customApiServices, string cmsId)
    {
        return customApiServices?.Select(c => c.GetCustomCmsAccountName(cmsId))
                                 .Where(path => !String.IsNullOrEmpty(path))
                                 .FirstOrDefault() ?? cmsId;
    }

    #endregion

    #region Subscriber

    public static CustomSubscriberClientname GetCustomClientname(this IEnumerable<ICustomApiSubscriberClientnameService> customServices, SubscriberDb.Client client)
    {
        return customServices?.Select(c => c.GetCustomClientname(client))
                              .Where(c => c != null)
                              .FirstOrDefault();
    }

    #endregion

    #region Cache

    static public void ClearCache(this IEnumerable<ICustomCacheService> customCacheServices, string cmsName = "")
    {
        if (customCacheServices != null)
        {
            foreach (var customCacheService in customCacheServices)
            {
                customCacheService.CacheClear(cmsName);
            }
        }
    }

    #endregion

    #region Serarch Services

    static public Dictionary<string, string> CustomSearchServices(this IEnumerable<ICustomApiService> customServices)
    {
        var dict = new Dictionary<string, string>();

        if (customServices != null)
        {
            foreach (var customService in customServices.Where(c => c.CustomSearchServices() != null))
            {
                foreach (var key in customService.CustomSearchServices().Keys)
                {
                    dict[key] = customService.CustomSearchServices()[key];
                }
            }
        }

        return dict;
    }

    #endregion
}
