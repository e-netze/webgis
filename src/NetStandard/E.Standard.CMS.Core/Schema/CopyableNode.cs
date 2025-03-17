using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema.Abstraction;
using System;
using System.IO;
using System.Xml;

namespace E.Standard.CMS.Core.Schema;

public abstract class CopyableNode : Copyable
{
    #region ICopyable Member

    override public bool CopyTo(string UriPath)
    {
        try
        {
            isInCopyMode = true;

            if (CmsManager == null ||
                CopyCmsManager == null)
            {
                return false;
            }

            var diFrom = DocumentFactory.PathInfo(CopyCmsManager.ConnectionString + @"/" + RelativePath);
            if (!diFrom.Exists)
            {
                return false;
            }

            var diTo = DocumentFactory.PathInfo(CmsManager.ConnectionString + @"/" + UriPath);
            if (!diFrom.Exists)
            {
                return false;
            }

            var target = DocumentFactory.PathInfo(diTo.FullName + @"/" + /*diFrom.Name*/Url);
            while (target.Exists)
            {
                throw new Exception("Already exists");
            }

            BeforeCopy();

            string oldNodePath = RelativePath;
            string newNodePath = target.FullName.Substring(CopyCmsManager.ConnectionString.Length, target.FullName.Length - CopyCmsManager.ConnectionString.Length).Replace("\\", "/");

            if (!oldNodePath.EndsWith("/"))
            {
                oldNodePath += "/";
            }

            if (!newNodePath.EndsWith("/"))
            {
                newNodePath += "/";
            }

            CopyAll(diFrom, target, oldNodePath, newNodePath, 0);
            if (this is ICreatable)
            {
                ICreatable c = (ICreatable)this;

                var fiConfig = DocumentFactory.DocumentInfo(diTo.FullName + @"/" + c.CreateAs(true) + ".xml");
                if (fiConfig.Exists)  // Config neu schreiben
                {
                    IStreamDocument xmlStream = DocumentFactory.New(CmsManager.ConnectionString);
                    Save(xmlStream);
                    xmlStream.SaveDocument(fiConfig.FullName);
                }
            }

            return true;
        }
        finally
        {
            isInCopyMode = false;
        }
    }

    #endregion

    #region Helper

    private void CopyAll(IPathInfo source, IPathInfo target, string oldNodePath, string newNodePath, int level)
    {
        if (level > 5)  // don't copy deeper: durch Bugs könnten so unendlich Kopiert werden!!!
        {
            return;
        }

        // Check if the target directory exists, if not, create it.
        //if (Directory.Exists(target.FullName) == false)
        //{
        //    Directory.CreateDirectory(target.FullName);
        //}
        if (target.Exists == false)
        {
            target.Create();
        }

        // Copy each file into it’s new directory.
        foreach (var fi in source.GetFiles())
        {
            var targetFi = DocumentFactory.DocumentInfo(Path.Combine(target.FullName, fi.Name));
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);

            if (targetFi.Extension.ToLower() == ".link" || targetFi.Extension.ToLower() == ".xml")
            {
                var xml = targetFi is IXmlConverter ? ((IXmlConverter)targetFi).ReadAllAsXmlString() : targetFi.ReadAll();
                XmlDocument targetXml = new XmlDocument();
                targetXml.LoadXml(xml);
                XmlNode linkuriNode = targetXml.SelectSingleNode("config/_linkuri");
                if (linkuriNode != null && linkuriNode.InnerText.Trim().StartsWith(oldNodePath))
                {
                    string newLinkUrl = linkuriNode.InnerText.Trim().Substring(oldNodePath.Length, linkuriNode.InnerText.Trim().Length - oldNodePath.Length);
                    newLinkUrl = newNodePath + newLinkUrl;
                    while (newLinkUrl.Contains("//"))
                    {
                        newLinkUrl = newLinkUrl.Replace("//", "/");
                    }

                    while (newLinkUrl.StartsWith("/"))
                    {
                        newLinkUrl = newLinkUrl.Substring(1, newLinkUrl.Length - 1);
                    }

                    linkuriNode.InnerText = newLinkUrl;
                    //targetXml.Save(targetFi.FullName);
                    targetFi.Write(targetXml.OuterXml);
                }

                #region Copy Auth

                CopyFileIfExists(
                    DocumentFactory.DocumentInfo(Path.Combine(source.FullName, $"{fi.Title}.acl")),
                    DocumentFactory.DocumentInfo(Path.Combine(target.FullName, $"{fi.Title}.acl")));

                #endregion
            }
        }

        #region Copy Auth

        CopyFileIfExists(
            DocumentFactory.DocumentInfo($"{source.FullName}.acl"),
            DocumentFactory.DocumentInfo($"{target.FullName}.acl"));

        #endregion

        // Copy each subdirectory using recursion.
        foreach (var diSourceSubDir in source.GetDirectories())
        {
            var nextTargetSubDir =
                target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir, oldNodePath, newNodePath, level + 1);
        }
    }

    private void CopyFileIfExists(IDocumentInfo fromPathInfo, IDocumentInfo toPathInfo)
    {
        if (fromPathInfo.Exists)
        {
            if (toPathInfo.Exists)
            {
                toPathInfo.Delete();
            }

            fromPathInfo.CopyTo(toPathInfo.FullName);
        }
    }

    #endregion
}
