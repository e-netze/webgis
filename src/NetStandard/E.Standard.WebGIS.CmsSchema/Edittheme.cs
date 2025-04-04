using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class EditTheme : CopyableNode, IUI, ICreatable, IEditable, IDisplayName
{
    private string _connection = String.Empty;

    public EditTheme()
    {
        base.StoreUrl = false;
        base.Create = true;
    }

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new NameUrlControl();
        ip.InitParameter = this;

        return ip;
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

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _connection = (string)stream.Load("connection", String.Empty);
    }
    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("connection", _connection);
    }

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Edit Thema"; }
    }
}
