using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebMapping.Core;

public interface ISpatialReferenceStore : IUserData
{
    SpatialReferenceCollection SpatialReferences { get; }
}

public class SpatialReferenceStore : UserData, ISpatialReferenceStore
{
    private readonly SpatialReferenceCollection _sRefs = new SpatialReferenceCollection();

    public SpatialReferenceStore(string etcPath)
    {
        _sRefs.RootPath = $"{etcPath}/coordinates/proj";
    }

    #region ISpatialReferenceStore Member

    public SpatialReferenceCollection SpatialReferences
    {
        get { return _sRefs; }
    }

    #endregion
}
