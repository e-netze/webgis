using E.Standard.WebGIS.CMS;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.Bridge;

public interface IQueryFeatureTransferBridge
{
    string Id { get; }

    string Name { get; }

    IEnumerable<IQueryFeatureTransferTargetBridge> Targets { get; }

    IEnumerable<IFieldSetter> FieldSetters { get; }

    FeatureTransferMethod Method { get; }

    bool CopyAttributes { get; }
}
