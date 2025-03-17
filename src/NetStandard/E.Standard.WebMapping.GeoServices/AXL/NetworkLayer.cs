using E.Standard.ArcXml.Extensions;
using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.AXL.GraphicElements;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;
using gView.GraphicsEngine;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace E.Standard.WebMapping.GeoServices.AXL;

class NetworkLayer : Layer, ILayer2, ILayer_QueryNodesRequired, ILayer_DynamicFields
{
    public NetworkLayer(string name, string id, IMapService service)
        : base(name, id, LayerType.network, service, queryable: true)
    {
    }

    async public override Task<bool> GetFeaturesAsync(QueryFilter filter, FeatureCollection result, IRequestContext requestContext)
    {
        if (!(filter is SpatialFilter))
        {
            return false;
        }

        CmsNodeCollection queryNodes = filter.CmsQueryNodeCollection as CmsNodeCollection;
        if (queryNodes == null || queryNodes.Count != 1)
        {
            return false;
        }

        CmsNode queryNode = queryNodes[0] is CmsLink ? ((CmsLink)queryNodes[0]).Target : queryNodes[0];
        if (queryNodes == null)
        {
            return false;
        }

        string tracerName = queryNode.LoadString("networktracer");
        if (String.IsNullOrEmpty(tracerName))
        {
            tracerName = Guid.Empty.ToString();
        }

        string tracerGuid = tracerName.Split(':')[0];

        SpatialFilter sFilter = (SpatialFilter)filter;

        StringBuilder axl = new StringBuilder();
        MemoryStream ms = new MemoryStream();
        XmlTextWriter xWriter = new XmlTextWriter(ms, ((AxlService)this._service).Encoding);

        xWriter.WriteStartDocument();
        xWriter.WriteStartElement("ARCXML");
        xWriter.WriteAttributeString("version", "1.1");
        xWriter.WriteStartElement("REQUEST");
        xWriter.WriteStartElement("gv_TRACE_NETWORK");

        xWriter.WriteStartElement("LAYER");
        xWriter.WriteAttributeString("id", this.ID);
        xWriter.WriteEndElement();  // LAYER

        xWriter.WriteStartElement("gv_TRACER");
        xWriter.WriteAttributeString("id", tracerGuid);
        xWriter.WriteEndElement();  // gv_TRACER

        NetworkBarrierGraphicElement[] barriers = NetworkBarrierGraphicElement.FindBarriers(_service.Map, _service.Url, base.ID);
        if (barriers != null && barriers.Length > 0)
        {
            xWriter.WriteStartElement("gv_NW_BARRIERS");

            foreach (NetworkBarrierGraphicElement barrier in barriers)
            {
                xWriter.WriteStartElement("gv_NW_BARRIER");
                xWriter.WriteAttributeString("nodeid", barrier.NodeId.ToString());
                xWriter.WriteEndElement(); // gv_NW_BARRIER
            }

            xWriter.WriteEndElement(); // gv_NW_BARRIERS
        }

        QueryFilter clone = filter.Clone();
        xWriter.WriteRaw(clone.ArcXML(((AxlService)_service)._nfi));

        xWriter.WriteEndElement(); // gv_CAN_TRACE_NETWORK

        xWriter.WriteEndDocument();
        xWriter.Flush();

        ms.Position = 0;
        StreamReader sr = new StreamReader(ms);
        axl.Append(sr.ReadToEnd());
        sr.Close();
        ms.Close();
        xWriter.Close();

        string req = axl.ToString();

        req = req.Replace("&amp;#", "&#");

        var httpService = requestContext.Http;

        // REQUEST verschicken
        //string resp = await ((Service)_service).Connector.SendRequestAsync(req, _service.Server, _service.ServiceName, "Query");
        string resp = await httpService.SendAxlRequestAsync(((AxlService)_service).ConnectionProperties, req, _service.Server, _service.Service, "Query");

        FeatureCollection features = new FeatureCollection();
        AxlHelper.AppendFeatures(this, features, resp, ((AxlService)_service)._nfi);

        this._service.Map.GraphicsContainer.Remove(typeof(EdgeGraphicElement));

        foreach (Feature feature in features)
        {
            if (feature.Shape is Point)
            {
                result.Add(feature);
            }
            if (feature.Shape is Polyline)
            {
                //result.Add(feature);
                this._service.Map.GraphicsContainer.Add(new EdgeGraphicElement((Polyline)feature.Shape));
            }
        }
        result.PostQueryExtraDrawphase = DrawPhase.Graphics;

        return true;
    }

    override public ILayer Clone(IMapService parent)
    {
        if (parent is null)
        {
            return null;
        }

        NetworkLayer clone = new NetworkLayer(this.Name, this.ID, parent);

        return clone;
    }

    #region ILayer2 Member

    async public Task<int> HasFeaturesAsync(QueryFilter filter, IRequestContext requestContext)
    {
        if (!(filter is SpatialFilter))
        {
            return 0;
        }

        CmsNodeCollection queryNodes = filter.CmsQueryNodeCollection as CmsNodeCollection;
        if (queryNodes == null || queryNodes.Count == 0)
        {
            return 0;
        }

        SpatialFilter sFilter = (SpatialFilter)filter;

        foreach (CmsNode queryLink in queryNodes)
        {
            CmsNode queryNode = (queryLink is CmsLink) ? ((CmsLink)queryLink).Target : queryLink;
            if (queryNode == null)
            {
                continue;
            }

            string tracerName = queryNode.LoadString("networktracer");
            if (String.IsNullOrEmpty(tracerName))
            {
                tracerName = Guid.Empty.ToString();
            }

            string tracerGuid = tracerName.Split(':')[0];

            StringBuilder axl = new StringBuilder();
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xWriter = new XmlTextWriter(ms, ((AxlService)this._service).Encoding);

            xWriter.WriteStartDocument();
            xWriter.WriteStartElement("ARCXML");
            xWriter.WriteAttributeString("version", "1.1");
            xWriter.WriteStartElement("REQUEST");
            xWriter.WriteStartElement("gv_CAN_TRACE_NETWORK");

            xWriter.WriteStartElement("LAYER");
            xWriter.WriteAttributeString("id", this.ID);
            xWriter.WriteEndElement();  // LAYER

            xWriter.WriteStartElement("gv_TRACER");
            xWriter.WriteAttributeString("id", tracerGuid);
            xWriter.WriteEndElement();  // gv_TRACER

            QueryFilter clone = filter.Clone();
            xWriter.WriteRaw(clone.ArcXML(((AxlService)_service)._nfi));

            xWriter.WriteEndElement(); // gv_CAN_TRACE_NETWORK

            xWriter.WriteEndDocument();
            xWriter.Flush();

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            axl.Append(sr.ReadToEnd());
            sr.Close();
            ms.Close();
            xWriter.Close();

            string req = axl.ToString();

            req = req.Replace("&amp;#", "&#");

            var httpService = requestContext.Http;

            // REQUEST verschicken
            //string resp = await ((Service)_service).Connector.SendRequestAsync(req, _service.Server, _service.ServiceName, "Query");
            string resp = await httpService.SendAxlRequestAsync(((AxlService)_service).ConnectionProperties, req, _service.Server, _service.Service, "Query");

            FeatureCollection features = new FeatureCollection();
            AxlHelper.AppendFeatures(this, features, resp, ((AxlService)_service)._nfi);

            if (features.Count >= 1)
            {
                return 1;
            }
        }

        return 0;
    }

    public Task<Shape> FirstFeatureGeometryAsync(QueryFilter filter, IRequestContext requestContext)
    {
        return Task.FromResult<Shape>(null);
    }

    public Task<Feature> FirstFeatureAsync(QueryFilter filter, IRequestContext requestContext)
    {
        return Task.FromResult<Feature>(null);
    }

    #endregion

    #region GraphicElement Classes

    public class EdgeGraphicElement : PolylineElement
    {
        public EdgeGraphicElement(Polyline polyline)
            : base(polyline, ArgbColor.Blue, 3f)
        {
        }
    }

    #endregion

    #region ILayer_DynamicFields Member

    public IField GetDynamicField(string name)
    {
        Field field = new Field(name, FieldType.String);
        return field;
    }

    #endregion
}
