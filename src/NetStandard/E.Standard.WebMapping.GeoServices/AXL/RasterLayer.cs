using E.Standard.ArcXml.Extensions;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace E.Standard.WebMapping.GeoServices.AXL;

class RasterLayer : Layer, IRasterlayer
{
    public RasterLayer(string name, string id, IMapService service)
        : base(name, id, service, queryable: true)
    {
    }
    public RasterLayer(string name, string id, LayerType type, IMapService service)
        : base(name, id, type, service, queryable: true)
    {
    }

    async public override Task<bool> GetFeaturesAsync(QueryFilter filter, FeatureCollection result, IRequestContext requestContext)
    {
        if (!(filter is SpatialFilter) ||
            ((SpatialFilter)filter).QueryShape == null ||
            result == null)
        {
            throw new ArgumentException();
        }

        if (!(_service is AxlService))
        {
            throw new Exception("FATAL ERROR: No Service Object!");
        }

        // REQUEST erzeugen
        StringBuilder axl = new StringBuilder();
        MemoryStream ms = new MemoryStream();
        XmlTextWriter xWriter = new XmlTextWriter(ms, ((AxlService)this._service).Encoding);

        xWriter.WriteStartDocument();
        xWriter.WriteStartElement("ARCXML");
        xWriter.WriteAttributeString("version", "1.1");
        xWriter.WriteStartElement("REQUEST");
        xWriter.WriteStartElement("GET_RASTER_INFO");
        xWriter.WriteAttributeString("x", ((SpatialFilter)filter).QueryShape.ShapeEnvelope.CenterPoint.X.ToString());
        xWriter.WriteAttributeString("y", ((SpatialFilter)filter).QueryShape.ShapeEnvelope.CenterPoint.Y.ToString());
        xWriter.WriteAttributeString("layerid", this.ID);
        xWriter.WriteEndElement(); // GET_RASTER_INFO
        xWriter.WriteEndDocument();
        xWriter.Flush();

        ms.Position = 0;
        StreamReader sr = new StreamReader(ms);
        axl.Append(sr.ReadToEnd());
        sr.Close();
        ms.Close();
        xWriter.Close();

        string req = axl.ToString().Replace("&amp;", "&");

        var httpService = requestContext.Http;

        // REQUEST verschicken
        //string resp = await ((Service)_service).Connector.SendRequestAsync(req, _service.Server, _service.ServiceName, String.Empty);
        string resp = await httpService.SendAxlRequestAsync(((AxlService)_service).ConnectionProperties, req, _service.Server, _service.Service, String.Empty);

        AxlHelper.AppendRasterBands(result, resp);
        result.Query = filter;
        result.Layer = this;

        return true;
    }

    override public ILayer Clone(IMapService parent)
    {
        if (parent is null)
        {
            return null;
        }

        RasterLayer clone = new RasterLayer(this.Name, this.ID, this.Type, parent);
        clone.ClonePropertiesFrom(this);

        return clone;
    }
}
