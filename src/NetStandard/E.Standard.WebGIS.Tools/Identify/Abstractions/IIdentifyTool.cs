using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Geometry;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Identify.Abstractions;

public interface IIdentifyTool
{
    Task<IEnumerable<CanIdentifyResult>> CanIdentifyAsync(IBridge brige, Point point, double scale, string[] availableServiceIds = null, string[] availableQueryIds = null);
}

public class CanIdentifyResult
{
    public string Name { get; set; }
    public string ToolParameters { get; set; }
    public int Count { get; set; }
}
