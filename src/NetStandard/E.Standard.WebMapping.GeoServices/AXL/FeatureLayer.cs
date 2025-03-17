using E.Standard.ArcXml.Extensions;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace E.Standard.WebMapping.GeoServices.AXL;

class FeatureLayer : Layer, ILabelableLayer, ILayer2, IQueryValueConverter
{
    private string _renderer = String.Empty, _originalRenderer = String.Empty;
    private XmlNode _labelrenderer = null;
    private bool _useLabelRenderer = false;
    private double _opacity = -1.0;

    public FeatureLayer(string name, string id, IMapService service, bool queryable)
        : base(name, id, service, queryable: queryable)
    {
    }
    public FeatureLayer(string name, string id, LayerType type, IMapService service, bool queryable)
        : base(name, id, type, service, queryable: queryable)
    {
    }

    public string OriginalRenderer
    {
        get { return _originalRenderer; }
        set { _originalRenderer = value; }
    }
    public string Renderer
    {
        get { return _renderer; }
        set { _renderer = value; }
    }
    public XmlNode LabelRenderer
    {
        get { return _labelrenderer; }
        set { _labelrenderer = value; }
    }
    public bool UseLabelRenderer
    {
        get { return _useLabelRenderer; }
        set { _useLabelRenderer = value; }
    }

    public double Opacity
    {
        get { return _opacity; }
        set { _opacity = value; }
    }

    async public override Task<bool> GetFeaturesAsync(QueryFilter filter, FeatureCollection features, IRequestContext requestContext)
    {
        if (filter == null || features == null)
        {
            throw new ArgumentException();
        }

        if (!(_service is AxlService))
        {
            throw new Exception("FATAL ERROR: No Service Object!");
        }

        string where = filter.Where.Replace("\"", "'");  // Abfragen aus GDB kommen mit " statt mit ' daher !!!
        string filterExpression = this.Filter;

        // TODO: appendWhereFilter was ist das?
        // Global aus der Map?
        // Filter?
        //if (appendWhereFilter != "")
        //    filter += ((filter != "") ? " AND " : "") + appendWhereFilter;

        if (filterExpression != "")
        {
            where = ((!String.IsNullOrEmpty(where)) ? "(" + where + ") AND " : "") + filterExpression;
        }

        // REQUEST erzeugen
        StringBuilder axl = new StringBuilder();
        MemoryStream ms = new MemoryStream();
        XmlTextWriter xWriter = new XmlTextWriter(ms, ((AxlService)this._service).Encoding);

        xWriter.WriteStartDocument();
        xWriter.WriteStartElement("ARCXML");
        xWriter.WriteAttributeString("version", "1.1");
        xWriter.WriteStartElement("REQUEST");
        xWriter.WriteStartElement("GET_FEATURES");
        xWriter.WriteAttributeString("outputmode", "newxml");
        xWriter.WriteAttributeString("geometry", filter.QueryGeometry.ToString().ToLower());
        xWriter.WriteAttributeString("checkesc", "true");

        xWriter.WriteAttributeString("envelope", "true");
        if (filter.FeatureLimit >= 0)
        {
            xWriter.WriteAttributeString("featurelimit", filter.FeatureLimit.ToString());
        }

        if (filter.BeginRecord >= 0)
        {
            xWriter.WriteAttributeString("beginrecord", filter.BeginRecord.ToString());
        }

        xWriter.WriteStartElement("LAYER");
        xWriter.WriteAttributeString("id", this.ID);
        xWriter.WriteEndElement();  // LAYER

        QueryFilter clone = filter.Clone();
        if (_service is AxlService && ((AxlService)_service).Is_gView)
        {
            if (clone is SpatialFilter && ((SpatialFilter)clone).QueryShape != null &&
                ((SpatialFilter)clone).QueryShape.Buffer != null && ((SpatialFilter)clone).QueryShape.Buffer.BufferDistance != 0.0)
            {
                using (var cts = new CancellationTokenSource())
                {
                    ((SpatialFilter)clone).QueryShape = ((SpatialFilter)clone).QueryShape.CalcBuffer(
                                                            ((SpatialFilter)clone).QueryShape.Buffer.BufferDistance,
                                                            cts);
                }
            }
        }

        if (clone is SpatialFilter && ((SpatialFilter)clone).QueryShape is Point)
        {
            var spatialClone = (SpatialFilter)clone;
            double delta = 1e-11;
            if (spatialClone.FilterSpatialReference != null && spatialClone.FilterSpatialReference.IsProjective)
            {
                delta = 1e-7;
            }
            var env = ((SpatialFilter)clone).QueryShape.ShapeEnvelope;
            env.Resize(delta, delta);
            ((SpatialFilter)clone).QueryShape = env;
        }

        clone.Where = WebGIS.CMS.Globals.EncUmlaute(where, ((AxlService)_service).Umlaute2Wildcard);
        xWriter.WriteRaw(clone.ArcXML(((AxlService)_service)._nfi));

        xWriter.WriteEndElement(); // GET_FEATURES

        xWriter.WriteEndDocument();
        xWriter.Flush();

        ms.Position = 0;
        StreamReader sr = new StreamReader(ms);
        axl.Append(sr.ReadToEnd());
        sr.Close();
        ms.Close();
        xWriter.Close();

        string req = axl.ToString();   //axl.ToString().Replace("&amp;", "&");
        //req = req.Replace("&amp;#228;", "&#228;");
        //req = req.Replace("&amp;#246;", "&#246;");
        //req = req.Replace("&amp;#252;", "&#252;");
        //req = req.Replace("&amp;#196;", "&#196;");
        //req = req.Replace("&amp;#214;", "&#214;");
        //req = req.Replace("&amp;#220;", "&#220;");
        //req = req.Replace("&amp;#223;", "&#223;");
        req = req.Replace("&amp;#", "&#");

        var httpService = requestContext.Http;

        // REQUEST verschicken
        //string resp = await ((Service)_service).Connector.SendRequestAsync(req, _service.Server, _service.ServiceName, "Query");
        string resp = await httpService.SendAxlRequestAsync(((AxlService)_service).ConnectionProperties, req, _service.Server, _service.Service, "Query");

        AxlHelper.AppendFeatures(this, features, resp, ((AxlService)_service)._nfi);
        features.Query = filter;
        features.Layer = this;

        if (features.HasMore && features.Count < filter.FeatureLimit)
        {
            features.MaximumReached = true;
        }

        return true;
    }

    override public ILayer Clone(IMapService parent)
    {
        if (parent is null)
        {
            return null;
        }

        FeatureLayer clone = new FeatureLayer(this.Name, this.ID, this.Type,
            parent, queryable: this.Queryable);
        clone.ClonePropertiesFrom(this);

        clone._renderer = _renderer;
        clone._originalRenderer = _originalRenderer;
        clone._labelrenderer = _labelrenderer;
        clone._useLabelRenderer = _useLabelRenderer;
        clone._opacity = _opacity;

        return clone;
    }

    #region ILabelableLayer Member

    E.Standard.WebMapping.Core.Renderer.LabelRenderer ILabelableLayer.LabelRenderer
    {
        set
        {
            if (value != null)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(value.ToArcXML());

                this.LabelRenderer = doc.ChildNodes[0];
            }
        }
    }

    #endregion

    #region ILayer2 Member

    async public Task<int> HasFeaturesAsync(QueryFilter filter, IRequestContext requestContext)
    {
        filter = filter.Clone();
        filter.FeatureLimit = 1;

        FeatureCollection features = new FeatureCollection();
        await GetFeaturesAsync(filter, features, requestContext);

        return features.Count;
    }

    async public Task<Shape> FirstFeatureGeometryAsync(QueryFilter filter, IRequestContext requestContext)
    {
        filter = filter.Clone();
        filter.FeatureLimit = 1;
        filter.QueryGeometry = true;

        FeatureCollection features = new FeatureCollection();

        await GetFeaturesAsync(filter, features, requestContext);

        if (features.Count > 0)
        {
            return features[0].Shape;
        }

        return null;
    }

    async public Task<Feature> FirstFeatureAsync(QueryFilter filter, IRequestContext requestContext)
    {
        filter = filter.Clone();
        filter.FeatureLimit = 1;
        filter.QueryGeometry = true;

        FeatureCollection features = new FeatureCollection();

        await GetFeaturesAsync(filter, features, requestContext);

        if (features.Count > 0)
        {
            return features[0];
        }

        return null;
    }

    #endregion

    #region IQueryValueConverter Member

    public string ConvertQueryValue(IField field, string value)
    {
        if (field == null)
        {
            return value;
        }

        switch (field.Type)
        {
            case FieldType.Date:
                //DateTime dt = Convert.ToDateTime(value);
                //string v = "{ts '" + string.Format("{0:u}", dt).ToUpper().Replace("00:00:00Z", "0:00:00") + "'}";
                //string v = "1269993600000";
                string v = "'" + value + "'";
                return v;
        }

        return value;
    }

    #endregion
}
