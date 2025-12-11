using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class GDISchema : NameUrl, IUI, ICreatable, IEditable, IDisplayName
{
    public GDISchema()
    {
        base.StoreUrl = false;
    }

    #region Properties 

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        base.Create = create;

        //IInitParameter ip = Helper.GetRelInstance("webgisCMS.UI.dll", "webgisCMS.UI.MapCollectionControl") as IInitParameter;
        //if (ip != null) ip.InitParameter = this;

        //return ip;
        IInitParameter ip = new NameUrlControl();
        ip.InitParameter = this;
        return ip;
    }

    #endregion

    #region IPersistable Member

    override public void Load(IStreamDocument stream)
    {
        base.Load(stream);
    }

    override public void Save(IStreamDocument stream)
    {
        base.Save(stream);
    }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
            return this.Url + @"/.general";
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
