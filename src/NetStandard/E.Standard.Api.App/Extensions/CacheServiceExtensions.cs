using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.WebGIS.SubscriberDatabase;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E.Standard.Api.App.Extensions;

public static class CacheServiceExtensions
{
    static public IEnumerable<CmsCacheItem> AllVisibleItems(this IDictionary<string, CmsCacheItem> cmsCacheItems, CacheService cacheService, CmsDocument.UserIdentification ui)
    {
        if (cmsCacheItems == null)
        {
            return new CmsCacheItem[0];
        }

        if (ui == null || ui.HasCmsRoles() == false)
        {
            return cmsCacheItems
                .Values
                .ToArray() // Threadsafe
                .Where(c => c.IsCustom == false);
        }

        var userRolesCmsNames = ui.UserRolesCmsNames();

        List<CmsCacheItem> items = new List<CmsCacheItem>(
            cmsCacheItems
            .Values
            .ToArray()
            .Where(i => i.IsCustom == false || userRolesCmsNames.Contains(i.Name)));

        foreach (var userRolesCmsName in userRolesCmsNames)
        {
            //Console.WriteLine("userCMS-Role: " + userRolesCmsName);
            if (items.Where(cms => cms?.Name == userRolesCmsName).FirstOrDefault() == null)
            {
                var cmsCacheItem = cacheService.LoadCustomCmsCacheItem(userRolesCmsName);
                if (cmsCacheItem != null)
                {
                    items.Add(cmsCacheItem);
                }
            }
        }

        return items;
    }

    static public bool BelongsToBranch(this CmsCacheItem cmsCacheItem, string branch)
        => string.IsNullOrEmpty(branch)
        ? cmsCacheItem.Name.Contains("$") == false
        : cmsCacheItem.Name.EndsWith($"${branch}");

    static public bool HasCmsRoles(this CmsDocument.UserIdentification ui)
    {
        return
            ui.Userroles != null &&
            ui.Userroles.Where(r => r.StartsWith(SubscriberDb.Client.CmsRolePrefix)).Count() > 0;
    }

    static public IEnumerable<string> UserRolesCmsNames(this CmsDocument.UserIdentification ui)
    {
        if (!ui.HasCmsRoles())
        {
            return new string[0];
        }

        return ui
            .Userroles
            .Where(r => r.StartsWith(SubscriberDb.Client.CmsRolePrefix))
            .Select(r => r.Substring(SubscriberDb.Client.CmsRolePrefix.Length));
    }

    static public string LayerVisibilityAuthPropertyId(this string layerId)
    {
        return $"layer-auth::{layerId}";
    }

    static public bool HasInitialzationErrors(this IMapService service)
    {
        return
            (service is IServiceInitialException && ((IServiceInitialException)service).InitialException != null) ||
            (service.HasDiagnosticWarnings());
    }

    static public bool HasDiagnosticWarnings(this IMapService service)
    {
        return service?.Diagnostics != null && service.Diagnostics.State != ServiceDiagnosticState.Ok;
    }

    static public string DiagnosticMessage(this IMapService service)
    {
        return service?.Diagnostics != null ?
            $"Service Initialization Diagnostic: '{service.Diagnostics.State}'\n{service.Diagnostics.Message}"
            : String.Empty;
    }

    static public string ErrorAndDiagnosticMessage(this IMapService service)
    {
        if (service.HasInitialzationErrors() == false)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();

        if (service is IServiceInitialException && ((IServiceInitialException)service).InitialException != null)
        {
            sb.Append($"Service Initialization Diagnostic: {((IServiceInitialException)service).InitialException.ErrorMessage}");
        }

        sb.Append(service.DiagnosticMessage());

        return sb.ToString();
    }
}
