using System.Collections.Generic;

namespace E.Standard.WebMapping.GeoServices.Topo;

public interface ITopoShape
{
    IEnumerable<int> Vertices { get; }
}
