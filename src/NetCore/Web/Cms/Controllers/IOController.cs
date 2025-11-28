using Cms.AppCode;
using Cms.AppCode.Mvc;
using Cms.AppCode.Services;
using Cms.Models;
using E.Standard.Cms.Configuration.Models;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Cms.Services;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Extensions.ErrorHandling;
using E.Standard.Security.App.Reflection;
using E.Standard.Security.App.Services;
using E.Standard.Security.Cryptography.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace Cms.Controllers;

[ApplicationSecurity]
public class IOController : ApplicationSecurityController
{
    private readonly CmsConfigurationService _ccs;
    private readonly string _applicationContentRootPath;
    private readonly ClearCmsService _clearCmsService;
    private readonly ReloadSchemeService _reloadSchemeService;
    private readonly ExportCmsService _exportCmsService;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public IOController(
                CmsConfigurationService ccs,
                UrlHelperService urlHelperService,
                ApplicationSecurityUserManager applicationSecurityUserManager,
                IWebHostEnvironment environment,
                ICryptoService crypto,
                CmsItemInjectionPackService instanceService,
                ClearCmsService clearCmsService,
                ReloadSchemeService reloadSchemeService,
                ExportCmsService exportCmsService,
                IEnumerable<ICustomCmsPageSecurityService> customSecurity = null)
        : base(ccs, urlHelperService, applicationSecurityUserManager, customSecurity, crypto, instanceService)
    {
        _ccs = ccs;
        _applicationContentRootPath = environment.ContentRootPath;
        _clearCmsService = clearCmsService;
        _reloadSchemeService = reloadSchemeService;
        _exportCmsService = exportCmsService;

        _servicePack = instanceService.ServicePack;
    }

    #region Export

    [HttpGet]
    public IActionResult Export(string id)
    {
        try
        {
            return View();
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    [HttpPost]
    public IActionResult Export(string id, ExportModel model)
    {
        try
        {
            bool forLinux =
                Request.Form["for-linux"] == "checked" ||
                Request.Form["for-linux"] == "on" ||
                Request.Form["for-linux"] == "true"; 

            var backgroundProcess = new BackgroundProcess(id, this.GetCurrentUsername(), ExportCms, 
                new CmsExportDefinition()
                {
                    ForLinux = forLinux
                });

            return View(new ExportModel()
            {
                ProcDefinition = backgroundProcess.ProcDefinition("Export")
            });
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    #region Background Process

    private void ExportCms(object arg)
    {
        BackgroundProcess process = (BackgroundProcess)arg;

        var context = new CmsToolContext()
        {
            CmsId = process.CmsId,
            Deployment = process.UserData,
            ContentRootPath = _applicationContentRootPath,
            Username = process.UserName
        };

        _exportCmsService.Run(context, process);
    }

    private void ExportPath(BackgroundProcess process, CMSManager cms, ZipArchive archive, IPathInfo pathInfo, string rootPath)
    {
        if (process.IsCanceled)
        {
            return;
        }
        foreach (var childPathInfo in pathInfo.GetDirectories())
        {
            var archivePath = ArchivePath(process.CmsId, rootPath, childPathInfo.FullName) + "/";  // end with / => folder
            archive.CreateEntry(archivePath);

            ExportPath(process, cms, archive, childPathInfo, rootPath);
        }

        foreach (var childDocumentInfo in pathInfo.GetFiles())
        {
            var archivePath = ArchivePath(process.CmsId, rootPath, childDocumentInfo.FullName);

            process.WriteLine($"export {archivePath}");

            string xml = childDocumentInfo is IXmlConverter xmlConverter 
                ? xmlConverter.ReadAllAsXmlString() 
                : childDocumentInfo.ReadAll();

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

    private CmsConfig.CmsItem DynamicCmsItem(string cmsId)
    {
        _ccs.InitCustomCms(_servicePack, cmsId);

        return new CmsConfig.CmsItem()
        {
            Id = cmsId,
            Name = _ccs.CMS[cmsId].CmsDisplayName,
            Scheme = _ccs.Instance.CustomCms.Scheme,
            Path = _ccs.CMS[cmsId].ConnectionString,
            Deployments = new CmsConfig.DeployItem[]
            {
                            new CmsConfig.DeployItem()
                            {
                                Name=_ccs.CMS[cmsId].CmsDisplayName,
                                Target=_ccs.Instance.CustomCms.RootUrl+"/"+cmsId+"/cms.xml"
                            }
            }
        };
    }

    #endregion

    #endregion

    #region Import

    [HttpGet]
    public IActionResult Import(string id)
    {
        ViewData["CmsId"] = id;
        return View();
    }

    [HttpPost]
    public IActionResult Import(string id, ImportModel model)
    {
        try
        {
            ViewData["CmsId"] = id;

            if (String.IsNullOrEmpty(id) ||
               id != Request.Form["confirm-id"])
            {
                ViewData["FormError"] = "Insert the correct Cms-Id to confirm clearing the Cms.";
                return View();
            }

            if (this.Request.Form.Files.Count == 0)
            {
                ViewData["FormError"] = "No file uploaded";
                return View();
            }

            byte[] fileBuffer = new byte[this.Request.Form.Files[0].Length];
            this.Request.Form.Files[0].OpenReadStream().ReadExactly(fileBuffer, 0, fileBuffer.Length);
            var backgroundProcess = new BackgroundProcess(id, this.GetCurrentUsername(), ImportCms,
                new ImportDefinition()
                {
                    ImportType = model.ImportType,
                    FileBytes = fileBuffer
                });

            return View(new ImportModel()
            {
                ProcDefinition = backgroundProcess.ProcDefinition("Import")
            });
        }
        catch (Exception ex)
        {
            model = model ?? new ImportModel();
            model.ErrorMesssage = ex.Message;
            return View(model);
        }
    }

    #region Background Process

    private void ImportCms(object arg)
    {
        BackgroundProcess process = (BackgroundProcess)arg;
        try
        {
            var cmsId = process.CmsId;

            CmsConfig.CmsItem cmsItem = null;
            //bool isDynamicCms = false;

            if (_ccs.IsCustomCms(cmsId))
            {
                cmsItem = DynamicCmsItem(cmsId);
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
            doc.Load(_applicationContentRootPath + "/schemes/" + cmsItem.Scheme + "/schema.xml");

            var cms = new CMSManager(doc);
            cms.SetConnectionString(_servicePack, cmsItem.Path);

            var importDefinition = (ImportDefinition)process.UserData;

            var fileStream = new MemoryStream(importDefinition.FileBytes);
            using (ZipArchive archive = new ZipArchive(fileStream))
            {
                foreach (var entry in archive.Entries)
                {
                    var cmspath = CmsPath(cmsItem.Path, entry);
                    var entryPath = entry.FullName.Substring(entry.FullName.IndexOf("/") + 1);

                    //var schemeNode=cms.SchemaNode(entryPath, CmsNodeType.Any);
                    //if(schemeNode==null)
                    //{
                    //    schemeNode = cms.SchemaNode(entryPath, CmsNodeType.Any);
                    //    process.WriteLine($"WARNING: invalid schema-path {entryPath}");
                    //    continue;
                    //}

                    if (process.IsCanceled == true)
                    {
                        break;
                    }

                    if (IsFolderEntry(entry))
                    {

                        var pathInfo = DocumentFactory.PathInfo(cmspath);
                        if (!pathInfo.Exists)
                        {
                            pathInfo.Create();
                            process.WriteLine("create folder " + entryPath);
                        }
                    }
                    else
                    {
                        using (var entryStream = entry.Open())
                        {
                            using (var reader = new BinaryReader(entryStream))
                            {
                                var bytes = reader.ReadBytes((int)entry.Length);
                                var documentInfo = DocumentFactory.DocumentInfo(cmspath);
                                if (documentInfo is IXmlConverter xmlConverter)
                                {
                                    if (xmlConverter.WriteXmlData(Encoding.UTF8.GetString(bytes), importDefinition.ImportType == ImportType.UpdateAll))
                                    {
                                        process.WriteLine("create/update file " + entryPath);
                                    } 
                                    else
                                    {
                                        process.WriteLine("skip existing file " + entryPath);
                                    }
                                } 
                                else
                                {
                                    if (documentInfo.Exists && importDefinition.ImportType == ImportType.OnlyNew)
                                    {
                                        process.WriteLine("skip existing file " + entryPath);
                                    }
                                    else
                                    {
                                        documentInfo.Write(Encoding.UTF8.GetString(bytes));
                                        process.WriteLine("create/update file " + entryPath);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (process.IsCanceled)
            {
                process.WriteLine("canceled");
            }
            else
            {
                process.WriteLine("succeeded");
            }
        }
        catch (Exception ex)
        {
            process.WriteLine("---------------------------------------------------------------------------");
            process.WriteLines("EXCEPTION:");
            process.WriteLines(ex.FullMessage());
            process.WriteLine("---------------------------------------------------------------------------");
        }
    }

    private bool IsFolderEntry(ZipArchiveEntry entry)
    {
        return entry.FullName.EndsWith("/");
    }

    private string CmsPath(string rootPath, ZipArchiveEntry entry)
    {
        var fullName = entry.FullName;
        while (fullName.EndsWith("/"))
        {
            fullName = fullName.Substring(0, fullName.Length - 1);
        }

        fullName = fullName.Replace(@"\", "/");
        fullName = fullName.Substring(fullName.IndexOf("/") + 1);  // cut first sub directory ... cms1/service/... => sercices/...

        return rootPath + "/" + fullName;
    }

    public class ImportDefinition
    {
        public ImportType ImportType { get; set; }
        public byte[] FileBytes { get; set; }
    }

    #endregion

    #endregion

    #region Clear

    [HttpGet]
    public IActionResult Clear(string id)
    {
        try
        {
            ViewData["CmsId"] = id;
            return View();
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    [HttpPost]
    public IActionResult Clear(string id, ClearModel model)
    {
        try
        {
            ViewData["CmsId"] = id;

            if (String.IsNullOrEmpty(id) ||
               id != Request.Form["confirm-id"])
            {
                ViewData["FormError"]= "Insert the correct Cms-Id to confirm clearing the Cms.";
                return View();
            }

            var backgroundProcess = new BackgroundProcess(id, this.GetCurrentUsername(), ClearCms, id);

            return View(new ClearModel()
            {
                ProcDefinition = backgroundProcess.ProcDefinition("Clear")
            });
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    #region Background Process

    private void ClearCms(object arg)
    {
        BackgroundProcess process = (BackgroundProcess)arg;

        var context = new CmsToolContext()
        {
            CmsId = process.CmsId,
            Deployment = process.UserData,
            ContentRootPath = _applicationContentRootPath,
            Username = process.UserName
        };

        _clearCmsService.Run(context, process);
    }

    #endregion

    #endregion

    #region Reload Root

    [HttpGet]
    public IActionResult ReloadScheme(string id)
    {
        try
        {
            return View();
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    [HttpPost]
    public IActionResult ReloadScheme(string id, ReloadSchemeModel model)
    {
        try
        {
            var backgroundProcess = new BackgroundProcess(id, this.GetCurrentUsername(), ReloadCmsScheme, id);

            return View(new ReloadSchemeModel()
            {
                ProcDefinition = backgroundProcess.ProcDefinition("ReloadScheme")
            });
        }
        catch (Exception ex)
        {
            return base.ExceptionResult(ex);
        }
    }

    #region Background Process

    private void ReloadCmsScheme(object arg)
    {
        BackgroundProcess process = (BackgroundProcess)arg;

        var context = new CmsToolContext()
        {
            CmsId = process.CmsId,
            Deployment = process.UserData,
            ContentRootPath = _applicationContentRootPath,
            Username = process.UserName
        };

        _reloadSchemeService.Run(context, process);
    }

    #endregion

    #endregion
}