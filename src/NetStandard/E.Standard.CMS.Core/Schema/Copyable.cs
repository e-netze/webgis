using E.Standard.CMS.Core.Schema.Abstraction;
using Newtonsoft.Json;
using System.ComponentModel;

namespace E.Standard.CMS.Core.Schema;

public abstract class Copyable : NameUrl, ICopyable
{
    public abstract string NodeTitle
    {
        get;
    }

    protected virtual void BeforeCopy() { }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    private bool _isInCopyMode = false;
    protected bool isInCopyMode
    {
        get { return _isInCopyMode; }
        set { _isInCopyMode = value; }
    }

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

    abstract public bool CopyTo(string UriPath);

    #endregion
}
