using E.Standard.Json;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using System;
using System.Collections.Generic;

namespace E.Standard.WebGIS.Tools.Extensions;

static public class BridgeExtensions
{
    static public IEnumerable<LabelingDefinitionDTO> GetLabeling(this IBridge bridge)
    {
        string labelsJson = bridge.GetRequestParameter("labels");
        if (String.IsNullOrEmpty(labelsJson))
        {
            return new LabelingDefinitionDTO[0];
        }

        return JSerializer.Deserialize<IEnumerable<LabelingDefinitionDTO>>(labelsJson);
    }

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
