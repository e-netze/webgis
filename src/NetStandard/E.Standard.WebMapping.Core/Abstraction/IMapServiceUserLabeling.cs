#nullable enable

namespace E.Standard.WebMapping.Core.Abstraction;

/// <summary>
/// A map service that can label layers with user-defined labeling
/// (e.g. ArcGIS Server map services with supportsDynamicLayers)
/// </summary>
public interface IMapServiceUserLabeling
{
    /// <summary>
    /// The service is technically able to draw user-defined labels
    /// </summary>
    bool SupportsUserLabeling { get; }
}
