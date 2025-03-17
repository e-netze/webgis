using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.WebGIS.CmsSchema.UI;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class MapCollection : NameUrl, IUI, ICreatable, IEditable, IDisplayName
{
    private bool _visible = true, _print = true;

    public MapCollection()
    {
        base.StoreUrl = false;
    }

    #region Properties 
    [DisplayName("Sichtbar")]
    public bool Visible
    {
        get { return _visible; }
        set { _visible = value; }
    }
    [DisplayName("Im Ausdruck anzeigen")]
    public bool Print
    {
        get { return _print; }
        set { _print = value; }
    }
    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        base.Create = create;

        //IInitParameter ip = Helper.GetRelInstance("webgisCMS.UI.dll", "webgisCMS.UI.MapCollectionControl") as IInitParameter;
        //if (ip != null) ip.InitParameter = this;

        //return ip;
        IInitParameter ip = new MapCollectionControl();
        ip.InitParameter = this;
        return ip;
    }

    #endregion

    #region IPersistable Member

    override public void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _visible = (bool)stream.Load("visible", true);
        _print = (bool)stream.Load("print", true);
    }

    override public void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("visible", _visible);
        stream.Save("print", _print);
    }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
            return this.Url + @"\.general";
        }
        else
        {
            return ".general";
        }
    }

    public Task<bool> CreatedAsync(string FullName)
    {
        return Task<bool>.FromResult(true);
    }
    #endregion

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return Name; }
    }

    #endregion
}
