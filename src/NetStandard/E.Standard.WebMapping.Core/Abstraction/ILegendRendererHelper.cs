using E.Standard.WebMapping.Core.Filters;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface ILegendRendererHelper
{
    LayerRendererType LengendRendererType { get; set; }
    string UniqueValue_Field1 { get; set; }
    string UniqueValue_Field2 { get; set; }
    string UniqueValue_Field3 { get; set; }
    string UniqueValue_FieldDelimiter { get; set; }
    Task<string> FirstLegendValueAsync(QueryFilter filter, IRequestContext requestContext);

    bool SupportsDynamicLegends { get; set; }
}
