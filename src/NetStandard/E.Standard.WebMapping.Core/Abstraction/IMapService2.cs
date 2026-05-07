using System.Collections.Generic;

using E.Standard.WebMapping.Core.ServiceResponses;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IMapService2 : IMapService
{
    ServiceResponse PreGetMap();

    IEnumerable<ILayerProperties> LayerProperties { get; set; }

    IEnumerable<ServiceTheme> ServiceThemes { get; set; }
}
