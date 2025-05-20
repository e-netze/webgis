//using E.Standard.CMS.Core;
//using E.Standard.ThreadSafe;
//using System;
//using System.Data;
//using System.Text;

//namespace E.Standard.WebGIS.CMS;

//public abstract class TabColumn : DataColumn
//{
//    private bool _showColNameWithHtml = true;
//    private bool _isHtmlHeader = false;
//    private bool _showInHtml = true;

//    protected TabColumn(string name, Type type)
//        : base(name, type)
//    {
//    }

//    #region Properties
//    public bool ShowColumnNameWithHtml
//    {
//        get { return _showColNameWithHtml; }
//        set { _showColNameWithHtml = value; }
//    }

//    public bool IsHtmlHeader
//    {
//        get { return _isHtmlHeader; }
//        set { _isHtmlHeader = value; }
//    }

//    public bool ShowInHtml
//    {
//        get { return _showInHtml; }
//        set { _showInHtml = value; }
//    }
//    #endregion

//    public abstract TabColumn Clone();

//    protected TabColumn CloneProperties(TabColumn proto)
//    {
//        if (proto != null)
//        {
//            _showColNameWithHtml = proto._showColNameWithHtml;
//            _isHtmlHeader = proto._isHtmlHeader;
//            _showInHtml = proto._showInHtml;
//        }
//        return this;
//    }
//}
//public class FieldTabColumn : TabColumn
//{
//    private string _fieldname, _simpleDomains;
//    private bool _intialSorting, _rawHtml = false;
//    private ThreadSafeDictionary<string, string> _domains = null;

//    public FieldTabColumn(string name, Type type, string fieldname, string simpleDomains, bool initialSorting, bool rawHtml)
//        : base(name, type)
//    {
//        _fieldname = fieldname;
//        _simpleDomains = simpleDomains;
//        _intialSorting = initialSorting;
//        _rawHtml = rawHtml;

//        if (!String.IsNullOrEmpty(_simpleDomains))
//        {
//            base.DataType = typeof(string);

//            _domains = new ThreadSafeDictionary<string, string>();
//            if (_simpleDomains.Contains("="))
//            {
//                int pos = _simpleDomains.IndexOf("=");
//                string[] left = _simpleDomains.Substring(0, pos).Trim().Split(',');
//                string[] right = _simpleDomains.Substring(pos + 1, _simpleDomains.Length - pos - 1).Trim().Split(',');

//                for (int i = 0; i < left.Length; i++)
//                {
//                    _domains.Add(left[i].Trim(), right.Length > i ? right[i].Trim() : left[i].Trim());
//                }
//            }
//        }
//    }

//    public string FieldName
//    {
//        get { return _fieldname; }
//    }

//    public string SimpleDomains
//    {
//        get { return _simpleDomains; }
//    }

//    public bool InitialSorting
//    {
//        get { return _intialSorting; }
//    }

//    public string ReplaceDomains(string val)
//    {
//        if (_domains == null || val == null)
//        {
//            return val;
//        }

//        if (_domains.ContainsKey(val.Trim()))
//        {
//            val = _domains[val.Trim()];
//        }

//        return val;
//    }

//    public bool RawHtml
//    {
//        get { return _rawHtml; }
//    }

//    public override TabColumn Clone()
//    {
//        return (new FieldTabColumn(this.ColumnName, this.DataType, FieldName, SimpleDomains, _intialSorting, _rawHtml)).CloneProperties(this);
//    }
//}

//public class DateTimeTabColumn : TabColumn
//{
//    private string _fieldName;
//    private DateFieldDisplayType _displayType = DateFieldDisplayType.Normal;

//    public DateTimeTabColumn(string name, Type type, string fieldName, DateFieldDisplayType displayType)
//        : base(name, type)
//    {
//        _fieldName = fieldName;
//        _displayType = displayType;
//    }

//    public string FieldName
//    {
//        get { return _fieldName; }
//    }

//    public DateFieldDisplayType DisplayType
//    {
//        get { return _displayType; }
//    }

//    public override TabColumn Clone()
//    {
//        return (new DateTimeTabColumn(this.ColumnName, this.DataType, this.FieldName, this.DisplayType)).CloneProperties(this);
//    }
//}

//public class MultiFieldTabColumn : TabColumn
//{
//    private string[] _fieldnames;
//    public MultiFieldTabColumn(string name, Type type, string[] fieldnames)
//        : base(name, type)
//    {
//        _fieldnames = fieldnames;
//    }

//    public string[] FieldNames
//    {
//        get { return _fieldnames; }
//    }

//    public override TabColumn Clone()
//    {
//        return (new MultiFieldTabColumn(this.ColumnName, this.DataType, FieldNames)).CloneProperties(this);
//    }
//}

//public class IdTabColumn : FieldTabColumn
//{
//    public IdTabColumn(string name, Type type, string fieldname)
//        : base(name, type, fieldname, String.Empty, false, false)
//    {
//    }

//    public override TabColumn Clone()
//    {
//        return (new IdTabColumn(this.ColumnName, this.DataType, FieldName)).CloneProperties(this);
//    }
//}

//public class ShapeTabColumn : TabColumn
//{
//    public ShapeTabColumn(string name)
//        : base(name, typeof(object))
//    {
//    }

//    public override TabColumn Clone()
//    {
//        return new ShapeTabColumn(this.ColumnName);
//    }
//}

///*
//public class CheckTabColumn : TabColumn
//{
//    public CheckTabColumn() 
//        : base("#CHECKED#",typeof(bool))
//    {
//        base.DefaultValue = true;
//    }

//    public override TabColumn Clone()
//    {
//        return new CheckTabColumn();
//    }
//}
//*/

//public class ExpressionTabColumn : FieldTabColumn
//{
//    private string _expression;

//    public ExpressionTabColumn(string name, Type type, string expression, bool initialSorting)
//        : base(name, type, expression, String.Empty, initialSorting, false)
//    {
//        _expression = expression;
//    }

//    new public string Expression
//    {
//        get { return _expression; }
//    }

//    public override TabColumn Clone()
//    {
//        return (new ExpressionTabColumn(this.ColumnName, this.DataType, Expression, this.InitialSorting)).CloneProperties(this);
//    }


//}

//public class HotlinkTabColumn : ExpressionTabColumn
//{
//    private string _hotlinkname, _url, _hotlinkId;
//    private bool _one2n = false;
//    private char _one2nSeperator = ';';
//    private string _hotlink = String.Empty;
//    private string _windowAttributes = String.Empty;
//    private ThreadSafeDictionary<string, string> _dict = null;
//    private BrowserWindowTarget _target = BrowserWindowTarget._blank;
//    private int _imgWidth, _imgHeight;
//    private string _imgExpression = String.Empty;

//    public HotlinkTabColumn(string name, string url, Type type, string expression, string hotlinkname)
//        : base(name, type, expression, false)
//    {
//        _hotlinkname = String.IsNullOrEmpty(hotlinkname) ? name : hotlinkname;
//        _url = url;
//    }

//    public string HotlinkName
//    {
//        get { return _hotlinkname; }
//    }
//    public string Url
//    {
//        get { return _url; }
//    }
//    public string HotlinkId
//    {
//        get { return _hotlinkId; }
//        set { _hotlinkId = value; }
//    }
//    public string Hotlink
//    {
//        get { return _hotlink; }
//        set { _hotlink = value; }
//    }
//    public bool One2N
//    {
//        get { return _one2n; }
//        set { _one2n = value; }
//    }
//    public char One2NSeperator
//    {
//        get { return _one2nSeperator; }
//        set { _one2nSeperator = value; }
//    }
//    public string WindowAttributes
//    {
//        get { return _windowAttributes; }
//    }
//    public BrowserWindowTarget Target
//    {
//        get { return _target; }
//        set { _target = value; }
//    }
//    public string ImageExpression
//    {
//        get { return _imgExpression; }
//        set { _imgExpression = value; }
//    }
//    public int ImageWidth
//    {
//        get { return _imgWidth; }
//        set { _imgWidth = value; }
//    }
//    public int ImageHeight
//    {
//        get { return _imgHeight; }
//        set { _imgHeight = value; }
//    }

//    static public string GetWindowAttributesFromNode(CmsNode node)
//    {
//        if (node == null)
//        {
//            return String.Empty;
//        }

//        int width = (int)node.Load("bwp_width", 0);
//        int height = (int)node.Load("bwp_height", 0);
//        YesNo titlebar = (YesNo)node.Load("bwp_titlebar", (int)YesNo.ignore);
//        YesNo toolbar = (YesNo)node.Load("bwp_toolbar", (int)YesNo.ignore);
//        YesNo scrollbars = (YesNo)node.Load("bwp_scrollbars", (int)YesNo.ignore);
//        YesNo resizable = (YesNo)node.Load("bwp_resizable", (int)YesNo.ignore);
//        YesNo location = (YesNo)node.Load("bwp_location", (int)YesNo.ignore);
//        YesNo menubar = (YesNo)node.Load("bwp_menubar", (int)YesNo.ignore);

//        StringBuilder sb = new StringBuilder();
//        if (width > 0)
//        {
//            Append(sb, "width=" + width);
//        }

//        if (height > 0)
//        {
//            Append(sb, "height=" + height);
//        }

//        if (titlebar != YesNo.ignore)
//        {
//            Append(sb, "tilebar=" + titlebar.ToString());
//        }

//        if (toolbar != YesNo.ignore)
//        {
//            Append(sb, "toolbar=" + toolbar.ToString());
//        }

//        if (scrollbars != YesNo.ignore)
//        {
//            Append(sb, "scrollbars=" + scrollbars.ToString());
//        }

//        if (resizable != YesNo.ignore)
//        {
//            Append(sb, "resizable=" + resizable.ToString());
//        }

//        if (location != YesNo.ignore)
//        {
//            Append(sb, "location=" + location.ToString());
//        }

//        if (menubar != YesNo.ignore)
//        {
//            Append(sb, "menubar=" + menubar.ToString());
//        }

//        return sb.ToString();
//    }

//    static private void Append(StringBuilder sb, string a)
//    {
//        if (sb.Length > 0)
//        {
//            sb.Append(",");
//        }

//        sb.Append(a);
//    }

//    public void WindowAttributesFromNode(CmsNode node)
//    {
//        _windowAttributes = GetWindowAttributesFromNode(node);
//    }

//    public string this[string key]
//    {
//        get
//        {
//            if (_dict == null || !_dict.ContainsKey(key))
//            {
//                return String.Empty;
//            }

//            return _dict[key];
//        }
//        set
//        {
//            if (_dict == null)
//            {
//                _dict = new ThreadSafeDictionary<string, string>();
//            }

//            _dict.Add(key, value);
//        }
//    }

//    public override TabColumn Clone()
//    {
//        HotlinkTabColumn clone = (HotlinkTabColumn)(new HotlinkTabColumn(this.ColumnName, _url, this.DataType, Expression, HotlinkName)).CloneProperties(this);
//        clone.One2N = this.One2N;
//        clone.One2NSeperator = this.One2NSeperator;
//        clone._windowAttributes = this._windowAttributes;
//        clone._target = this._target;

//        clone._hotlinkId = _hotlinkId;
//        clone._dict = _dict;

//        clone._imgWidth = this._imgWidth;
//        clone._imgHeight = this._imgHeight;
//        clone._imgExpression = this._imgExpression;
//        return clone;
//    }
//}

//public class ImageTabColumn : ExpressionTabColumn
//{
//    private int _iWidth, _iHeight;

//    public ImageTabColumn(string name, Type type, string expression, int width, int height)
//        : base(name, type, expression, false)
//    {
//        _iWidth = width;
//        _iHeight = height;
//    }

//    public int iWidth
//    {
//        get { return _iWidth; }
//        set { _iWidth = value; }
//    }
//    public int iHeight
//    {
//        get { return _iHeight; }
//        set { _iHeight = value; }
//    }
//    public override TabColumn Clone()
//    {
//        return (new ImageTabColumn(this.ColumnName, this.DataType, Expression, _iWidth, _iHeight)).CloneProperties(this);
//    }
//}
