using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Services;
using E.Standard.Web.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Api.Core.Controllers;

public class OutputController : ApiBaseController
{
    private readonly ILogger<OutputController> _logger;
    private readonly UrlHelperService _urlHelper;

    public OutputController(ILogger<OutputController> logger,
                            UrlHelperService urlHelper,
                            IHttpService http)
        : base(logger, urlHelper, http, null)
    {
        _logger = logger;
        _urlHelper = urlHelper;
    }

    async public Task<IActionResult> Index(string id)
    {
        if (String.IsNullOrEmpty(_urlHelper.OutputPath()))
        {
            return null;
        }

        if (id.Contains("/") ||
           id.Contains("..") ||
           id.Contains("\\"))
        {
            return StatusCode(400);
        }

        var fileInfo = new FileInfo($"{_urlHelper.OutputPath()}/{id}");
        string contentType;
        switch (fileInfo.Extension.ToLower())
        {
            case ".png":
                contentType = "image/png";
                break;
            case ".jpg":
            case ".jpeg":
                contentType = "image/jpeg";
                break;
            case ".pdf":
                contentType = "image/bmp";
                break;
            case ".csv":
                contentType = "text/csv";
                break;
            case ".txt":
                contentType = "text/plain";
                break;
            case ".json":
                contentType = "application/json";
                break;
            case ".zip":
                contentType = "application/zip";
                break;
            default:
                return StatusCode(403);
        }

        if (!fileInfo.Exists)
        {
            return StatusCode(404);
        }

        return base.RawResponse(await System.IO.File.ReadAllBytesAsync(fileInfo.FullName), contentType, null);
    }
}
