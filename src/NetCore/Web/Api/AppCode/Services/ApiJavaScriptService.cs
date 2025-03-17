using E.Standard.WebGIS.Core;
using System;

namespace Api.Core.AppCode.Services;

public class ApiJavaScriptService
{
    private const string CurrentVersionPlaceholder = "{{currentJavascriptVersion}}";
    private readonly UrlHelperService _urlHelper;

    public ApiJavaScriptService(UrlHelperService urlHelper)
    {
        _urlHelper = urlHelper;
    }

    public string GetJsVersion()
    {
        try
        {
            var apiJs = System.IO.File.ReadAllText($"{_urlHelper.WWWRootPath()}/scripts/api/api.min.js");

            if (!String.IsNullOrEmpty(apiJs))
            {

                int pos1 = apiJs.IndexOf("this.api_version=\"");
                int pos2 = pos1 > 0 ? apiJs.IndexOf("\"", pos1 + 18) : -1;

                if (pos1 < 0)
                {
                    pos1 = apiJs.IndexOf("this.api_version='");
                    pos2 = pos1 > 0 ? apiJs.IndexOf("'", pos1 + 18) : -1;
                }

                if (pos1 > 0 && pos2 > 0)
                {
                    var versionString = apiJs.Substring(pos1 + 18, pos2 - pos1 - 18);

                    if (versionString == CurrentVersionPlaceholder)
                    {
                        return $"{WebGISVersion.JsVersion} (Current)";
                    }

                    return new Version(versionString).ToString();
                }
            }
        }
        catch (Exception ex)
        {
            return $"Exception: {ex.Message}";
        }

        return "???";
    }
}
