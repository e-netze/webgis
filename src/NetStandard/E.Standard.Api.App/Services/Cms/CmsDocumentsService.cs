using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using E.Standard.Api.App.Configuration;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Abstractions;
using E.Standard.CMS.Core.Branches;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;

using Microsoft.Extensions.Options;

namespace E.Standard.Api.App.Services.Cms;

public class CmsDocumentsService
{
    private readonly ConfigurationService _config;
    private readonly CmsDocumentsServiceOptions _options;
    private readonly IEnumerable<ICustomApiCustomCmsService> _customCmsServices;
    private readonly IEnumerable<ICustomCmsDocumentAclProviderService> _aclProviders;

    public CmsDocumentsService(ConfigurationService config,
                               IOptionsMonitor<CmsDocumentsServiceOptions> optionsMonitor,
                               IEnumerable<ICustomCmsDocumentAclProviderService> aclProviderServices,
                               IEnumerable<ICustomApiCustomCmsService> customServices = null)
    {
        _config = config;
        _options = optionsMonitor.CurrentValue;
        _customCmsServices = customServices;
        _aclProviders = aclProviderServices;
    }

    public bool AllowBranches
        => bool.TryParse(_config[ApiConfigKeys.AllowBranches], out bool allow) && allow;

    public Dictionary<string, CmsDocument> AllCmsDocuments()
    {
        Dictionary<string, CmsDocument> allcms = new Dictionary<string, CmsDocument>();

        foreach (var cmsName in AllCmsDocumentNames())
        {
            CmsDocument cmsDocument = GetCmsDocument(cmsName);
            if (cmsDocument == null)
            {
                continue;
            }

            try
            {
                cmsDocument.ReplaceInXmlDocument($"_config/cms_replace.config");
            }
            catch { }

            allcms.Add(cmsName, cmsDocument);
        }

        return allcms;
    }

    // main cms names from api.config + (if allowed) the deployed branches found on disk: {cmsName}${encoded-branch}
    public List<string> AllCmsDocumentNames()
    {
        List<string> cmsNames = new List<string>();

        foreach (var cmsName in MainCmsDocumentNames())
        {
            cmsNames.Add(cmsName);
            cmsNames.AddRange(BranchCmsDocumentNames(cmsName));
        }

        return cmsNames;
    }

    public IEnumerable<string> BranchCmsDocumentNames(string mainCmsName)
    {
        if (!AllowBranches)
        {
            return Array.Empty<string>();
        }

        try
        {
            return CmsBranches.FindBranches(MainCmsDocumentPath(mainCmsName))
                              .Select(b => CmsBranches.ToCmsName(mainCmsName, b))
                              .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    // path of the xml file of a cms name (also branches); null if not exists
    public string CmsDocumentPath(string cmsName)
    {
        var (mainCmsName, encodedBranch) = CmsBranches.SplitCmsName(cmsName);
        var mainPath = MainCmsDocumentPath(mainCmsName);

        if (String.IsNullOrEmpty(encodedBranch) || String.IsNullOrEmpty(mainPath))
        {
            return mainPath;
        }

        if (!AllowBranches || !CmsBranches.IsValidEncoded(encodedBranch))
        {
            return null;
        }

        var branchPath = CmsBranches.BranchFilePath(mainPath, encodedBranch);
        return File.Exists(branchPath) ? branchPath : null;
    }

    public string MainCmsDocumentPath(string mainCmsName)
    {
        var path = _config[ApiConfigKeys.ToKey(String.IsNullOrEmpty(mainCmsName) ? "cmspath" : $"cmspath_{mainCmsName}")];

        return String.IsNullOrEmpty(path) ? null : path.Split('|')[0];
    }

    public CmsDocument GetCmsDocument(string cmsName)
    {
        string documentName = String.IsNullOrEmpty(cmsName) || cmsName.StartsWith(CmsBranches.CmsNameSeparator.ToString())
            ? $"CMS{cmsName}"
            : $"CMS_{cmsName}";

        string path = CmsDocumentPath(cmsName);
        if (String.IsNullOrEmpty(path))
        {
            throw new Exception("Cms '" + documentName + "' not found!");
        }

        CmsDocument cms = new CmsDocument(documentName, _options.AppRootPath, ApiGlobals.AppEtcPath, _aclProviders);
        cms.ReadXml(path);

        return cms;
    }

    private IEnumerable<string> MainCmsDocumentNames()
    {
        foreach (var configKey in _config.GetPathsStartWith(ApiConfigKeys.ToKey("cmspath")))
        {
            if (configKey == ApiConfigKeys.ToKey("cmspath"))
            {
                yield return String.Empty;
            }
            else if (configKey.StartsWith(ApiConfigKeys.ToKey("cmspath_")))
            {
                var cmsName = configKey.Substring(ApiConfigKeys.ToKey("cmspath_").Length);

                // branches are not configured anymore (cmspath_x$y), they are found on disk
                if (!cmsName.Contains(CmsBranches.CmsNameSeparator))
                {
                    yield return cmsName;
                }
            }
        }
    }

    public CmsDocument GetCustomCmsDocument(string cmsId)
    {
        var cmsFilePath = _customCmsServices.GetCustomCmsDocumentPath(cmsId);
        if (!String.IsNullOrWhiteSpace(cmsFilePath))
        {
            FileInfo fi = new FileInfo(cmsFilePath);
            if (fi.Exists)
            {
                CmsDocument cms = new CmsDocument(cmsId, _options.AppRootPath, ApiGlobals.AppEtcPath, _aclProviders);
                cms.ReadXml(fi.FullName);

                return cms;
            }
        }

        return null;
    }

    public string GetCustomCmsDocumentDisplayName(string cmsId)
    {
        return _customCmsServices.GetCustomCmsAccountName(cmsId);
    }
}