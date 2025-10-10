using E.Standard.WebGIS.Tools.Presentation.Models;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Api.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Presentation.Extensions;

static internal class TocVisFilterRequestDTOExtensions
{
    static public string TryGetWhereClause(
            this Models.TocVisFilterRequestDTO dto, 
            IEnumerable<VisFilterDefinitionDTO> requestVisFilters)
    {
        var filterIds = dto.ServiceLayers?
            .SelectMany(kvp => kvp.Value.Select(layer => VisFilterDefinitionDTO.CreateTocFilterId(kvp.Key, layer)))
            .ToList();

        if (!filterIds.Any())
        {
            return "";
        }

        foreach (var filterId in filterIds)
        {
            var visFilter = requestVisFilters.FirstOrDefault(f => f.Id == filterId);
            if (visFilter?.IsTocVisFilter() != true) continue;

            string whereClause = visFilter.TocVisFilterWhereClause();
            if(!string.IsNullOrEmpty(whereClause))
            {
                return whereClause;
            }
        }

        return "";
    }
}
