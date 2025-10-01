using E.Standard.Cms.Abstraction;
using E.Standard.Cms.Configuration.Models;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Extensions;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Abstractions;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.Extensions.Compare;
using E.Standard.Extensions.ErrorHandling;
using E.Standard.Web.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace E.Standard.Cms.Services;

public class SolveWaringsService : ICmsTool
{
    private readonly CmsConfigurationService _ccs;
    private readonly ICmsLogger _cmsLogger;
    private readonly IHttpService _http;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public SolveWaringsService(
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
        IPathInfo2? rootPathInfo2 = null;

        try
        {
            CmsConfig.CmsItem? cmsItem = null;
            if (_ccs.IsCustomCms(context.CmsId))
            {
                cmsItem = context.CmsId.ToDynamicCmsItem(_ccs, _servicePack);
            }
            else
            {
                cmsItem = _ccs.Instance.CmsItems.Where(i => i.Id == context.CmsId).FirstOrDefault();
            }
            if (cmsItem == null)
            {
                throw new Exception("Unknown Cms-Item-Id: " + context.CmsId);
            }

            string cmsTreePath = context.CmsTreePath.OrTake(cmsItem.Path);

            var deploy = cmsItem.Deployments.Where(d => d.Name == context.Deployment.ToString()).FirstOrDefault();
            if (deploy == null)
            {
                throw new Exception("Unknown deploy: " + context.CmsId + "/" + context.Deployment.ToString());
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(context.ContentRootPath, "schemes", cmsItem.Scheme, "schema.xml"));

            var cms = new CMSManager(doc);
            cms.SetConnectionString(_servicePack, cmsTreePath);

            rootPathInfo2 = DocumentFactory.PathInfo(cmsTreePath) as IPathInfo2;
            if (rootPathInfo2 != null)
            {
                console.WriteLine($"Loaded {rootPathInfo2.CacheAllRecursive()} nodes...");
            }

            int counter = 0;
            cms.OnParseWaring += (object? sender, EventArgs e) =>
            {
                counter++;
                if (counter % 1000 == 0)
                {
                    if (e is CMSManager.ParseEventArgs)
                    {
                        CMSManager.ParseEventArgs pe = (CMSManager.ParseEventArgs)e;

                        console.WriteLine("scanned " + counter + " nodes...");
                    }
                }
            };

            console.WriteLine("Scann for warnings");
            var warnings = cms.Warnings();

            FileInfo fiWarnings = deploy.Target.WarningsFileInfo();
            if (fiWarnings.Exists)
            {
                fiWarnings.Delete();
            }

            if (warnings.Count > 0)
            {
                foreach (var warning in warnings)
                {
                    var path = warning.Path;
                    if (path.ToLower().StartsWith(cms.ConnectionString.ToLower()) ||
                        path.ToLower().Replace(@"\", "/").StartsWith(cms.ConnectionString.ToLower().Replace(@"\", "/")))
                    {
                        path = path.Substring(cms.ConnectionString.Length + 1);
                    }

                    string line = "solve: " + warning.Message + ": " + path;
                    console.WriteLine(line);

                    //FileInfo fi = new FileInfo(warning.Filename);
                    var fi = DocumentFactory.DocumentInfo($"{cms.ConnectionString}/{path}");
                    if (fi.Exists)
                    {
                        fi.Delete();
                    }
                }
            }

            _cmsLogger.Log(context.Username,
                           "Warnings", "Solved_Succeeded", context.CmsId);

            console.WriteLine("Succeeded");

            return true;
        }
        catch (Exception ex)
        {
            console.WriteLine("---------------------------------------------------------------------------");
            console.WriteLines("EXCEPTION:");
            console.WriteLines(ex.FullMessage());
            //process.WriteLine("Stacktrace:");
            //foreach (string stacktrace in ex.StackTrace.Replace("\r", "").Split("\n"))
            //    process.WriteLine(stacktrace);
            console.WriteLine("---------------------------------------------------------------------------");

            return false;
        }
        finally
        {
            if (rootPathInfo2 != null)
            {
                console.WriteLine("Release Cache");
                rootPathInfo2.ReleaseCacheRecursive();
            }
        }
    }
}
