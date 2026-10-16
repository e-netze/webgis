#nullable enable

namespace E.Standard.WebMapping.Core.Abstraction;

/// <summary>
/// A map service that can render layers with user-defined renderers
/// (e.g. ArcGIS Server map services with supportsDynamicLayers)
/// </summary>
public interface IMapServiceUserRenderers
{
    /// <summary>
    /// The service is technically able to draw user-defined renderers
    /// </summary>
    bool SupportsUserRenderers { get; }
}
