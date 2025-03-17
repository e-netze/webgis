using Portal.Core.Models.Map;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Services;

public class ViewerLayoutService
{
    private readonly UrlHelperService _urlHelper;

    public ViewerLayoutService(UrlHelperService urlHelper)
    {
        _urlHelper = urlHelper;
    }

    async public Task<string> GetLayoutAsync(string id, int width, int height, string templateId)
    {
        var dirInfo = new DirectoryInfo($"{_urlHelper.AppRootPath()}/viewerlayouts/{id}");
        if (!dirInfo.Exists)
        {
            dirInfo = new DirectoryInfo($"{_urlHelper.AppRootPath()}/viewerlayouts/__default");
        }

        if (!dirInfo.Exists)
        {
            return String.Empty;
        }

        var viewerLayouts = await GetViewerLayoutsAsync(dirInfo);

        var viewerLayout = viewerLayouts?
            .Where(l => l.Width <= width)
            .OrderByDescending(l => l.Width)
            .FirstOrDefault();

        if (viewerLayout?.Templates == null)
        {
            return String.Empty;
        }

        var template =
            viewerLayout.Templates
                .Where(t => t.Id == templateId)
                .FirstOrDefault() ??
            viewerLayout.Templates
                .FirstOrDefault();

        if (String.IsNullOrEmpty(template?.File))
        {
            return String.Empty;
        }

        var templateFileInfo = new FileInfo(Path.Combine(dirInfo.FullName, template.File));
        if (!templateFileInfo.Exists)
        {
            return String.Empty;
        }

        return await File.ReadAllTextAsync(templateFileInfo.FullName);
    }

    async public Task<ViewerLayoutModel> GetLayoutTemplatesAsync(string id, int width)
    {
        var dirInfo = new DirectoryInfo($"{_urlHelper.AppRootPath()}/viewerlayouts/{id}");
        if (!dirInfo.Exists)
        {
            dirInfo = new DirectoryInfo($"{_urlHelper.AppRootPath()}/viewerlayouts/__default");
        }

        if (!dirInfo.Exists)
        {
            return null;
        }

        var viewerLayouts = await GetViewerLayoutsAsync(dirInfo);

        var viewerLayout = viewerLayouts?
            .Where(l => l.Width <= width)
            .OrderByDescending(l => l.Width)
            .FirstOrDefault();

        if (viewerLayout?.Templates == null)
        {
            return null;
        }

        return viewerLayout;
    }

    async private Task<IEnumerable<ViewerLayoutModel>> GetViewerLayoutsAsync(DirectoryInfo dirInfo)
    {
        var layoutJsonFileInfo = new FileInfo(Path.Combine(dirInfo.FullName, "layouts.json"));

        if (layoutJsonFileInfo.Exists)
        {
            return System.Text.Json.JsonSerializer.Deserialize<ViewerLayoutModel[]>(
                await File.ReadAllTextAsync(layoutJsonFileInfo.FullName),
                new System.Text.Json.JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        List<ViewerLayoutModel> viewerLayouts = new List<ViewerLayoutModel>();

        foreach (var layoutFile in dirInfo.GetFiles("*.html"))
        {
            if (layoutFile.Name.StartsWith("w"))
            {
                if (int.TryParse(layoutFile.Name.Substring(1, layoutFile.Name.Length - 1 - layoutFile.Extension.Length), out int width))
                {
                    viewerLayouts.Add(new ViewerLayoutModel()
                    {
                        Width = width,
                        Templates = new[]
                        {
                            new ViewerLayoutModel.Template()
                            {
                                Id = "default",
                                Name = "Default",
                                File = layoutFile.Name
                            }
                        }
                    });
                }
            }
        }

        return viewerLayouts;
    }
}
