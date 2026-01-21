using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace E.Standard.WebMapping.Core.Filters;

public class QueryFilter
{
    protected string _where = String.Empty, _subfields = "*", _orderBy = "";
    protected int _featureLimit = -1, _beginRecord = -1;
    protected SpatialReference _featureSr = null;
    protected BufferFilter _buffer = null;
    protected bool _queryGeometry = false;
    protected string _idFieldName = String.Empty;
    private readonly List<long> _plus = new List<long>();
    private readonly List<long> _minus = new List<long>();

    public QueryFilter(string idFieldName, int featureLimit, int beginRecord)
    {
        _idFieldName = idFieldName;
        _featureLimit = featureLimit;
        _beginRecord = beginRecord;
    }
    public QueryFilter(string idFieldName, string where, int featureLimit, int beginRecord)
        : this(idFieldName, featureLimit, beginRecord)
    {
        _where = where;
    }
    internal QueryFilter(QueryFilter query)
    {
        _where = query._where;
        _idFieldName = query._idFieldName;
        _subfields = query._subfields;
        _featureLimit = query._featureLimit;
        _beginRecord = query._beginRecord;
        _featureSr = (query._featureSr == null) ? null : query._featureSr.Clone();
        _buffer = (query._buffer == null) ? null : query._buffer.Clone();
        _queryGeometry = query._queryGeometry;

        foreach (long p in query._plus)
        {
            _plus.Add(p);
        }

        foreach (long m in query._minus)
        {
            _minus.Add(m);
        }

        _cmsQueryNodeCollection = query._cmsQueryNodeCollection;

        this.SuppressResolveAttributeDomains = query.SuppressResolveAttributeDomains;
        this.TimeEpoch = query.TimeEpoch;
    }

    public void Plus(long featureId)
    {
        if (_minus.Contains(featureId))
        {
            _minus.Remove(featureId);
        }

        if (!_plus.Contains(featureId))
        {
            _plus.Add(featureId);
        }
    }
    public void Minus(long featureId)
    {
        if (_plus.Contains(featureId))
        {
            _plus.Remove(featureId);
        }

        if (!_minus.Contains(featureId))
        {
            _minus.Add(featureId);
        }
    }
    public void ClearPlusMinus()
    {
        _plus.Clear();
        _minus.Clear();
    }

    public string SubFields
    {
        get { return _subfields; }
        set { _subfields = value; }
    }

    virtual public string Where
    {
        get
        {
            if (!String.IsNullOrEmpty(_idFieldName) &&
                (_plus.Count > 0 ||
                 _minus.Count > 0))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(_where);

                #region Plus

                StringBuilder plusSb = new StringBuilder();
                foreach (long p in _plus)
                {
                    if (_minus.Contains(p))
                    {
                        continue;
                    }

                    if (plusSb.Length > 0)
                    {
                        plusSb.Append(",");
                    }

                    plusSb.Append(p);
                }
                if (plusSb.Length > 0)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(" OR ");
                    }

                    sb.Append(_idFieldName + " in (" + plusSb.ToString() + ")");
                }

                #endregion

                #region Old Plus Implentation

                /*foreach (long p in _plus)
                {
                    if (sb.Length > 0) sb.Append(" OR ");
                    sb.Append(_idFieldName + "=" + p);  
                }* */

                #endregion

                #region Minus

                foreach (long m in _minus)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(" AND ");
                    }

                    sb.Append(_idFieldName + "<>" + m);
                }

                #endregion

                return sb.ToString();
            }
            return _where;
        }
        set { _where = value; }
    }
    public bool QueryGeometry
    {
        get { return _queryGeometry; }
        set { _queryGeometry = value; }
    }
    public BufferFilter Buffer
    {
        get { return _buffer; }
        set { _buffer = value; }
    }

    public int FeatureLimit
    {
        get { return _featureLimit; }
        set { _featureLimit = value; }
    }

    public string OrderBy
    {
        get { return _orderBy; }
        set { _orderBy = value; }
    }
    public int BeginRecord
    {
        get { return _beginRecord; }
        set { _beginRecord = value; }
    }

    public bool SuppressResolveAttributeDomains { get; set; }

    public SpatialReference FeatureSpatialReference
    {
        get { return _featureSr; }
        set { _featureSr = value; }
    }

    public TimeEpochDefinition TimeEpoch { get; set; }

    virtual public string ArcXML(NumberFormatInfo nfi)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("<QUERY");
        if (!String.IsNullOrEmpty(_where))
        {
            sb.Append(" where=\"" + EncodeXmlString(this.Where) + "\"");
        }

        sb.Append(" subfields=\"" +
            ((_subfields == "*" || String.IsNullOrEmpty(_subfields)) ? "#ALL#" : _subfields) +
            "\">");

        if (_featureSr != null)
        {
            sb.Append("<FEATURECOORDSYS id='" + _featureSr.Id + "' />");
        }

        if (_buffer != null)
        {
            sb.Append(_buffer.ArcXML(nfi));
        }

        sb.Append("</QUERY>");

        return sb.ToString();
    }

    virtual public QueryFilter Clone()
    {
        return new QueryFilter(this);
    }

    virtual public QueryFilter CloneTo(Type type)
    {
        if (type.Equals(typeof(SpatialFilter)))
        {
            return new SpatialFilter(this);
        }

        if (type.Equals(typeof(QueryFilter)))
        {
            return new QueryFilter(this);
        }

        return Clone();
    }

    protected string EncodeXmlString(string str)
    {
        return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    public static void SetFeatureFilterCoordsys(IMap map, QueryFilter filter, ILayer layer)
    {
        if (map == null || filter == null || layer == null)
        {
            return;
        }

        IMapService service = map.ServiceByLayer(layer);
        if (service == null)
        {
            return;
        }

        SetFeatureFilterCoordsys(map, service, filter);
    }

    private static void SetFeatureFilterCoordsys(IMap map, IMapService service, QueryFilter filter)
    {
        if (service is IMapServiceProjection)
        {
            switch (((IMapServiceProjection)service).ProjectionMethode)
            {
                case ServiceProjectionMethode.Map:
                    filter.FeatureSpatialReference = map.SpatialReference;
                    if (filter is SpatialFilter)
                    {
                        ((SpatialFilter)filter).FilterSpatialReference = filter.FeatureSpatialReference;
                    }

                    break;
                case ServiceProjectionMethode.Userdefined:
                    filter.FeatureSpatialReference = CoreApiGlobals.SRefStore.SpatialReferences.ById(((IMapServiceProjection)service).ProjectionId);
                    if (filter is SpatialFilter)
                    {
                        ((SpatialFilter)filter).FilterSpatialReference = filter.FeatureSpatialReference;
                    }
                    break;
                case ServiceProjectionMethode.none:
                    filter.FeatureSpatialReference = null;
                    if (filter is SpatialFilter)
                    {
                        ((SpatialFilter)filter).FilterSpatialReference = null;
                    }

                    break;
                default:
                    break;
            }
        }
    }

    public static void ReduceSubFields(QueryFilter filter, ILayer layer, bool appendIdAndShapeField)
    {
        if (filter == null || String.IsNullOrEmpty(filter.SubFields) || layer == null)
        {
            return;
        }

        string subfields = filter.SubFields;
        char sep = (subfields.Contains(",") ? ',' : ' ');

        List<string> fields = new List<string>(layer.Fields.ToString(";").Split(';'));
        List<string> candidates = new List<string>(subfields.Split(sep));
        List<string> subFields = new List<string>();

        if (appendIdAndShapeField)
        {
            if (!String.IsNullOrEmpty(layer.IdFieldName))
            {
                subFields.Add(layer.IdFieldName);
            }

            if (!String.IsNullOrEmpty(layer.ShapeFieldName))
            {
                subFields.Add(layer.ShapeFieldName);
            }
        }

        foreach (string candidate in candidates)
        {
            foreach (string field in fields)
            {
                if (field == candidate && !subFields.Contains(candidate))
                {
                    subFields.Add(candidate);
                }
                else if (ShortName(field) == ShortName(candidate) &&
                    !fields.Contains(ShortName(candidate)))
                {
                    subFields.Add(ShortName(candidate));
                }
            }
        }

        StringBuilder sb = new StringBuilder();
        foreach (string subField in subFields)
        {
            if (sb.Length > 0)
            {
                sb.Append(sep.ToString());
            }

            sb.Append(subField);
        }
        filter.SubFields = sb.ToString();
    }

    static private string ShortName(string fieldname)
    {
        int pos = 0;
        string[] fieldnames = fieldname.Split(';');
        fieldname = "";
        for (int i = 0; i < fieldnames.Length; i++)
        {
            while ((pos = fieldnames[i].IndexOf(".")) != -1)
            {
                fieldnames[i] = fieldnames[i].Substring(pos + 1, fieldnames[i].Length - pos - 1);
            }
            if (fieldname != "")
            {
                fieldname += ";";
            }

            fieldname += fieldnames[i];
        }

        return fieldname;
    }

    public bool IsValidSelectionFilter
    {
        get
        {
            if (this is SpatialFilter)
            {
                return true;
            }

            if (String.IsNullOrEmpty(_where) &&
                _plus.Count == 0 && _minus.Count > 0)
            {
                return false;
            }

            return true;
        }
    }

    private IList _cmsQueryNodeCollection = null;
    public IList CmsQueryNodeCollection
    {
        get
        {
            if (_cmsQueryNodeCollection == null)
            {
                return new List<object>();
            }

            return _cmsQueryNodeCollection;
        }
        set
        {
            _cmsQueryNodeCollection = value;
        }
    }
}
