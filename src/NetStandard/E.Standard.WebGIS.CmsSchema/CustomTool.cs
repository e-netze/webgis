using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CMS;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class CustomTool : CopyableXml, IEditable, IUI, ICreatable, IDisplayName, IUrlNode
{
    private string _img = String.Empty, _imgAct = String.Empty;
    private CustomToolType _type = CustomToolType.Button;
    private string _urlcommand = String.Empty;
    private string _container = "default";
    //private int _width = 0, _height = 0;
    private TableColumn.BrowserWindowProperties _browserWindowProps = new TableColumn.BrowserWindowProperties();
    private BrowserWindowTarget _target = BrowserWindowTarget._blank;

    public CustomTool()
    {
        this.ValidateUrl = false;
        base.StoreUrl = false;
    }

    #region Properties
    [Browsable(true)]
    [DisplayName("#image_url")]
    [Category("#category_image_url")]
    public string ImageUrl
    {
        get { return _img; }
        set { _img = value; }
    }
    [Browsable(true)]
    [DisplayName("#image_url_act")]
    [Category("#category_image_url_act")]
    public string ImageUrlAct
    {
        get { return _imgAct; }
        set { _imgAct = value; }
    }
    [Browsable(true)]
    [DisplayName("#container")]
    [Category("#category_container")]
    public string Container
    {
        get { return _container; }
        set { _container = value; }
    }

    [Browsable(true)]
    [DisplayName("#type")]
    [Category("#category_type")]
    public CustomToolType Type
    {
        get { return _type; }
        set { _type = value; }
    }
    [Browsable(true)]
    [DisplayName("#url_command")]
    [Category("#category_url_command")]
    public string UrlCommand
    {
        get { return _urlcommand; }
        set { _urlcommand = value; }
    }

    /*
    [Browsable(true)]
    [DisplayName("#width")]
    [Category("#category_width")]
    public int Width
    {
        get { return _width; }
        set { _width = value; }
    }
    [Browsable(true)]
    [DisplayName("#height")]
    [Category("#category_height")]
    public int Height
    {
        get { return _height; }
        set { _height = value; }
    }
    */

    [DisplayName("#browser_window_props")]
    [Category("#category_browser_window_props")]
    //[TypeConverter(typeof(ExpandableObjectConverter))]
    public TableColumn.BrowserWindowProperties BrowserWindowProps
    {
        get { return _browserWindowProps; }
        set { _browserWindowProps = value; }
    }
    [DisplayName("#target")]
    [Category("#category_target")]
    public BrowserWindowTarget Target
    {
        get { return _target; }
        set { _target = value; }
    }
    #endregion

    #region IPersistable Member

    override public void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _img = (string)stream.Load("img", String.Empty);
        _imgAct = (string)stream.Load("imgact", String.Empty);
        _type = (CustomToolType)stream.Load("type", (int)CustomToolType.Button);
        _urlcommand = (string)stream.Load("urlcommand", String.Empty);
        _container = (string)stream.Load("container", "default");

        _browserWindowProps.Load(stream);
        _target = (BrowserWindowTarget)stream.Load("target", (int)BrowserWindowTarget._blank);

        //_width = (int)stream.Load("width", _width);
        //_height = (int)stream.Load("height", _height);

        // Alte Werte auslesen
        int width = (int)stream.Load("width", 0);
        int height = (int)stream.Load("height", 0);
        if (width > 0)
        {
            _browserWindowProps.Width = width;
        }

        if (height > 0)
        {
            _browserWindowProps.Height = height;
        }
    }

    override public void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("img", _img);
        stream.Save("imgact", _imgAct);
        stream.Save("type", (int)_type);
        stream.Save("urlcommand", _urlcommand);
        stream.Save("container", _container);

        //stream.Save("width", _width);
        //stream.Save("height", _height);

        _browserWindowProps.Save(stream);
        stream.Save("target", (int)_target);
    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        base.Create = create;

        IInitParameter ip = new NameUrlControl();
        ((NameUrlControl)ip).UrlIsVisible = false;
        ip.InitParameter = this;

        return ip;
    }

    #endregion

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "CustomTool"; }
    }

    #region ICreatable Member

    override public string CreateAs(bool appendRoot)
    {
        return Crypto.GetID();
    }

    override public Task<bool> CreatedAsync(string FullName)
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
}
