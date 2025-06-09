using E.Standard.WebGIS.CMS;

namespace E.Standard.WebMapping.Core.Api.Bridge;

public interface IEditThemeBridge : IApiObjectBridge
{
    string Name { get; }
    string ThemeId { get; }
    string LayerId { get; }
    EditingRights DbRights { get; }
    string[] Tags { get; }
}