using E.Standard.Custom.Core.Abstractions;
using Microsoft.AspNetCore.Hosting;
using System;

namespace Portal.Core.AppCode.Services;

public class AppEnvironment : IAppEnvironment
{
    private readonly IWebHostEnvironment _environment;
    private readonly UrlHelperService _urlHelper;

    public AppEnvironment(IWebHostEnvironment environment,
                          UrlHelperService urlHeper)
    {
        _environment = environment;
        _urlHelper = urlHeper;
    }

    public string ConfigRootPath => _environment.ContentRootPath;

    public string AppEtcPath => throw new NotImplementedException();

    public string AppRootUrl => throw new NotImplementedException();
}
