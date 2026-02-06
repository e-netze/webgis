using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.Security;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.Extensions.Text;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.UI;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class GeoRssService : CopyableXml, ICreatable, IEditable, IDisplayName, IUI, IAuthentification
{
    private string _serviceUrl = "http://";
    private string _user = String.Empty, _pwd = String.Empty;
    private string _guid;
    private string _marker = "marker_rot.gif";
    private string _highlightMarker = "marker.gif";
    private int _updateDuration = 0;
    private GeoRssLabelMethod _labelMethod = GeoRssLabelMethod.Numbers;

    public GeoRssService()
    {
        base.StoreUrl = false;
        _guid = Guid.NewGuid().ToString("N").ToLower(); //GuidEncoder.Encode(Guid.NewGuid());
    }

    #region Properties
    [DisplayName("#service_url")]
    public string ServiceUrl
    {
        get { return _serviceUrl; }
        set { _serviceUrl = value; }
    }
    [DisplayName("#username")]
    [Category("#category_username")]
    public string Username
    {
        get { return _user; }
        set { _user = value; }
    }

    [DisplayName("#password")]
    [Category("#category_password")]
    [PasswordPropertyText(true)]
    public string Password
    {
        get { return _pwd; }
        set { _pwd = value; }
    }

    [DisplayName("#token")]
    [Category("#category_token")]
    [PasswordPropertyText(true)]
    [Editor(typeof(TypeEditor.TokenAuthentificationEditor), typeof(TypeEditor.ITypeEditor))]
    public string Token { get; set; }

    [DisplayName("#marker")]
    [Editor(typeof(TypeEditor.GeoRssMarkerEditor), typeof(TypeEditor.ITypeEditor))]
    [Category("#category_marker")]
    public string Marker
    {
        get { return _marker; }
        set { _marker = value; }
    }
    [Editor(typeof(TypeEditor.GeoRssMarkerEditor), typeof(TypeEditor.ITypeEditor))]
    [Category("#category_highlight_marker")]
    public string HighlightMarker
    {
        get { return _highlightMarker; }
        set { _highlightMarker = value; }
    }
    [Category("#category_label_method")]
    public GeoRssLabelMethod LabelMethod
    {
        get { return _labelMethod; }
        set { _labelMethod = value; }
    }
    [DisplayName("#update_duration")]
    public int UpdateDuration
    {
        get { return _updateDuration; }
        set { _updateDuration = value; }
    }
    #endregion

    #region ICreatable Member

    public override string CreateAs(bool appendRoot)
    {
        return this.Url;
    }

    public override Task<bool> CreatedAsync(string FullName)
    {
        return Task<bool>.FromResult(true);
    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        this.Create = create;

        IInitParameter ip = new GeoRssServiceControl();
        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region IPersistable Member

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _serviceUrl = (string)stream.Load("server", String.Empty);
        _guid = (string)stream.Load("guid", Guid.NewGuid().ToString("N").ToLower());
        _user = (string)stream.Load("user", String.Empty);
        _pwd = CmsCryptoHelper.Decrypt((string)stream.Load("pwd", String.Empty), "georssservice").Replace(stream.StringReplace);
        this.Token = CmsCryptoHelper.Decrypt((string)stream.Load("token", String.Empty), "georssservice").Replace(stream.StringReplace);
        _marker = (string)stream.Load("marker", "marker_rot.gif");
        _highlightMarker = (string)stream.Load("highlightmarker", "marker.gif");
        _updateDuration = (int)stream.Load("updateduration", 0);
        _labelMethod = (GeoRssLabelMethod)stream.Load("labelmethod", (int)GeoRssLabelMethod.Numbers);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("server", _serviceUrl);
        stream.Save("guid", _guid.ToString());
        stream.Save("user", _user);
        stream.Save("pwd", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(_pwd), "georssservice"));
        stream.Save("token", CmsCryptoHelper.Encrypt(stream.FireParseBoforeEncryptValue(this.Token), "georssservice"));
        stream.Save("marker", _marker);
        stream.Save("highlightmarker", _highlightMarker);
        stream.Save("updateduration", _updateDuration);
        stream.Save("labelmethod", (int)_labelMethod);
    }

    #endregion

    #region IDisplayName Member
    [Browsable(false)]
    public string DisplayName
    {
        get { return this.Name; }
    }

    #endregion

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "GeoRss Dienst"; }
    }

    protected override void BeforeCopy()
    {
        base.BeforeCopy();

        _guid = Guid.NewGuid().ToString("N").ToLower(); //GuidEncoder.Encode(Guid.NewGuid());
    }
}
