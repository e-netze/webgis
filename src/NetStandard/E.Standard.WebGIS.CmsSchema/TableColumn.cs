using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Reflection;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CMS;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

[CmsUI(PrimaryDisplayProperty = "Data.FieldName")]
public class TableColumn : CopyableXml, IUI, IEditable, IDisplayName, IForcePerist
{
    private ColumnType _colType = ColumnType.Field;
    private FieldData _fieldData = new FieldDataField();
    private bool _silentChanges = false;
    private bool _showColNameWithHtml = true;
    private bool _isHtmlHeader = false;
    private bool _showInHtml = true;
    private bool _sort = false;

    public TableColumn()
    {
        base.StoreUrl = false;
        base.ValidateUrl = false;

        this.Visible = true;
    }

    #region Properties

    [DisplayName("#column_type")]
    [Category("#category_column_type")]
    public ColumnType ColumnType
    {
        get
        {
            return _colType;
        }
        set
        {
            FieldData data = null;
            switch (value)
            {
                case ColumnType.Field:
                case ColumnType.EmailAddress:
                case ColumnType.PhoneNumber:
                    data = new FieldDataField();
                    break;
                case ColumnType.Hotlink:
                    data = new FieldDataHotlink();
                    break;
                case ColumnType.Expression:
                    data = new FieldDataExpression();
                    break;
                case ColumnType.ImageExpression:
                    data = new FieldDataImage();
                    break;
                case ColumnType.MultiField:
                    data = new MultiFieldDataField();
                    break;
                case ColumnType.DateTime:
                    data = new DateTimeDataField();
                    break;
            }
            if (data != null)
            {
                data.CmsManager = this.CmsManager;
                data.RelativePath = this.RelativePath;
            }

            if (_fieldData == null)
            {
                _fieldData = data;
            }
            else if (!_fieldData.GetType().Equals(data.GetType()))
            {
                if (_silentChanges == false)
                {
                    //if (MessageBox.Show("Durch das Änderen des Feld-Types gehen die Information von '" + _fieldData.GetType() + "' verloren.\nWollen Sie den Vorgang vortsetzen?", "Warnung", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        _fieldData = data;
                    }
                    //else
                    //{
                    //    return;
                    //}
                }
                else
                {
                    _fieldData = data;
                }
            }
            _colType = value;
        }
    }

    [DisplayName("#data")]
    [Category("#category_data")]
    //[TypeConverter(typeof(ExpandableObjectConverter))]
    public FieldData Data
    {
        get { return _fieldData; }
        set
        {
            switch (ColumnType)
            {
                case ColumnType.Field:
                case ColumnType.EmailAddress:
                case ColumnType.PhoneNumber:
                    if (value is FieldDataField)
                    {
                        _fieldData = value;
                    }

                    break;
                case ColumnType.Hotlink:
                    if (value is FieldDataHotlink)
                    {
                        _fieldData = value;
                    }

                    break;
                case ColumnType.Expression:
                    if (value is FieldDataExpression)
                    {
                        _fieldData = value;
                    }

                    break;
                case ColumnType.ImageExpression:
                    if (value is FieldDataImage)
                    {
                        _fieldData = value;
                    }

                    break;
                case ColumnType.MultiField:
                    if (value is MultiFieldDataField)
                    {
                        _fieldData = value;
                    }

                    break;
                case ColumnType.DateTime:
                    if (value is DateTimeDataField)
                    {
                        _fieldData = value;
                    }

                    break;
            }
        }
    }

    [DisplayName("#visible")]
    [Category("#category_visible")]
    public bool Visible { get; set; }

    [Category("~#category_show_column_name_with_html")]
    [DisplayName("#show_column_name_with_html")]
    public bool ShowColumnNameWithHtml
    {
        get { return _showColNameWithHtml; }
        set { _showColNameWithHtml = value; }
    }

    [Category("~#category_is_html_header")]
    [DisplayName("#is_html_header")]
    public bool IsHtmlHeader
    {
        get { return _isHtmlHeader; }
        set { _isHtmlHeader = value; }
    }

    [Category("~#category_show_in_html")]
    [DisplayName("#show_in_html")]
    public bool ShowInHtml
    {
        get { return _showInHtml; }
        set { _showInHtml = value; }
    }

    [Category("~#category_sort")]
    [DisplayName("#sort")]
    public bool Sort
    {
        get { return _sort; }
        set { _sort = value; }
    }

    #endregion

    internal bool SilentChanges
    {
        get { return _silentChanges; }
        set { _silentChanges = value; }
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

    override public string CreateAs(bool appendRoot)
    {
        return "s" + GuidEncoder.Encode(Guid.NewGuid()); //Guid.NewGuid().ToString("N");
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

    #region IPersistable
    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _colType = (ColumnType)stream.Load("columntype", (int)ColumnType.Field);
        _isHtmlHeader = (bool)stream.Load("ishtmlheader", false);
        _showColNameWithHtml = (bool)stream.Load("showcolhtml", true);
        _showInHtml = (bool)stream.Load("showhtml", true);
        _sort = (bool)stream.Load("sort", false);

        this.Visible = (bool)stream.Load("visible", true);

        _fieldData = null;
        switch (_colType)
        {
            case ColumnType.Field:
            case ColumnType.EmailAddress:
            case ColumnType.PhoneNumber:
                _fieldData = new FieldDataField();
                break;
            case ColumnType.Hotlink:
                _fieldData = new FieldDataHotlink();
                break;
            case ColumnType.Expression:
                _fieldData = new FieldDataExpression();
                break;
            case ColumnType.ImageExpression:
                _fieldData = new FieldDataImage();
                break;
            case ColumnType.MultiField:
                _fieldData = new MultiFieldDataField();
                break;
            case ColumnType.DateTime:
                _fieldData = new DateTimeDataField();
                break;
        }

        if (_fieldData != null)
        {
            _fieldData.Load(stream);
            _fieldData.CmsManager = this.CmsManager;
            _fieldData.RelativePath = this.RelativePath;
        }
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("columntype", (int)_colType);
        stream.Save("ishtmlheader", _isHtmlHeader);
        stream.Save("showcolhtml", _showColNameWithHtml);
        stream.Save("showhtml", _showInHtml);
        stream.Save("sort", _sort);

        if (this.Visible == false)
        {
            stream.Save("visible", this.Visible);
        }

        if (_fieldData != null)
        {
            _fieldData.Save(stream);
        }
    }
    #endregion

    #region ISchemaNode
    public override CMSManager CmsManager
    {
        get
        {
            return base.CmsManager;
        }
        set
        {
            base.CmsManager = value;
            if (_fieldData != null)
            {
                _fieldData.CmsManager = value;
            }
        }
    }
    public override string RelativePath
    {
        get
        {
            return base.RelativePath;
        }
        set
        {
            base.RelativePath = value;
            if (_fieldData != null)
            {
                _fieldData.RelativePath = value;
            }
        }
    }
    #endregion

    #region HelperClasses
    public class FieldData : SchemaNode, IPersistable
    {
        #region IPersistable Member

        virtual public void Load(IStreamDocument stream)
        {
        }

        virtual public void Save(IStreamDocument stream)
        {
        }

        #endregion
    }

    public class FieldDataField : FieldData
    {
        private string _fieldName = String.Empty, _simpledomains = String.Empty;
        private bool _rawHtml = false;

        #region Properties
        [DisplayName("#field_name")]
        [Editor(typeof(TypeEditor.ThemeFieldsEditor), typeof(TypeEditor.ITypeEditor))]
        public string FieldName
        {
            get { return _fieldName; }
            set { _fieldName = value; }
        }

        [DisplayName("#simple_domains")]
        public string SimpleDomains
        {
            get { return _simpledomains; }
            set { _simpledomains = value; }
        }

        [DisplayName("#raw_html")]
        public bool RawHtml
        {
            get { return _rawHtml; }
            set { _rawHtml = value; }
        }

        [DisplayName("#sorting_algorithm")]
        [Category("#category_sorting_algorithm")]
        public string SortingAlgorithm { get; set; }


        [DisplayName("#auto_sort")]
        [Category("#category_auto_sort")]
        public FieldAutoSortMethod AutoSort { get; set; }

        #endregion

        #region IPersistable

        public override void Load(IStreamDocument stream)
        {
            base.Load(stream);

            _fieldName = (string)stream.Load("fieldname", String.Empty);
            _simpledomains = (string)stream.Load("simpledomains", String.Empty);

            this.SortingAlgorithm = (string)stream.Load("sorting_alg", String.Empty);
            this.AutoSort = (FieldAutoSortMethod)stream.Load("auto_sort", (int)FieldAutoSortMethod.None);

            _rawHtml = (bool)stream.Load("rawhtml", false);
        }

        public override void Save(IStreamDocument stream)
        {
            base.Save(stream);

            stream.Save("fieldname", _fieldName);
            stream.Save("simpledomains", _simpledomains);

            stream.SaveOrRemoveIfEmpty("sorting_alg", this.SortingAlgorithm);
            if (this.AutoSort != FieldAutoSortMethod.None)
            {
                stream.Save("auto_sort", (int)this.AutoSort);
            }

            if (_rawHtml == true)
            {
                stream.Save("rawhtml", _rawHtml);
            }
        }

        #endregion

        public override string ToString()
        {
            return "Feld:" + _fieldName;
        }
    }

    public class MultiFieldDataField : FieldData
    {
        private string[] _fieldNames = null;

        #region Properties
        [DisplayName("Feld Names")]
        [Editor(typeof(TypeEditor.ThemeFieldsEditor), typeof(TypeEditor.ITypeEditor))]
        public string[] FieldNames
        {
            get { return _fieldNames; }
            set { _fieldNames = value; }
        }
        #endregion

        #region IPersistable
        public override void Load(IStreamDocument stream)
        {
            base.Load(stream);

            _fieldNames = Helper.StringToArray((string)stream.Load("fieldnames", String.Empty));
        }

        public override void Save(IStreamDocument stream)
        {
            base.Save(stream);

            stream.Save("fieldnames", Helper.ArrayToString(_fieldNames));
        }
        #endregion

        public override string ToString()
        {
            return "Felds:" + Helper.ArrayToString(_fieldNames);
        }
    }

    public class DateTimeDataField : FieldData
    {
        private string _fieldName = String.Empty;
        private DateFieldDisplayType _displayType = DateFieldDisplayType.Normal;

        #region Properties

        [DisplayName("#field_name")]
        [Editor(typeof(TypeEditor.ThemeFieldsEditor), typeof(TypeEditor.ITypeEditor))]
        public string FieldName
        {
            get { return _fieldName; }
            set { _fieldName = value; }
        }

        public DateFieldDisplayType DisplayType
        {
            get { return _displayType; }
            set { _displayType = value; }
        }

        [DisplayName("#format_string")]
        public string FormatString { get; set; }

        [DisplayName("#sorting_algorithm")]
        [Category("#category_sorting_algorithm")]
        public string SortingAlgorithm { get; set; }

        #endregion

        #region IPersistable
        public override void Load(IStreamDocument stream)
        {
            base.Load(stream);

            _fieldName = (string)stream.Load("fieldname", String.Empty);
            _displayType = (DateFieldDisplayType)stream.Load("displaytype", (int)DateFieldDisplayType.Normal);
            this.FormatString = (string)stream.Load("date_formatstring", String.Empty);
            this.SortingAlgorithm = (string)stream.Load("sorting_alg", String.Empty);
        }

        public override void Save(IStreamDocument stream)
        {
            base.Save(stream);

            stream.Save("fieldname", _fieldName ?? String.Empty);
            stream.Save("displaytype", (int)_displayType);
            stream.Save("date_formatstring", this.FormatString ?? String.Empty);
            stream.SaveOrRemoveIfEmpty("sorting_alg", this.SortingAlgorithm);

        }
        #endregion
    }

    public class FieldDataHotlink : FieldData
    {
        private string _hotlinkUrl = String.Empty;
        private string _hotlinkName = String.Empty;
        private bool _one2n = false;
        private char _one2nSeperator = ';';
        private BrowserWindowProperties _browserWindowProps = new BrowserWindowProperties();
        private BrowserWindowTarget _target = BrowserWindowTarget._blank;

        private string _imgExpresson = String.Empty;
        private int _imgwidth = 0;
        private int _imgheight = 0;

        #region Properties
        [DisplayName("#hotlink_url")]
        public string HotlinkUrl
        {
            get { return _hotlinkUrl; }
            set { _hotlinkUrl = value; }
        }

        [DisplayName("#hotlink_name")]
        public string HotlinkName
        {
            get { return _hotlinkName; }
            set { _hotlinkName = value; }
        }

        [DisplayName("#one2_n")]
        public bool One2N
        {
            get { return _one2n; }
            set { _one2n = value; }
        }

        [DisplayName("#one2_n_seperator")]
        public char One2NSeperator
        {
            get { return _one2nSeperator; }
            set { _one2nSeperator = value; }
        }

        [DisplayName("#browser_window_props")]
        //[TypeConverter(typeof(ExpandableObjectConverter))]
        public BrowserWindowProperties BrowserWindowProps
        {
            get { return _browserWindowProps; }
            set { _browserWindowProps = value; }
        }

        [DisplayName("#target")]
        public BrowserWindowTarget Target
        {
            get { return _target; }
            set { _target = value; }
        }

        [DisplayName("#image_expression")]
        public string ImageExpression
        {
            get { return _imgExpresson; }
            set { _imgExpresson = value; }
        }
        [DisplayName("#i_width")]
        public int IWidth
        {
            get { return _imgwidth; }
            set { _imgwidth = value; }
        }
        [DisplayName("#i_height")]
        public int IHeight
        {
            get { return _imgheight; }
            set { _imgheight = value; }
        }
        #endregion

        #region IPersistable
        public override void Load(IStreamDocument stream)
        {
            base.Load(stream);

            _hotlinkUrl = (string)stream.Load("hotlinkurl", String.Empty);
            _hotlinkName = (string)stream.Load("hotlinkname", String.Empty);
            _one2n = (bool)stream.Load("one2n", false);
            _one2nSeperator = Convert.ToChar(stream.Load("one2nseperator", ';'));

            _browserWindowProps.Load(stream);
            _target = (BrowserWindowTarget)stream.Load("target", (int)BrowserWindowTarget._blank);

            _imgExpresson = (string)stream.Load("imgexpression", String.Empty);
            _imgwidth = (int)stream.Load("imgwidth", 0);
            _imgheight = (int)stream.Load("imgheight", 0);
        }

        public override void Save(IStreamDocument stream)
        {
            base.Save(stream);

            stream.Save("hotlinkurl", _hotlinkUrl);
            stream.Save("hotlinkname", _hotlinkName);
            stream.Save("one2n", _one2n);
            stream.Save("one2nseperator", _one2nSeperator);

            _browserWindowProps.Save(stream);
            stream.Save("target", (int)_target);

            stream.Save("imgexpression", _imgExpresson);
            stream.Save("imgwidth", _imgwidth);
            stream.Save("imgheight", _imgheight);
        }
        #endregion

        public override string ToString()
        {
            if (_hotlinkUrl == null)
            {
                return "Hotlink:";
            }

            return "Hotlink:" +
                           ((_hotlinkUrl.Length > 11) ? _hotlinkUrl.Substring(0, 10) + "..." : _hotlinkUrl);
        }

    }

    public class FieldDataExpression : FieldData
    {
        private string _expresson = String.Empty;
        private ColumnDataType _colDataType = ColumnDataType.String;

        #region Properties
        [DisplayName("#expression")]
        public string Expression
        {
            get { return _expresson; }
            set { _expresson = value; }
        }

        [DisplayName("#column_data_type")]
        public ColumnDataType ColumnDataType
        {
            get { return _colDataType; }
            set { _colDataType = value; }
        }
        #endregion

        #region IPersistable
        public override void Load(IStreamDocument stream)
        {
            base.Load(stream);

            _expresson = (string)stream.Load("expression", String.Empty);
            _colDataType = (ColumnDataType)stream.Load("coldatatype", (int)ColumnDataType.String);
        }

        public override void Save(IStreamDocument stream)
        {
            base.Save(stream);

            stream.Save("expression", _expresson);
            stream.Save("coldatatype", (int)_colDataType);
        }
        #endregion

        public override string ToString()
        {
            return "Ausdruck:" +
                ((_expresson.Length > 11) ? _expresson.Substring(0, 10) + "..." : _expresson);
        }
    }

    public class FieldDataImage : FieldData
    {
        private string _imgExpresson = String.Empty;
        private int _iwidth = 0;
        private int _iheight = 0;

        #region Properties
        [DisplayName("#image_expression")]
        public string ImageExpression
        {
            get { return _imgExpresson; }
            set { _imgExpresson = value; }
        }
        [DisplayName("#i_width")]
        public int IWidth
        {
            get { return _iwidth; }
            set { _iwidth = value; }
        }
        [DisplayName("#i_height")]
        public int IHeight
        {
            get { return _iheight; }
            set { _iheight = value; }
        }
        #endregion

        #region IPersistable
        public override void Load(IStreamDocument stream)
        {
            base.Load(stream);

            _imgExpresson = (string)stream.Load("imgexpression", String.Empty);
            _iwidth = (int)stream.Load("iwidth", 0);
            _iheight = (int)stream.Load("iheight", 0);
        }

        public override void Save(IStreamDocument stream)
        {
            base.Save(stream);

            stream.Save("imgexpression", _imgExpresson);
            stream.Save("iwidth", _iwidth);
            stream.Save("iheight", _iheight);
        }
        #endregion

        public override string ToString()
        {
            return "Ausdruck:" +
                ((_imgExpresson.Length > 11) ? _imgExpresson.Substring(0, 10) + "..." : _imgExpresson);
        }
    }

    public class BrowserWindowProperties : IPersistable
    {
        private YesNo _titlebar = YesNo.ignore;
        private YesNo _toolbar = YesNo.ignore;
        private YesNo _scrollbars = YesNo.ignore;
        private YesNo _resizable = YesNo.ignore;
        private YesNo _location = YesNo.ignore;
        private YesNo _menubar = YesNo.ignore;
        private int _width = 0, _height = 0;

        #region Properties
        public YesNo TitleBar
        {
            get { return _titlebar; }
            set { _titlebar = value; }
        }
        public YesNo ToolBar
        {
            get { return _toolbar; }
            set { _toolbar = value; }
        }
        public YesNo ScrollBars
        {
            get { return _scrollbars; }
            set { _scrollbars = value; }
        }
        public YesNo Resizable
        {
            get { return _resizable; }
            set { _resizable = value; }
        }
        public YesNo Location
        {
            get { return _location; }
            set { _location = value; }
        }
        public YesNo MenuBar
        {
            get { return _menubar; }
            set { _menubar = value; }
        }
        public int Width
        {
            get { return _width; }
            set { _width = value; }
        }
        public int Height
        {
            get { return _height; }
            set { _height = value; }
        }
        #endregion

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (_width > 0)
            {
                Append(sb, "width=" + _width);
            }

            if (_height > 0)
            {
                Append(sb, "height=" + _height);
            }

            if (_titlebar != YesNo.ignore)
            {
                Append(sb, "tilebar=" + _titlebar.ToString());
            }

            if (_toolbar != YesNo.ignore)
            {
                Append(sb, "toolbar=" + _toolbar.ToString());
            }

            if (_scrollbars != YesNo.ignore)
            {
                Append(sb, "scrollbars=" + _scrollbars.ToString());
            }

            if (_resizable != YesNo.ignore)
            {
                Append(sb, "resizable=" + _resizable.ToString());
            }

            if (_location != YesNo.ignore)
            {
                Append(sb, "location=" + _location.ToString());
            }

            if (_menubar != YesNo.ignore)
            {
                Append(sb, "menubar=" + _menubar.ToString());
            }

            return sb.ToString();
        }

        private void Append(StringBuilder sb, string a)
        {
            if (sb.Length > 0)
            {
                sb.Append(",");
            }

            sb.Append(a);
        }

        #region IPersistable Member

        public void Load(IStreamDocument stream)
        {
            _titlebar = (YesNo)stream.Load("bwp_titlebar", (int)YesNo.ignore);
            _toolbar = (YesNo)stream.Load("bwp_toolbar", (int)YesNo.ignore);
            _scrollbars = (YesNo)stream.Load("bwp_scrollbars", (int)YesNo.ignore);
            _resizable = (YesNo)stream.Load("bwp_resizable", (int)YesNo.ignore);
            _location = (YesNo)stream.Load("bwp_location", (int)YesNo.ignore);
            _menubar = (YesNo)stream.Load("bwp_menubar", (int)YesNo.ignore);
            _width = (int)stream.Load("bwp_width", 0);
            _height = (int)stream.Load("bwp_height", 0);
        }

        public void Save(IStreamDocument stream)
        {
            stream.Save("bwp_titlebar", (int)_titlebar);
            stream.Save("bwp_toolbar", (int)_toolbar);
            stream.Save("bwp_scrollbars", (int)_scrollbars);
            stream.Save("bwp_resizable", (int)_resizable);
            stream.Save("bwp_location", (int)_location);
            stream.Save("bwp_menubar", (int)_menubar);
            stream.Save("bwp_width", _width);
            stream.Save("bwp_height", _height);
        }

        #endregion
    }
    #endregion

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Tabellenspalte"; }
    }

    [Browsable(false)]
    public bool AlwaysForcePersitForInstance => true;  // Damit FiedData immer richtig zugewiesen ist
}
