using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class EditingGroup : NameUrl, IUI, ICreatable, IDisplayName, IEditable
{
    public EditingGroup()
    {
        base.StoreUrl = false;
        base.ValidateUrl = false;
    }

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new NameUrlControl();
        ((NameUrlControl)ip).UrlIsVisible = false;

        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
            return Crypto.GetID() + @"\.general";
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
        get { return this.Name; }
    }

    #endregion

    #region IPersistable
    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);
    }
    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);
    }
    #endregion
}
