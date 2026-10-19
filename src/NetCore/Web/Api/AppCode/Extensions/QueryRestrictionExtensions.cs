#nullable enable

using System;
using System.Linq;

using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.Extensions.Compare;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Api.Models;

namespace Api.Core.AppCode.Extensions;

static public class QueryRestrictionExtensions
{
    /// <summary>
    /// A query result can be restricted with the QueryBuilder (table tools),
    /// if the QueryBuilder is allowed for the service and at least one allowed field exists in the query layer
    /// </summary>
    static public bool IsQueryRestrictable(this CacheService cache, QueryDTO? query, CmsDocument.UserIdentification ui)
    {
        var service = query?.Service;
        if (service == null || String.IsNullOrEmpty(service.Url))
        {
            return false;
        }

        if (!cache.IsServiceQueryBuilderAllowed(service.Url, ui))
        {
            return false;
        }

        var allowedFieldNames = cache.GetAllowedQueryBuilderFields(service.Url, ui)
            .Select(f => f.Name)
            .Where(n => !String.IsNullOrEmpty(n))
            .ToArray();
        if (allowedFieldNames.Length == 0)
        {
            return false;
        }

        var layerFieldNames = service.Layers?.FindByLayerId(query!.LayerId)?.Fields?
            .Where(f => f.Type != FieldType.Shape)
            .Select(f => f.Name)
            .ToArray() ?? [];

        return allowedFieldNames.Any(n => n == "*")
            ? layerFieldNames.Length > 0
            : allowedFieldNames.Any(n => layerFieldNames.Any(l => l.Equals(n, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Adds the (encrypted) restriction descriptor to the result metadata, if the query is restrictable
    /// </summary>
    static public FeaturesDTO TrySetQueryRestriction(this FeaturesDTO features,
                                                     QueryRestrictionDescriptor? descriptor,
                                                     QueryDTO? query,
                                                     CacheService cache,
                                                     ICryptoService crypto,
                                                     CmsDocument.UserIdentification ui)
    {
        try
        {
            if (features?.metadata == null || descriptor == null || query?.Service == null)
            {
                return features!;
            }

            if (!cache.IsQueryRestrictable(query, ui))
            {
                return features;
            }

            descriptor.ServiceId = descriptor.ServiceId.OrTake(query.Service.Url);
            descriptor.QueryId = descriptor.QueryId.OrTake(query.id);

            features.metadata.QueryRestriction = new FeaturesDTO.QueryRestrictionMeta()
            {
                Token = descriptor.ToToken(crypto),
                Where = String.IsNullOrWhiteSpace(descriptor.Restriction) ? null : descriptor.Restriction
            };
        }
        catch { /* restriction is optional => never break the query result */ }

        return features;
    }
}
