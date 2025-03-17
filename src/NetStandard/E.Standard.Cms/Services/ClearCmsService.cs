using E.Standard.Cms.Abstraction;
using E.Standard.Cms.Configuration.Models;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Extensions;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Abstractions;
using E.Standard.Extensions.Compare;
using E.Standard.Extensions.ErrorHandling;
using E.Standard.Web.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace E.Standard.Cms.Services;

public class ClearCmsService : ICmsTool
{
    private readonly CmsConfigurationService _ccs;
    private readonly ICmsLogger _cmsLogger;
    private readonly IHttpService _http;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public ClearCmsService(
                CmsConfigurationService ccs,
                ICmsLogger cmsLogger,
                IHttpService http,
                CmsItemInjectionPackService instanceService
            )
    {
        _ccs = ccs;
        _cmsLogger = cmsLogger;
        _http = http;

        _servicePack = instanceService.ServicePack;
    }

    public bool Run(CmsToolContext context, IConsoleOutputStream console)
    {
        try
        {
            var cmsId = context.CmsId;

            CmsConfig.CmsItem? cmsItem = null;
            //bool isDynamicCms = false;

            if (_ccs.IsCustomCms(cmsId))
            {
                cmsItem = cmsId.ToDynamicCmsItem(_ccs, _servicePack);
                //isDynamicCms = true;
            }
            else
            {
                cmsItem = _ccs.Instance.CmsItems.Where(i => i.Id == cmsId).FirstOrDefault();
            }
            if (cmsItem == null)
            {
                throw new Exception("Unknown Cms-Item-Id: " + cmsId);
            }

            string cmsTreePath = context.CmsTreePath.OrTake(cmsItem.Path);

            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(context.ContentRootPath, "schemes", cmsItem.Scheme, "schema.xml"));

            var cms = new CMSManager(doc);
            cms.SetConnectionString(_servicePack, cmsTreePath);

            var rootPath = cmsTreePath;
            console.WriteLine("Delete database...");
            cms.DeleteCmsDatabase(console).Wait();

            console.WriteLine("Reload Schema...");
            cms.Reload(_servicePack, false);

            console.WriteLine("succeeded");

            return true;

        }
        catch (Exception ex)
        {
            console.WriteLine("---------------------------------------------------------------------------");
            console.WriteLines("EXCEPTION:");
            console.WriteLines(ex.FullMessage());
            console.WriteLine("---------------------------------------------------------------------------");

            return false;
        }
    }
}