using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface ILayerFieldDomains
{
    IEnumerable<KeyValuePair<string, string>> CodedValues(string fieldName);
}
