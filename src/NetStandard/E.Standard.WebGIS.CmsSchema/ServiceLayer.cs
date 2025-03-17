using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

[ReadOnly(true)]
public class ServiceLayer : NameUrl, IEditable, IDisplayName, ICreatable
{
    private string _id = String.Empty;
    private bool _visible = true;

    public ServiceLayer()
    {
        base.StoreUrl = false;
    }

    #region Properties

    [ReadOnly(true)]
    public string Id
    {
        get { return _id; }
        internal set { _id = value; }
    }

    [ReadOnly(true)]
    public bool Visible
    {
        get { return _visible; }
        internal set { _visible = value; }
    }

    [ReadOnly(true)]
    virtual public new string Name
    {
        get { return base.Name; }
        set { base.Name = value; }
    }

    #endregion

    #region IPersistable Member

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _id = (string)stream.Load("id", String.Empty);
        _visible = (bool)stream.Load("visible", true);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("id", _id);
        stream.Save("visible", _visible);
    }

    #endregion

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return this.Name; }
    }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        return this.Url;
    }

    public Task<bool> CreatedAsync(string FullName)
    {
        return Task<bool>.FromResult(true);
    }

    #endregion
}
