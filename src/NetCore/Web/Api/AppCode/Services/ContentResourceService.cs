using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace Api.Core.AppCode.Services;

public class ContentResourceService
{
    private readonly UrlHelperService _urlHelper;

    public ContentResourceService(UrlHelperService urlHelper)
    {
        _urlHelper = urlHelper;
    }

    public byte[] GetImageBytes(HttpRequest request, string id, string sub)
    {
        byte[] resourceBytes = null;

        id = id?.Replace("~", ".");
        string path = $"{_urlHelper.WWWRootPath()}/content/api/img/";

        FileInfo fi = null;

        string colorScheme = request.Query["colorscheme"];
        if (!String.IsNullOrEmpty(colorScheme) && !colorScheme.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            fi = new FileInfo($"{path}/_company/{request.Query["colorscheme"]}/{sub}/{id}");
        }

        if (fi == null || !fi.Exists)
        {
            fi = new FileInfo($"{path}/_company/{request.Query["company"]}/{sub}/{id}");
        }
        if (!fi.Exists)
        {
            fi = new FileInfo($"{path}/_company/_legacy/{sub}/{id}");
        }
        if (!fi.Exists)
        {
            fi = new FileInfo($"{path}/{sub}/{id}");
        }
        if (!fi.Exists)
        {
            fi = new FileInfo($"{path}/{id}");
        }

        if (fi.Exists)
        {
            resourceBytes = System.IO.File.ReadAllBytes(fi.FullName);
        }
        return resourceBytes;
    }
}
