using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Web.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace Api.Core.Controllers;

public class CustomContentController : ApiBaseController
{
    private readonly ILogger<CustomContentController> _logger;
    private readonly IHostEnvironment _environment;

    public CustomContentController(ILogger<CustomContentController> logger,
                                   IHostEnvironment environment,
                                   UrlHelperService urlHelper,
                                   IHttpService http,
                                   IEnumerable<ICustomApiService> customServices = null)
            : base(logger, urlHelper, http, customServices)
    {
        _logger = logger;
        _environment = environment;
    }

    async public Task<IActionResult> DefaultCss(string company)
    {
        if (!String.IsNullOrEmpty(company) && Regex.IsMatch(company, "^[a-z0-9-]+$"))
        {
            string path = System.IO.Path.Combine(_environment.ContentRootPath, "wwwroot", "content", "styles", company, "default.css");

            if (System.IO.File.Exists(path))
            {
                return new FileContentResult(await System.IO.File.ReadAllBytesAsync(path), "text/css");
            }
        }
        return new FileContentResult(Array.Empty<byte>(), "text/css");
    }
}
