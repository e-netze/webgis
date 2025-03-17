using E.Standard.Cms.Abstraction;
using E.Standard.Cms.Configuration.Models;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Extensions;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Abstractions;
using E.Standard.Extensions.ErrorHandling;
using E.Standard.Web.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Xml;
using static E.Standard.CMS.Core.CMSManager;

namespace E.Standard.Cms.Services;

public class ReloadSchemeService : ICmsTool
{
    private readonly CmsConfigurationService _ccs;
    private readonly ICmsLogger _cmsLogger;
    private readonly IHttpService _http;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public ReloadSchemeService(
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

            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(context.ContentRootPath, "schemes", cmsItem.Scheme, "schema.xml"));

            var cms = new CMSManager(doc);
            cms.SetConnectionString(_servicePack, cmsItem.Path);

            var rootPath = cmsItem.Path.ToLower();
            cms.OnParseSchemaNode += (object? sender, EventArgs e) =>
            {
                if (e is ParseEventArgs)
                {
                    var pea = (ParseEventArgs)e;
                    if (pea.FileName.ToLower().StartsWith(rootPath))
                    {
                        console.WriteLine($"Parse: {pea.FileName.Substring(rootPath.Length + 1)}");
                    }
                }
            };
            cms.Reload(_servicePack, true);

            console.WriteLine("succeeded");

            return true;
        }
        catch (Exception ex)
        {
            console.WriteLine("---------------------------------------------------------------------------");
            console.WriteLines("EXCEPTION:");
            console.WriteLines(ex.FullMessage());
            //process.WriteLine("Stackgrache: " + ex.StackTrace);
            console.WriteLine("---------------------------------------------------------------------------");

            return false;
        }
    }
}
