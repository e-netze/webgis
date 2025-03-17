using E.Standard.Extensions.Formatting;
using E.Standard.ThreadSafe;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E.Standard.WebMapping.Core.Collections;

public class FeatureCollection : ThreadSafeList<Feature>, IClone<FeatureCollection, IMap>
{
    private bool _hasMore = false, _maxReached = false;
    private QueryFilter _query = null;
    private ILayer _layer = null;
    private string _resultText = String.Empty;
    private DrawPhase _drawPhase = DrawPhase.None;

    public FeatureCollection() : base()
    {

    }
    public FeatureCollection(Feature feature)
        : this()
    {
        this.Add(feature);
    }

    public FeatureCollection(IEnumerable<Feature> features)
        : this()
    {
        if (features != null)
        {
            this.AddRange(features);
        }
    }

    public bool HasMore
    {
        get { return _hasMore; }
        set { _hasMore = value; }
    }

    public bool MaximumReached
    {
        get { return _maxReached; }
        set { _maxReached = value; }
    }

    public QueryFilter Query
    {
        get { return _query; }
        set { _query = value; }
    }

    public ILayer Layer
    {
        get { return _layer; }
        set { _layer = value; }
    }

    public Feature FeatureByFid(long fid)
    {
        string idFieldName = _layer.IdFieldName;

        foreach (Feature feature in this)
        {
            long id;
            if (long.TryParse(feature[idFieldName], out id) &&
                id == fid)
            {
                return feature;
            }
        }
        return null;
    }

    public long FeatureFid(Feature feature)
    {
        long id;
        if (long.TryParse(feature[_layer.IdFieldName], out id))
        {
            return id;
        }

        return -1;
    }

    public void CheckAll()
    {
        foreach (Feature feature in this)
        {
            feature.Checked = true;
        }
    }

    public void UnCheckAll()
    {
        foreach (Feature feature in this)
        {
            feature.Checked = false;
        }
    }

    public void OrderByIds(IEnumerable<long> ids)
    {
        var tempFeatures = new List<Feature>(this);

        this.Clear();
        foreach (var id in ids)
        {
            var feature = tempFeatures.Where(f =>
            {
                if (f.Oid > 0)
                {
                    return id.Equals(f.Oid);
                }
                else if (f.GlobalOid.Contains(":"))
                {
                    if (long.TryParse(f.GlobalOid.Split(':').Last(), out long oid))
                    {
                        return id.Equals(oid);
                    }
                }

                return false;
            }
            ).FirstOrDefault();
            if (feature != null)
            {
                this.Add(feature);
            }
        }
    }

    public void OrderByPointDistance(Point point)
    {
        var tempFeatures = new List<Feature>(this);
        tempFeatures.Sort(new DistanceComparerer(point));

        this.Clear();
        this.AddRange(tempFeatures);
    }

    public void Distinct(bool compareGeometry, IEnumerable<string> compareFields, double tolerance = Shape.Epsilon)
    {
        var tempFeatures = new List<Feature>(this);
        var distinctedFeatures = tempFeatures.Distinct(new FeatureComparer(compareGeometry, compareFields, tolerance));

        this.Clear();
        this.AddRange(distinctedFeatures);
    }

    public void RemoveUnchecked()
    {
        FeatureCollection coll = new FeatureCollection();
        foreach (Feature feature in this)
        {
            if (!feature.Checked)
            {
                coll.Add(feature);
            }
        }

        foreach (Feature feature in coll)
        {
            this.Remove(feature);
        }
    }

    public FeatureCollection CheckedFeatures
    {
        get
        {
            FeatureCollection coll = new FeatureCollection();
            foreach (Feature feature in this)
            {
                if (feature.Checked)
                {
                    coll.Add(feature);
                }
            }

            return coll;
        }
    }
    public FeatureCollection UncheckedFeatures
    {
        get
        {
            FeatureCollection coll = new FeatureCollection();
            foreach (Feature feature in this)
            {
                if (!feature.Checked)
                {
                    coll.Add(feature);
                }
            }

            return coll;
        }
    }

    public Envelope ZoomEnvelope
    {
        get
        {
            Envelope env = null;
            foreach (Feature feature in this)
            {
                if (feature?.ZoomEnvelope == null && feature?.Shape == null)
                {
                    continue;
                }

                if (env == null)
                {
                    env = new Envelope(feature.ZoomEnvelope ?? feature.Shape.ShapeEnvelope);
                }
                else
                {
                    env.Union(feature.ZoomEnvelope ?? feature.Shape.ShapeEnvelope);
                }
            }
            return env;
        }
    }

    public string ResultText
    {
        get { return _resultText; }
        set { _resultText = value; }
    }

    public DrawPhase PostQueryExtraDrawphase
    {
        get { return _drawPhase; }
        set { _drawPhase = value; }
    }

    public Dictionary<string, string> Links = null;

    public IEnumerable<TableFieldDefintion> TableFieldDefintions = null;

    public void AddWarning(string warning)
    {
        if (_warnings == null)
        {
            _warnings = new List<string>();
        }

        _warnings.Add(warning);
    }

    public void AddWarnings(IEnumerable<string> warnings)
    {
        if (warnings != null)
        {
            foreach (var warning in warnings)
            {
                AddWarning(warning);
            }
        }
    }

    public void AddInformation(string warning)
    {
        if (_informations == null)
        {
            _informations = new List<string>();
        }

        _informations.Add(warning);
    }

    public void AddInformations(IEnumerable<string> informations)
    {
        if (informations != null)
        {
            foreach (var information in informations)
            {
                AddInformation(information);
            }
        }
    }

    private List<string> _warnings = null;
    public IEnumerable<string> Warnings => _warnings;

    private List<string> _informations = null;
    public IEnumerable<string> Informations => _informations;

    #region Export

    public string ToCsv(char separator = ';', bool addHeader = true, bool excel = false)
    {
        if (this.Count() == 0)
        {
            return String.Empty;
        }

        var columns = this.FirstOrDefault()
            .Attributes?
            .Where(a => !a.Name.StartsWith("_") && !a.Value.StartsWith("<a "))   // keine Links
            .Select(a => a.Name);

        if (columns.Count() == 0)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();

        if (addHeader)
        {
            bool firstHeader = true;
            foreach (var header in columns)
            {
                if (!firstHeader)
                {
                    sb.Append(separator);
                }
                else
                {
                    firstHeader = false;
                }

                sb.Append(header);
            }
            sb.Append(System.Environment.NewLine);
        }

        foreach (var feature in this)
        {
            bool firstColumn = true;
            foreach (var column in columns)
            {
                var value = feature[column]?.ToString() ?? String.Empty;

                if (!firstColumn)
                {
                    sb.Append(separator);
                }
                else
                {
                    firstColumn = false;
                }

                if (value.StartsWith("<a "))
                {
                    int hrefPos = value.IndexOf("href=");

                    int quoteStartPos = hrefPos + 5;
                    var quote = value[hrefPos + 5].ToString();
                    int quoteClosePos = value.IndexOf(quote, quoteStartPos + 1);

                    value = value.Substring(quoteStartPos + 1, quoteClosePos - quoteStartPos - 1);
                }

                if (excel && !value.IsNumber())
                {
                    sb.Append($@"=""{value}""");  // verhindert, dass beispielsweise Grundstücksnummer in ein Datum umgewandelt werden
                }
                else
                {
                    sb.Append(value);
                }
            }
            sb.Append(System.Environment.NewLine);
        }

        return sb.ToString();
    }

    #endregion

    #region IClone Member

    public FeatureCollection Clone(IMap parent)
    {
        FeatureCollection clone = new FeatureCollection();

        //foreach (Feature feature in this)
        //    clone.Add(feature);
        this.CopyElementsThreadSafeTo(clone);

        clone._hasMore = _hasMore;
        clone._resultText = _resultText;

        if (_query != null)
        {
            clone._query = _query.Clone();
        }

        if (parent is not null)
        {
            if (_layer != null)
            {
                clone._layer = parent.LayerById(_layer.GlobalID);
            }
        }

        clone._drawPhase = _drawPhase;

        return clone;
    }

    #endregion

    #region Classes

    public class TableFieldDefintion
    {
        public string Name { get; set; }

        public bool Visible { get; set; }

        public string SortingAlgorithm { get; set; }

        public (int width, int height)? ImageSize { get; set; }
    }

    private class DistanceComparerer : IComparer<Feature>
    {
        private readonly Point _point;

        public DistanceComparerer(Point point)
        {
            _point = point;
        }

        public int Compare(Feature x, Feature y)
        {
            try
            {
                if (x?.Shape == null && y?.Shape == null)
                {
                    return 0;
                }

                if (x?.Shape == null)
                {
                    return 1;
                }

                if (y?.Shape == null)
                {
                    return -1;
                }

                return SpatialAlgorithms.Point2ShapeDistance(x.Shape, _point).CompareTo(SpatialAlgorithms.Point2ShapeDistance(y.Shape, _point));
            }
            catch
            {
                return 0;
            }
        }
    }

    private class FeatureComparer : IEqualityComparer<Feature>
    {
        private readonly bool _compareGeometry;
        private readonly IEnumerable<string> _compareFields;
        private readonly double _tolerance;

        public FeatureComparer(bool compareGeometry, IEnumerable<string> compareFields, double tolerance)
        {
            _compareGeometry = compareGeometry;
            _compareFields = compareFields;
            _tolerance = tolerance;
        }

        public bool Equals(Feature x, Feature y)
        {
            if (_compareGeometry)
            {
                var p1 = SpatialAlgorithms.ShapePoints(x.Shape, false);
                var p2 = SpatialAlgorithms.ShapePoints(y.Shape, false);

                if (!SpatialAlgorithms.IsEqual2D(p1, p2))
                {
                    return false;
                }
            }

            if (_compareFields != null && _compareFields.Count() > 0)
            {
                if (x.Attributes == null || y.Attributes == null)
                {
                    return false;
                }

                bool allFields = _compareFields.Contains("*");

                foreach (var attribute in x.Attributes)
                {
                    if (allFields || _compareFields.Contains(attribute.Name))
                    {
                        if (attribute.Value != y[attribute.Name])
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public int GetHashCode(Feature obj)
        {
            return 0;
        }
    }

    #endregion
}
