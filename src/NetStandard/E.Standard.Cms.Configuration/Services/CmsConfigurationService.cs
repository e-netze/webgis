using E.Standard.Cms.Configuration.Models;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.Plattform;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Schema;
using E.Standard.Configuration;
using E.Standard.Json;
using E.Standard.Security.App.Services.Abstraction;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Xml;

namespace E.Standard.Cms.Configuration.Services;

public class CmsConfigurationService
{
    private readonly CmsConfigurationServiceOptions _options;

    public CmsConfigurationService(
        IOptionsMonitor<CmsConfigurationServiceOptions> optionsMonitor,
        IEnumerable<IConfigValueParser> configValueParsers)
    {
        _options = optionsMonitor.CurrentValue;

        var appConfig = new JsonAppConfiguration("cms.config");

        if (!appConfig.Exists)
        {
            throw new Exception("_config/cms.config not exists");
        }

        Instance = appConfig.Deserialize<CmsConfig>();

        foreach (var configValueParser in configValueParsers)
        {
            Instance.Parse(configValueParser);
        }

        if (Instance.ServicesDefaultUrlScheme == "https://" || Instance.ServicesDefaultUrlScheme == "http://")
        {
            E.Standard.WebGIS.CmsSchema.CmsSchemaGlobals.ServicesDefaultUrlScheme = Instance.ServicesDefaultUrlScheme;
        }

        CMS = new Dictionary<string, E.Standard.CMS.Core.CMSManager>();
        TranslationDictionary = new Dictionary<string, XmlDocument>();

        foreach (var cmsItem in Instance.CmsItems)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(_options.ContentPath + "/schemes/" + cmsItem.Scheme + "/schema.xml");

            CMS[cmsItem.Id] = new E.Standard.CMS.Core.CMSManager(doc);
            CMS[cmsItem.Id].SetConnectionString(new CmsItemTransistantInjectionServicePack(null), cmsItem.Path);
            CMS[cmsItem.Id].CmsDisplayName = cmsItem.Name;
            CMS[cmsItem.Id].CmsSchemaName = cmsItem.Scheme;

            try
            {
                doc = new XmlDocument();
                doc.Load(_options.ContentPath + "/schemes/" + CmsGlobals.SchemaName + "/translate.xml");
                TranslationDictionary[cmsItem.Id] = doc;
            }
            catch { }
        }
    }

    public readonly CmsConfig Instance;

    public IDictionary<string, CMSManager> CMS { get; set; }
    public IDictionary<string, XmlDocument> TranslationDictionary { get; set; }

    public string Translate(string id, string key)
    {
        try
        {
            if (TranslationDictionary == null)
            {
                return key;
            }

            XmlNode node = TranslationDictionary[id].SelectSingleNode($"dictionary/add[{Helper.XPathToLowerCase("key")}='{key.ToLower().Replace("'", "\\'")}' and @value]");
            if (node != null)
            {
                return node.Attributes["value"].Value;
            }
        }
        catch { }

        return key;
    }

    public bool HasElastic
    {
        get
        {
            return !String.IsNullOrWhiteSpace(Instance.ElasticSearchEndpoint);
        }
    }

    public bool AllowCustomBehavior
    {
        get
        {
            return Instance.CustomCms != null && Instance.CustomCms.Allow == true && !String.IsNullOrWhiteSpace(Instance.CustomCms.RootUrl) && !String.IsNullOrWhiteSpace(Instance.CustomCms.Scheme);
        }
    }

    public bool IsCustomCms(string cmsId)
    {
        if (String.IsNullOrWhiteSpace(cmsId))
        {
            return false;
        }

        if (!AllowCustomBehavior)
        {
            return false;
        }

        return Instance.CmsItems?
            .Where(i => cmsId.Equals(i.Id, StringComparison.InvariantCultureIgnoreCase))
            .FirstOrDefault() == null;
    }

    public void InitCustomCms(CmsItemTransistantInjectionServicePack servicePack,
                              string cmsId)
    {
        if (!IsCustomCms(cmsId) || CMS.ContainsKey(cmsId))
        {
            return;
        }

        var fi = new FileInfo(Instance.CustomCms.RootUrl + "/" + cmsId + "/_info.config");
        var displayName = fi.Exists ? (CustomCmsInfoAccountName(fi.FullName) ?? cmsId) : cmsId;

        XmlDocument doc = new XmlDocument();
        doc.Load(_options.ContentPath + "/schemes/" + Instance.CustomCms.Scheme + "/schema.xml");

        string connectionString = Instance.CustomCms.RootUrl + "/" + cmsId + "/cms";
        if (!String.IsNullOrWhiteSpace(Instance.CustomCms.RootTemplate))
        {
            connectionString = Instance.CustomCms.RootTemplate.Replace("{id}", cmsId);
        }

        CMS[cmsId] = new CMSManager(doc);
        CMS[cmsId].SetConnectionString(servicePack, connectionString);
        CMS[cmsId].CmsDisplayName = CMS[cmsId].CmsDisplayName ?? displayName;
        CMS[cmsId].CmsSchemaName = Instance.CustomCms.Scheme;

        try
        {
            // DoTo: Ist immer gleich, sollte man das nicht nur einmal speichern (RAM Verbrauch, wenn es viele CMS gibt)
            doc = new XmlDocument();
            doc.Load(_options.ContentPath + "/schemes/" + CmsGlobals.SchemaName + "/translate.xml");
            TranslationDictionary[cmsId] = doc;
        }
        catch { }
    }

    public NameValueCollection GetCmsSecrets(CmsItemTransistantInjectionServicePack servicePack,
                                             string cmsId,
                                             DeployEnvironment environment = DeployEnvironment.Default)
    {
        var result = new NameValueCollection();

        XmlDocument doc = this.CMS[cmsId].ToXml(servicePack, false, false);
        XmlNode rootNode = doc.SelectSingleNode("CMS[@root]");

        var secretsDi = DocumentFactory.PathInfo($"{rootNode.Attributes["root"].Value}/__secrets".ToPlattformPath());
        if (secretsDi.Exists)
        {
            ItemOrder itemOrder = new ItemOrder(secretsDi.FullName, true);
            foreach (string item in itemOrder.Items)
            {
                var secretFi = DocumentFactory.DocumentInfo($"{secretsDi.FullName}/{item}".ToPlattformPath());
                if (secretFi.Exists)
                {
                    var secret = new Secret();
                    secret.Load(DocumentFactory.Open(secretFi.FullName));

                    if (!String.IsNullOrEmpty(secret.Placeholder))
                    {
                        result[secret.Placeholder] = secret.GetSecret(environment);
                    }
                }
            }
        }

        return result;
    }

    public string SecretsPassword(string cmsId)
    {
        if (IsCustomCms(cmsId))
        {
            var fi = new FileInfo(Instance.CustomCms.RootUrl + "/" + cmsId + "/_info.config");
            return fi.Exists ?
                (CustomCmsInfoSecretsPassword(fi.FullName) ?? String.Empty) :
                String.Empty;
        }

        var item = Instance.CmsItems?.Where(i => i.Id == cmsId).FirstOrDefault();
        return item?.SecretsPassword ?? String.Empty;
    }

    #region Helper

    private string CustomCmsInfoAccountName(string dynamimcInfoFilename)
    {
        var dynamicInfo = JSerializer.Deserialize<CustomCmsInfo>(File.ReadAllText(dynamimcInfoFilename));
        return dynamicInfo.Name;
    }

    private string CustomCmsInfoSecretsPassword(string dynamimcInfoFilename)
    {
        var dynamicInfo = JSerializer.Deserialize<CustomCmsInfo>(File.ReadAllText(dynamimcInfoFilename));
        return dynamicInfo.SecretsPassword;
    }

    #endregion
}
