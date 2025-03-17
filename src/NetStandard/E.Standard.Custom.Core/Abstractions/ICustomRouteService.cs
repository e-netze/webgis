using E.Standard.Custom.Core.Models;
using System.Collections.Generic;

namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomRouteService
{
    IEnumerable<CustomRoute> Routes { get; }
}
