using E.Standard.WebMapping.Core.Collections;
using System.Linq;

namespace E.Standard.WebMapping.Core.Extensions;

public static class FeatureCollectionExtensions
{
    static public bool ContainsFeatureWithOid(this FeatureCollection features, int oid)
        => features?.Where(f => f.Oid == oid).Any() == true;

    static public bool NotContainsFeatureWithOid(this FeatureCollection features, int oid)
        => features?.ContainsFeatureWithOid(oid) == false;
}
