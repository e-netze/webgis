using System.Collections.Generic;

using E.Standard.Custom.Core.Models;

namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomRouteService
{
    IEnumerable<CustomRoute> Routes { get; }
}
