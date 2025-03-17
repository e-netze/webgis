using E.Standard.WebGIS.Core;

namespace E.Standard.WebGIS.Api.Abstractions;

public interface IUrlHelperService
{
    string PortalUrl(HttpSchema httpSchema = HttpSchema.Default);
    string AppRootUrl(HttpSchema httpSchema = HttpSchema.Default);
    string GetCustomGdiScheme();

    string OutputUrl();
    string OutputPath();
}
