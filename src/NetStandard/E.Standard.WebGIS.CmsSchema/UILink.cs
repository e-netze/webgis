using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class UILinkCollection : NameUrl, ICreatable, IUI, IDisplayName, IEditable
{
    public UILinkCollection()
    {
        base.StoreUrl = false;
        base.ValidateUrl = false;
    }

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

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new NameUrlControl();
        ((NameUrlControl)ip).UrlIsVisible = false;

        ip.InitParameter = this;

        return ip;
    }

    #endregion
}

public class UILink : CopyableXml, ICreatable, IUI, IDisplayName, IEditable
{
    private string _link = String.Empty, _thumbnail = String.Empty, _description = String.Empty;
    private TableColumn.BrowserWindowProperties _browserWindowProps = new TableColumn.BrowserWindowProperties();

    public UILink()
    {
        base.StoreUrl = false;
        base.ValidateUrl = false;
    }

    #region Properties
    public string Link
    {
        get { return _link; }
        set { _link = value; }
    }

    [DisplayName("Browser Fenster Attribute")]
    //[TypeConverter(typeof(ExpandableObjectConverter))]
    public TableColumn.BrowserWindowProperties BrowserWindowProps
    {
        get { return _browserWindowProps; }
        set { _browserWindowProps = value; }
    }

    [Browsable(true)]
    [DisplayName("Vorschau Bild")]
    public string ThumbNail
    {
        get { return _thumbnail; }
        set { _thumbnail = value; }
    }

    [Browsable(true)]
    [DisplayName("Beschreibung")]
    [Editor(typeof(TypeEditor.MultilineStringEditor), typeof(TypeEditor.ITypeEditor))]
    public string Description
    {
        get { return _description; }
        set { _description = value; }
    }

    #endregion

    #region ICreatable Member

    override public string CreateAs(bool appendRoot)
    {
        return "s" + GuidEncoder.Encode(Guid.NewGuid());
    }

    override public Task<bool> CreatedAsync(string FullName)
    {
        return Task<bool>.FromResult(true);
    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new NameUrlControl();
        ((NameUrlControl)ip).UrlIsVisible = false;

        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region IPersistable
    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _link = (string)stream.Load("link", String.Empty);
        _thumbnail = (string)stream.Load("thumbnail", String.Empty);
        _description = (string)stream.Load("description", String.Empty);
        _browserWindowProps.Load(stream);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("link", _link);
        stream.Save("thumbnail", _thumbnail);
        stream.Save("description", _description);
        _browserWindowProps.Save(stream);
    }
    #endregion

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Link"; }
    }

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return this.Name; }
    }

    #endregion
}
