using E.Standard.Cms.Configuration.Models;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.Plattform;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Schema;
using System;
using System.Data;

namespace E.Standard.Cms.Extensions;
static public class CmsExtensions
{
    public static void AddCmsSecrets(this CmsReplace replace, CmsConfig.CmsItem cmsItem, CmsConfig.DeployItem deploy)
    {
        var secretsDi = DocumentFactory.PathInfo($"{cmsItem.Path}/__secrets".ToPlattformPath());

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

                    if (!String.IsNullOrEmpty(secret.Url))
                    {
                        replace.Add($"{{{{{secret.Url}}}}}", secret.GetSecret(deploy.Environment));
                    }
                }
            }
        }
    }

    public static void AddReplacementFile(this CmsReplace replace, string filename)
    {
        var table = CmsReplace.CreateEmptySchemaTable();
        table.ReadXml(filename);

        foreach (DataRow row in table.Rows)
        {
            if (!String.IsNullOrEmpty(row["From"]?.ToString()))
            {
                replace.Add(row["From"].ToString(), row["To"]?.ToString());
            }
        }
    }

}
