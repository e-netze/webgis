using E.Standard.WebMapping.Core.ServiceResponses;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IMapService2 : IMapService
{
    ServiceResponse PreGetMap();

    IEnumerable<ILayerProperties> LayerProperties { get; set; }

    IEnumerable<ServiceTheme> ServiceThemes { get; set; }
}
