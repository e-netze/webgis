using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using E.Standard.Extensions.Compare;
using E.Standard.WebGIS.Tools.Presentation.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

namespace E.Standard.WebGIS.Tools.Helpers;

/// <summary>
/// Shared implementation of the QueryBuilder "values" command (suggested values for a field):
/// distinct values (combo) or autocomplete items for the allowed QueryBuilder fields of the given layers
/// </summary>
static internal class QueryBuilderValuesHelper
{
    async static public Task<ApiEventResponse> ValuesResponseAsync(IBridge bridge,
                                                                   ApiToolEventArguments e,
                                                                   IEnumerable<(string ServiceId, string LayerId)> serviceLayers)
    {
        string fieldName = e["field"];
        bool autocomplete = e["mode"] == "autocomplete";
        string term = e["term"] ?? String.Empty;

        int limit = autocomplete
            ? UIQueryBuilder.AutocompleteMaxValues
            : UIQueryBuilder.MaxDistinctComboValues + 1;

        var values = new List<string>();

        if (!String.IsNullOrEmpty(fieldName) && serviceLayers != null)
        {
            foreach (var serviceId in serviceLayers.Select(sl => sl.ServiceId).Distinct())
            {
                var allowedQueryFilterFields = bridge.GetAllowedQueryBuilderFields(serviceId);
                if (!allowedQueryFilterFields?.Any() == true)
                {
                    continue;
                }

                foreach (var layerId in serviceLayers.Where(sl => sl.ServiceId == serviceId).Select(sl => sl.LayerId))
                {
                    var fields = await bridge.GetServiceLayerFields(serviceId, layerId);
                    var field = allowedQueryFilterFields.SelectAllowedFields(fields.OrEmpty())
                                    .FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

                    if (field == null)
                    {
                        continue;
                    }

                    string whereClause = autocomplete && !String.IsNullOrEmpty(term)
                        ? $"UPPER({field.Name}) LIKE '%{term.ToUpperInvariant().Replace("'", "''")}%'"
                        : $"{field.Name} IS NOT NULL";

                    var layerValues = await bridge.QueryLayerDistinctAsync<object>(
                                            serviceId, layerId, field.Name,
                                            whereClause: whereClause,
                                            orderBy: field.Name,
                                            limit: limit);

                    values.AddRange(layerValues
                        .Where(v => v != null && v is not DBNull)
                        .Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))
                        .Where(v => !String.IsNullOrEmpty(v)));
                }
            }
        }

        var distinctValues = values.Distinct().ToArray();
        distinctValues = distinctValues.All(v => double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            ? distinctValues.OrderBy(v => double.Parse(v, NumberStyles.Any, CultureInfo.InvariantCulture)).ToArray()
            : distinctValues.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray();

        if (autocomplete)
        {
            return new ApiRawJsonEventResponse(distinctValues
                .Take(UIQueryBuilder.AutocompleteMaxValues)
                .ToAutocompleteItems()
                .ToArray());
        }

        return new ApiRawJsonEventResponse(new
        {
            values = distinctValues.Take(UIQueryBuilder.MaxDistinctComboValues).ToArray(),
            truncated = distinctValues.Length > UIQueryBuilder.MaxDistinctComboValues
        });
    }
}
