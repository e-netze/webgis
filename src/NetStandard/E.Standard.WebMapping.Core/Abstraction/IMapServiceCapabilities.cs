#nullable enable

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IMapServiceCapabilities
{
    MapServiceCapability[]? Capabilities { get; }
}
