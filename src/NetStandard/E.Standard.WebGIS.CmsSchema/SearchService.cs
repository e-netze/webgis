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

public class SearchService : CopyableXml, IEditable, IUI, IDisplayName
{
    public SearchService()
    {
        this.Create = true;
    }

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        NameUrlControl ctrl = new NameUrlControl();
        ctrl.InitParameter = this;
        ctrl.NameIsVisible = true;

        return ctrl;
    }

    #endregion

    #region ICreatable Member

    override public string CreateAs(bool appendRoot)
    {
        return this.Url;
    }

    override public Task<bool> CreatedAsync(string FullName)
    {
        return Task<bool>.FromResult(true);
    }

    #endregion

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "SearchService"; }
    }

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return this.Name + " (" + this.Url + ")"; }
    }

    #endregion

    #region Properties

    private string _serviceUrl = String.Empty;

    [DisplayName("Service Url")]
    [Description("Die Url zum Suchdienst. Ist der Dienst ein geschützer LuceneServerNET Dienst kann die Authentifizierung über diese Url angefüft werden: https://client:secret@server.com")]
    public string ServiceUrl
    {
        get { return _serviceUrl; }
        set { _serviceUrl = value; }
    }

    [DisplayName("Optional: Indexname")]
    [Description("Der Indexname wird bei den meisten Suchdiensten schon über die Service Url definiert. Ausnahme sind Suchdienste vom Typ 'LuceneServerNET'. Hier ist die Angabe des Indexnamens pflicht.")]
    public string IndexName { get; set; }

    private SearchServiceTarget _target = SearchServiceTarget.Solr;
    public SearchServiceTarget Target
    {
        get { return _target; }
        set { _target = value; }
    }

    private string _suggestedText = "textsuggest";
    [Category("Attributes")]
    public string SuggestedText
    {
        get { return _suggestedText; }
        set { _suggestedText = value; }
    }

    private string _thumbnail = "thumbnail_url";
    [Category("Attributes")]
    public string Thumbnail
    {
        get { return _thumbnail; }
        set { _thumbnail = value; }
    }

    private string _geo = "geo";
    [Category("Attributes")]
    public string Geometry
    {
        get { return _geo; }
        set { _geo = value; }
    }

    private string _link = "";
    [Category("Attributes")]
    public string Link
    {
        get { return _link; }
        set { _link = value; }
    }

    private string _subtext = "subtext";
    [Category("Attributes")]
    public string Subtext
    {
        get { return _subtext; }
        set { _subtext = value; }
    }

    private int _rows = 5;
    [Category("Attributes")]
    public int Rows
    {
        get { return _rows; }
        set { _rows = value; }
    }

    private int _projId = -1;
    [DisplayName("Kartenprojektion")]
    [Category("Koordinatensystem")]
    //[Editor(typeof(TypeEditor.Proj4TypeEditor), typeof(TypeEditor.ITypeEditor))]
    public int ProjId
    {
        get { return _projId; }
        set { _projId = value; }
    }

    [Browsable(true)]
    [DisplayName("Copyright Info")]
    [Description("Gibt die Copyright Info an, die für diesen Dienst hinterlegt ist. Die Info muss unnter Sonstiges/Copyright definiert sein.")]
    [Category("Allgemein")]
    [Editor(typeof(TypeEditor.CopyrightInfoEditor), typeof(TypeEditor.ITypeEditor))]
    public string CopyrightInfo
    {
        get; set;
    }

    #endregion

    #region IPersistable

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        this.ServiceUrl = (string)stream.Load("serviceUrl", String.Empty);
        this.IndexName = (string)stream.Load("indexname", String.Empty);
        this.Target = (SearchServiceTarget)stream.Load("target", (int)SearchServiceTarget.Solr);

        this.SuggestedText = (string)stream.Load("suggestedtext", String.Empty);
        this.Thumbnail = (string)stream.Load("thumbnail", String.Empty);
        this.Geometry = (string)stream.Load("geo", String.Empty);
        this.Subtext = (string)stream.Load("subtext", String.Empty);
        this.Link = (string)stream.Load("link", String.Empty);
        this.Rows = (int)stream.Load("rows", 5);

        this.ProjId = (int)stream.Load("projid", -1);

        this.CopyrightInfo = (string)stream.Load("copyright", String.Empty);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("serviceUrl", this.ServiceUrl);
        if (!String.IsNullOrEmpty(this.IndexName))
        {
            stream.Save("indexname", this.IndexName);
        }

        stream.Save("target", (int)this.Target);

        stream.Save("suggestedtext", this.SuggestedText);
        stream.Save("thumbnail", this.Thumbnail);
        stream.Save("geo", this.Geometry);
        stream.Save("subtext", this.Subtext);
        stream.Save("link", this.Link);
        stream.Save("rows", this.Rows);

        stream.Save("projid", this.ProjId);

        stream.Save("copyright", this.CopyrightInfo ?? String.Empty);
    }

    #endregion
}
