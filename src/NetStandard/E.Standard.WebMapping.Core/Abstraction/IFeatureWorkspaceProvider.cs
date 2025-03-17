using E.Standard.WebMapping.Core.Editing;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IFeatureWorkspaceProvider
{
    IFeatureWorkspace GetFeatureWorkspace(string layerId);
}
