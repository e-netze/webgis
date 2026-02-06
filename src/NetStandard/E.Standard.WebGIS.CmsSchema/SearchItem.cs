using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.UI;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

[Serializable]
public class SearchItem : CopyableXml, IUI, IEditable, IDisplayName, ICopyable
{
    private string _regEx = String.Empty;
    private string[] _fields = null;
    private LookUp _lookup;
    private int _minInputLength = 2;
    private bool _useLookup = false;
    //private bool _caseSensitiv = true;
    private bool _useUpper = false;
    private QueryMethod _method = QueryMethod.Exact;
    private bool _visible = true;
    private string _examples = String.Empty;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public SearchItem(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        base.StoreUrl = false;
        base.Create = true;
    }

    #region Properties

    [DisplayName("Suchfelder")]
    [Description("Felder in denen für diesen Ausdruck gesucht wird.")]
    [Category("Allgemein")]
    [Editor(typeof(TypeEditor.ThemeFieldsEditor), typeof(TypeEditor.ITypeEditor))]
    public string[] Fields
    {
        get { return _fields; }
        set { _fields = value; }
    }

    [DisplayName("#method")]
    [Category("#category_method")]
    public QueryMethod Method
    {
        get { return _method; }
        set { _method = value; }
    }

    [DisplayName("#visible")]
    [Category("#category_visible")]
    public bool Visible
    {
        get { return _visible; }
        set { _visible = value; }
    }

    [DisplayName("#required")]
    [Category("#category_required")]
    public bool Required { get; set; }

    [DisplayName("#examples")]
    [Category("~#category_examples")]
    public string Examples
    {
        get { return _examples; }
        set { _examples = value; }
    }

    [DisplayName("#regular_expression")]
    [Category("~#category_regular_expression")]
    [Editor(typeof(TypeEditor.RegExTypeEditor), typeof(TypeEditor.ITypeEditor))]
    public string RegularExpression
    {
        get { return _regEx; }
        set { _regEx = value; }
    }

    [DisplayName("#format_expression")]
    [Category("~#category_format_expression")]
    public string FormatExpression { get; set; }

    //[DisplayName("Case-Sesitive abfragen")]
    //[Category("Allgemein")]
    //public bool CaseSensitiv
    //{
    //    get { return _caseSensitiv; }
    //    set { _caseSensitiv = value; }
    //}



    [DisplayName("#look_up")]
    [Category("~#category_look_up")]
    [Editor(typeof(TypeEditor.LookUpEditor), typeof(TypeEditor.ITypeEditor))]
    public LookUp LookUp
    {
        get { return _lookup; }
        set { _lookup = value; }
    }

    [DisplayName("#use_look_up")]
    [Category("~#category_use_look_up")]
    public bool UseLookUp
    {
        get { return _useLookup; }
        set { _useLookup = value; }
    }

    [DisplayName("#min_input_length")]
    [Category("~#category_min_input_length")]
    public int MinInputLength
    {
        get { return _minInputLength; }
        set { _minInputLength = value; }
    }

    [Browsable(true)]
    [DisplayName("#sql_injection_white_list")]
    [Category("~#category_sql_injection_white_list")]
    public string SqlInjectionWhiteList
    {
        get; set;
    }

    [Browsable(true)]
    [DisplayName("#ignore_in_preview_text")]
    [Category("#category_ignore_in_preview_text")]
    public bool IgnoreInPreviewText
    {
        get; set;
    }

    [DisplayName("#use_upper")]
    [Category("~~#category_use_upper")]
    public bool UseUpper
    {
        get { return _useUpper; }
        set { _useUpper = value; }
    }
    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new NewSearchItemControl(_servicePack, this);
        ip.InitParameter = this;

        return ip;
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

        _fields = Helper.StringToArray((string)stream.Load("fields", String.Empty));
        _method = (QueryMethod)stream.Load("method", (int)QueryMethod.Exact);
        _regEx = (string)stream.Load("regex", String.Empty);
        FormatExpression = (string)stream.Load("format_expression", String.Empty);

        string lookup_connectionstring = (string)stream.Load("lookup_connectionstring", String.Empty);
        string lookup_sqlclause = (string)stream.Load("lookup_sqlclause", String.Empty);
        if (!String.IsNullOrEmpty(lookup_connectionstring) ||
            !String.IsNullOrEmpty(lookup_sqlclause))
        {
            _lookup = new LookUp(lookup_connectionstring, lookup_sqlclause);
        }
        else
        {
            _lookup = null;
        }

        _useLookup = (bool)stream.Load("uselookup", _lookup != null);
        _minInputLength = (int)stream.Load("mininputlength", 2);

        //_caseSensitiv = (bool)stream.Load("casesens", true);
        _useUpper = (bool)stream.Load("useupper", false);

        _visible = (bool)stream.Load("visible", true);
        this.Required = (bool)stream.Load("required", false);
        _examples = (string)stream.Load("examples", String.Empty);

        this.SqlInjectionWhiteList = (string)stream.Load("sqlinjectionwhitelist", string.Empty);

        this.IgnoreInPreviewText = (bool)stream.Load("ignore_in_preview_text", false);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("fields", Helper.ArrayToString(_fields));
        stream.Save("method", (int)_method);
        stream.Save("regex", _regEx);
        stream.SaveOrRemoveIfEmpty("format_expression", FormatExpression);

        if (_lookup != null)
        {
            stream.Save("lookup_connectionstring", _lookup.ConnectionString);
            stream.Save("lookup_sqlclause", _lookup.SqlClause);
        }

        stream.Save("uselookup", _useLookup);
        stream.Save("mininputlength", _minInputLength);

        //stream.Save("casesens", _caseSensitiv);
        stream.Save("useupper", _useUpper);

        stream.Save("visible", _visible);
        stream.Save("required", this.Required);
        stream.Save("examples", _examples);

        //if (!String.IsNullOrWhiteSpace(this.SqlInjectionWhiteList))
        stream.Save("sqlinjectionwhitelist", this.SqlInjectionWhiteList?.Trim() ?? String.Empty);

        if (this.IgnoreInPreviewText)
        {
            stream.Save("ignore_in_preview_text", true);
        }
    }

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Suchbegriff"; }
    }
}
