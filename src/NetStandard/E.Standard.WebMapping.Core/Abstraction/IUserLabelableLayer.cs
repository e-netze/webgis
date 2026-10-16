#nullable enable

using E.Standard.WebMapping.Core.Renderer.UserLabels;

namespace E.Standard.WebMapping.Core.Abstraction;

/// <summary>
/// A layer that can be labeled with a user-defined labeling
/// </summary>
public interface IUserLabelableLayer
{
    /// <summary>
    /// The layer itself is technically able to be labeled with a user-defined labeling
    /// (feature layer with point, line or polygon geometry)
    /// </summary>
    bool SupportsUserLabeling { get; }

    /// <summary>
    /// The (validated) user-defined labeling for the current request. Null => default labeling of the service
    /// </summary>
    UserLabeling? UserLabeling { get; set; }
}
