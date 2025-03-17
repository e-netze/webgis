using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema.Abstraction;
using System;
using System.Threading.Tasks;

namespace E.Standard.CMS.Core.Schema;

public abstract class CopyableXml : Copyable, ICreatable
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

            string xmlFile = CopyCmsManager.ConnectionString + @"/" + RelativePath + ".xml";
            var fiFrom = DocumentFactory.DocumentInfo(xmlFile);
            if (!fiFrom.Exists)
            {
                return false;
            }

            var fiTo = DocumentFactory.DocumentInfo(CmsManager.ConnectionString + @"/" + UriPath + @"/" + CreateAs(false) + ".xml");
            while (fiTo.Exists)
            {
                throw new Exception("Already exists");
            }

            BeforeCopy();
            IStreamDocument xmlStream = DocumentFactory.New(CmsManager.ConnectionString);
            Save(xmlStream);
            xmlStream.SaveDocument(fiTo.FullName);

            return true;
        }
        finally
        {
            isInCopyMode = false;
        }
    }

    #endregion

    #region ICreatable Member

    public abstract string CreateAs(bool appendRoot);

    public abstract Task<bool> CreatedAsync(string FullName);

    #endregion
}
