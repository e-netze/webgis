using E.Standard.Custom.Core.Abstractions;
using E.Standard.WebGIS.Core;
using Microsoft.AspNetCore.Hosting;

namespace Api.Core.AppCode.Services;

public class AppEnvironment : IAppEnvironment
{
    private readonly IWebHostEnvironment _environment;
    private readonly UrlHelperService _urlHelper;

    public AppEnvironment(IWebHostEnvironment environment,
                          UrlHelperService urlHelper)
    {
        _environment = environment;
        _urlHelper = urlHelper;
    }

    public string ConfigRootPath => _environment.ContentRootPath;

    public string AppEtcPath => _urlHelper.AppEtcPath();

    public string AppRootUrl => _urlHelper.AppRootUrl(HttpSchema.Default);
}
