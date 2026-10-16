#nullable enable

using E.Standard.WebMapping.Core.Renderer.UserRenderers;

namespace E.Standard.WebMapping.Core.Abstraction;

/// <summary>
/// A layer that can be drawn with a user-defined renderer
/// </summary>
public interface IUserRenderableLayer
{
    /// <summary>
    /// The layer itself is technically able to be drawn with a user-defined renderer
    /// (feature layer with point, line or polygon geometry)
    /// </summary>
    bool SupportsUserRenderer { get; }

    /// <summary>
    /// The (validated) user-defined renderer for the current request. Null => default rendering
    /// </summary>
    UserRenderer? UserRenderer { get; set; }
}
