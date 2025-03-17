using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.Bridge;

public interface IVisFilterBridge : IApiObjectBridge
{
    string Id { get; }
    string Name { get; }
    Dictionary<string, string> Parameters { get; }
    LookupType LookupType(string parameter);
    bool SetLayersVisible { get; }
    string[] LayerNames { get; }
    string LookupLayerName { get; }
}
