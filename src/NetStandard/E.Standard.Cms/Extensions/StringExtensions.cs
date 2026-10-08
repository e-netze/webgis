using System;
using System.IO;
using System.Linq;

using E.Standard.Cms.Configuration.Models;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Services;
using E.Standard.CMS.Core;
using E.Standard.Extensions.Security;

namespace E.Standard.Cms.Extensions;

public static class StringExtensions
{
    static public string CommandFileName(this string commandLine)
    {
        commandLine = commandLine.Trim();

        if (commandLine.Contains(" "))
        {
            commandLine = commandLine.Substring(0, commandLine.IndexOf(" "));
        }

        return commandLine;
    }

    static public string? CommandLineArguments(this string commandLine)
    {
        commandLine = commandLine.Trim();

        if (commandLine.Contains(" "))
        {
            return commandLine.Substring(commandLine.IndexOf(" ") + 1).Trim();
        }

        return null;
    }

    static public CmsConfig.CmsItem ToDynamicCmsItem(this string id, CmsConfigurationService ccs, CmsItemTransistantInjectionServicePack servicePack)
    {
        ccs.InitCustomCms(servicePack, id);

        var cmsItem = new CmsConfig.CmsItem()
        {
            Id = id,
            Name = ccs.CMS[id].CmsDisplayName,
            Scheme = ccs.Instance.CustomCms.Scheme,
            Path = ccs.CMS[id].ConnectionString,
            Deployments = new CmsConfig.DeployItem[]
            {
                            new CmsConfig.DeployItem()
                            {
                                Name = ccs.CMS[id].CmsDisplayName,
                                Target = ccs.Instance.CustomCms.RootUrl+"/"+id+"/cms.xml",
                                PostEvents=new CmsConfig.Events()
                                {
                                    HttpGet= new string[]
                                    {
                                        ccs.Instance.CustomCms.RootTemplate
                                    }
                                }
                            }
            }
        };

        if (ccs.Instance?.CustomCms?.HttpPostEvents != null)
        {
            var deployment = cmsItem.Deployments.First();
            deployment.PostEvents = new CmsConfig.Events()
            {
                HttpGet = ccs.Instance.CustomCms.HttpPostEvents
                                .Select(h => h.Replace("{cmsid}", id))
                                .ToArray()
            };
        }

        return cmsItem;
    }

    static public string ToPrintableUrl(this string url, bool isDynamic)
    {
        try
        {
            if (isDynamic)
            {
                var uri = new Uri(url);
                url = uri.PathAndQuery;
            }
        }
        catch { }

        return url;
    }

    static public FileInfo WarningsFileInfo(this string target)
    {
        if (target.IsUrl())
        {
            return new FileInfo(Path.Combine(Environment.CurrentDirectory, $"{target.UrlToHash()}.warnings"));
        }

        return new FileInfo($"{target}.warnings");
    }

    // upload url of a branch deploy: {target}?branch=...&branch_name=...&user=...&commit=...
    static public string AppendBranchUploadParameters(this string uploadUrl, CmsToolContext context)
        => uploadUrl.AppendUrlParameters(
                ("branch", context.Branch),
                ("branch_name", context.BranchName),
                ("user", context.Username),
                ("commit", context.Commit));

    // derive other api cache endpoints from the upload target url: .../cache/upload/{id} => .../cache/{action}/{id}
    static public string ToCacheApiUrl(this string uploadUrl, string action, params (string name, string? value)[] parameters)
    {
        int pos = uploadUrl.LastIndexOf("/upload", StringComparison.OrdinalIgnoreCase);
        if (pos < 0)
        {
            throw new Exception($"Can't derive '{action}' url from upload target {uploadUrl}");
        }

        return $"{uploadUrl.Substring(0, pos)}/{action}{uploadUrl.Substring(pos + "/upload".Length)}"
                    .AppendUrlParameters(parameters);
    }

    static public string AppendUrlParameters(this string url, params (string name, string? value)[] parameters)
    {
        foreach (var (name, value) in parameters.Where(p => !String.IsNullOrEmpty(p.value)))
        {
            url += $"{(url.Contains('?') ? "&" : "?")}{name}={Uri.EscapeDataString(value!)}";
        }

        return url;
    }
}