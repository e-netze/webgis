using E.Standard.Api.App.Configuration;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Abstractions;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;

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

    public Dictionary<string, CmsDocument> AllCmsDocuments()
    {
        Dictionary<string, CmsDocument> allcms = new Dictionary<string, CmsDocument>();

        foreach (var configKey in _config.GetPathsStartWith(ApiConfigKeys.ToKey("cmspath")))
        {
            string cmsName;

            if (configKey == ApiConfigKeys.ToKey("cmspath"))
            {
                cmsName = String.Empty;
            }
            else if (configKey.StartsWith(ApiConfigKeys.ToKey("cmspath_")))
            {
                cmsName = configKey.Substring(ApiConfigKeys.ToKey("cmspath_").Length);
            }
            else
            {
                continue;
            }
            CmsDocument cmsDocument = GetCmsDocument(cmsName);
            if (cmsDocument == null)
            {
                continue;
            }

            try
            {
                //CmsReplaceConfig cmsReplaceConfig = CmsReplaceConfig.TryLoad($"_config/replace_{ cmsName }.rpl");
                cmsDocument.ReplaceInXmlDocument($"_config/cms_replace.config");
            }
            catch { }

            allcms.Add(cmsName, cmsDocument);
        }

        return allcms;
    }

    public List<string> AllCmsDocumentNames()
    {
        List<string> cmsNames = new List<string>();

        foreach (var configKey in _config.GetPathsStartWith(ApiConfigKeys.ToKey("cmspath")))
        {
            string cmsName;

            if (configKey == ApiConfigKeys.ToKey("cmspath"))
            {
                cmsName = String.Empty;
            }
            else if (configKey.StartsWith(ApiConfigKeys.ToKey("cmspath_")))
            {
                cmsName = configKey.Substring(ApiConfigKeys.ToKey("cmspath_").Length);
            }
            else
            {
                continue;
            }

            cmsNames.Add(cmsName);
        }

        return cmsNames;
    }

    public CmsDocument GetCmsDocument(string postFix)
    {
        if (!String.IsNullOrEmpty(postFix))
        {
            postFix = "_" + postFix;
        }
        string cmsName = "CMS" + postFix;

        string path = _config[ApiConfigKeys.ToKey("cmspath" + postFix)];
        if (String.IsNullOrEmpty(path))
        {
            throw new Exception("Cms '" + cmsName + "' not found!");
        }

        path = path.Split('|')[0];

        CmsDocument cms = new CmsDocument(cmsName, _options.AppRootPath, ApiGlobals.AppEtcPath, _aclProviders);
        cms.ReadXml(path);

        return cms;
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