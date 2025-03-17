using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.Plattform;
using E.Standard.CMS.Core.Schema.Abstraction;
using System;
using System.ComponentModel;

namespace E.Standard.CMS.Core.Schema;

public class SchemaNodeLink : Link, ISchemaNode, ICopyable
{
    private string _relativePath = string.Empty;
    private CMSManager _manager = null;

    #region ISchemaNode Member
    [Browsable(false)]
    virtual public string RelativePath
    {
        get
        {
            return _relativePath;
        }
        set
        {
            _relativePath = value;
        }
    }
    [Browsable(false)]
    virtual public CMSManager CmsManager
    {
        get { return _manager; }
        set { _manager = value; }
    }

    #endregion

    #region ICopyable Member

    private CMSManager _copyCmsManager;
    [Browsable(false)]
    public CMSManager CopyCmsManager
    {
        get
        {
            return _copyCmsManager;
        }
        set
        {
            _copyCmsManager = value;
        }
    }

    public bool CopyTo(string UriPath)
    {
        var fi = DocumentFactory.DocumentInfo((CmsManager.ConnectionString + @"/" + RelativePath + ".link").ToPlattformPath());
        if (fi.Exists)
        {
            try
            {
                fi.CopyTo(_copyCmsManager.ConnectionString + @"/" + UriPath + @"/" + "l" + GuidEncoder.Encode(Guid.NewGuid()) + ".link");
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    #endregion
}
