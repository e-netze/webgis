using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace E.Standard.WebMapping.Core.Filters;

public class SpatialFilter : QueryFilter
{
    private Shape _shape;
    private SpatialReference _filterSr = null;

    public SpatialFilter(string idFieldName, Shape shape, int featureLimit, int beginRecord)
        : base(idFieldName, featureLimit, beginRecord)
    {
        _shape = shape;
    }
    public SpatialFilter(string idFieldName, Shape shape, string where, int featureLimit, int beginRecord)
        : base(idFieldName, where, featureLimit, beginRecord)
    {
        _shape = shape;
    }
    public SpatialFilter(QueryFilter query)
        : base(query)
    {
        if (query is SpatialFilter)
        {
            _shape = (((SpatialFilter)query)._shape == null) ?
                            null : ((SpatialFilter)query)._shape;
            _filterSr = ((SpatialFilter)query)._filterSr;
        }
    }

    public Shape QueryShape
    {
        get { return _shape; }
        set { _shape = value; }
    }

    public SpatialReference FilterSpatialReference
    {
        get { return _filterSr; }
        set { _filterSr = value; }
    }

    public override string ArcXML(NumberFormatInfo nfi)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("<SPATIALQUERY");
        string where = this.Where;
        if (!String.IsNullOrEmpty(where))
        {
            sb.Append(" where=\"" + EncodeXmlString(where) + "\"");
        }

        sb.Append(" subfields=\"" +
            ((_subfields == "*" || String.IsNullOrEmpty(_subfields)) ? "#ALL#" : _subfields) +
            "\">");


        if (_buffer != null)
        {
            sb.Append(_buffer.ArcXML(nfi));
        }

        if (_featureSr != null)
        {
            sb.Append("<FEATURECOORDSYS id='" + _featureSr.Id + "' />");
        }

        if (_filterSr != null)
        {
            sb.Append("<FILTERCOORDSYS id='" + _filterSr.Id + "' />");
        }

        sb.Append("<SPATIALFILTER relation=\"area_intersection\">");
        if (_shape.Buffer != null)
        {
            sb.Append(_shape.Buffer.ArcXML(nfi));
        }

        if (_shape is Point)
        {
            sb.Append("<MULTIPOINT>" + _shape.ArcXML(nfi) + "</MULTIPOINT>");
        }
        else if (_shape != null)
        {
            sb.Append(_shape.ArcXML(nfi));
        }
        else
        {
            throw new Exception("SpatialFilter: Filter shape is NULL");
        }
        sb.Append("</SPATIALFILTER>");
        sb.Append("</SPATIALQUERY>");

        return sb.ToString();
    }

    public override QueryFilter Clone()
    {
        return new SpatialFilter(this);
    }

    public QueryFilter ToFilter(List<long> featureIds)
    {
        QueryFilter filter = new QueryFilter(this);

        if (featureIds != null)
        {
            foreach (long featureId in featureIds)
            {
                filter.Plus(featureId);
            }
        }

        return filter;
    }
    public QueryFilter ToFilter(FeatureCollection features)
    {
        List<long> featureIds = new List<long>();
        if (features != null)
        {
            foreach (Feature feature in features)
            {
                long oid;
                if (long.TryParse(feature[_idFieldName], out oid))
                {
                    featureIds.Add(oid);
                }
            }
        }

        return ToFilter(featureIds);
    }
}
