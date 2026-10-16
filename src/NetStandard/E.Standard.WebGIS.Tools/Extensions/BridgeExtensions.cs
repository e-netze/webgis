using System;
using System.Collections.Generic;

using E.Standard.Json;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;

namespace E.Standard.WebGIS.Tools.Extensions;

static public class BridgeExtensions
{
    static public IEnumerable<VisFilterDefinitionDTO> GetVisFilters(this IBridge bridge)
    {
        string filtersJson = bridge.GetRequestParameter("filters");
        if (String.IsNullOrEmpty(filtersJson))
        {
            return new VisFilterDefinitionDTO[0];
        }

        return JSerializer.Deserialize<IEnumerable<VisFilterDefinitionDTO>>(filtersJson);
    }
}
