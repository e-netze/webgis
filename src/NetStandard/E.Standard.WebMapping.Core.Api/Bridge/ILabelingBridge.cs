using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.Bridge;

public interface ILabelingBridge : IApiObjectBridge
{
    string Id { get; }
    string Name { get; }
    Dictionary<string, string> FieldAliases { get; }
}
