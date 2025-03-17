using E.Standard.Cms.Abstraction;
using E.Standard.Cms.Configuration.Models;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Extensions;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Abstractions;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.Extensions.ErrorHandling;
using E.Standard.Web.Abstractions;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;

namespace E.Standard.Cms.Services;

public class ExportCmsService : ICmsTool
{
    private readonly CmsConfigurationService _ccs;
    private readonly ICmsLogger _cmsLogger;
    private readonly IHttpService _http;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public ExportCmsService(
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

            var rootPath = cmsItem.Path;
            var root = DocumentFactory.PathInfo(rootPath);
            using (MemoryStream zipFileStream = new MemoryStream())
            {
                using (ZipArchive archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create))
                {
                    ExportPath(context, console, cms, archive, root, rootPath);
                }
                if (!console.IsCanceled)
                {
                    console.PutFile(new ConsoleStreamFileResult($"{cmsId}.zip", "application/zip", zipFileStream.ToArray()));
                }
            }

            if (console.IsCanceled)
            {
                console.WriteLine("canceled");

                return false;
            }
            else
            {
                console.WriteLine("succeeded");

                return true;
            }
        }
        catch (Exception ex)
        {
            console.WriteLine("---------------------------------------------------------------------------");
            console.WriteLine("EXCEPTION:");
            console.WriteLines(ex.FullMessage());
            console.WriteLine("---------------------------------------------------------------------------");

            return false;
        }
    }

    private void ExportPath(CmsToolContext context, IConsoleOutputStream console, CMSManager cms, ZipArchive archive, IPathInfo pathInfo, string rootPath)
    {
        if (console.IsCanceled)
        {
            return;
        }
        foreach (var childPathInfo in pathInfo.GetDirectories())
        {
            var archivePath = ArchivePath(context.CmsId, rootPath, childPathInfo.FullName) + "/";  // end with / => folder
            archive.CreateEntry(archivePath);

            ExportPath(context, console, cms, archive, childPathInfo, rootPath);
        }

        foreach (var childDocumentInfo in pathInfo.GetFiles())
        {
            var archivePath = ArchivePath(context.CmsId, rootPath, childDocumentInfo.FullName);

            console.WriteLine($"export {archivePath}");

            string xml = childDocumentInfo is IXmlConverter ?
                ((IXmlConverter)childDocumentInfo).ReadAllAsXmlString() :
                childDocumentInfo.ReadAll();

            var entry = archive.CreateEntry(archivePath);
            using (StreamWriter writer = new StreamWriter(entry.Open()))
            {
                writer.Write(xml);
            }
        }
    }

    private string ArchivePath(string cmsId, string rootPath, string itemPath)
    {
        if (itemPath.Replace(@"\", "/").ToLower().StartsWith(rootPath.Replace(@"\", "/").ToLower()))
        {
            itemPath = itemPath.Substring(rootPath.Length).Replace(@"\", "/");
            while (itemPath.StartsWith("/"))
            {
                itemPath = itemPath.Substring(1);
            }

            return cmsId + "/" + itemPath;
        }
        else
        {
            throw new Exception("Invalid root path");
        }
    }
}
