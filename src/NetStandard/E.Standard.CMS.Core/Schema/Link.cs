using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema.Abstraction;
using System.Collections.Generic;
using System.ComponentModel;

namespace E.Standard.CMS.Core.Schema;

public class Link : IPersistable
{
    private string _linkUri;

    public Link()
    {
        _linkUri = string.Empty;
    }
    public Link(string linkUri)
    {
        _linkUri = linkUri;
    }

    [Browsable(true)]
    [Category("~Link")]
    [DisplayName("Link Uri (only change this value if you know what you are doing!)")]
    //[ReadOnly(true)]
    public string LinkUri
    {
        get { return _linkUri; }
        set { _linkUri = value; }
    }

    public virtual void LoadParent(IStreamDocument stream)
    {

    }

    public virtual bool HasAdditionalLinks(string rootDirectory, string parentPath)
    {
        return false;
    }
    public virtual ICreatable GroupForAdditianalLinks()
    {
        return null;
    }
    public virtual List<Link> AdditionalLinks(string rootDirectory, string parentPath)
    {
        return new List<Link>();
    }

    #region IPersistable Member

    virtual public void Load(IStreamDocument stream)
    {
        _linkUri = (string)stream.Load("_linkuri", string.Empty);
    }

    virtual public void Save(IStreamDocument stream)
    {
        stream.Save("_linkuri", _linkUri.ToLower());
    }

    #endregion
}
