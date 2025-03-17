using E.Standard.Extensions.Text;
using E.Standard.Platform;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using E.Standard.WebMapping.GeoServices.ArcServer.Services;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest;

class RasterLayer : RestLayer, ILayer2, IRasterlayer
{
    private new readonly MapService _service;
    private readonly string _id = String.Empty;

    public RasterLayer(string name, string id, IMapService service, bool queryable)
        : base(name, id, service, queryable: queryable)
    {
        _service = (MapService)service;
        _id = id;
    }
    public RasterLayer(string name, string id, LayerType type, IMapService service, bool queryable)
        : base(name, id, type, service, queryable: queryable)
    {
        _service = (MapService)service;
        _id = id;
    }

    async public override Task<bool> GetFeaturesAsync(QueryFilter filter, FeatureCollection result, IRequestContext requestContext)
    {
        Point clickPoint = null;
        if (filter is SpatialFilter && ((SpatialFilter)filter).QueryShape != null)
        {
            clickPoint = ((SpatialFilter)filter).QueryShape.ShapeEnvelope.CenterPoint;
        }
        else
        {
            return false;
        }

        var postBodyData = new StringBuilder();

        postBodyData.Add(
            "&geometry=",
            clickPoint.X.ToPlatformNumberString(), ",", clickPoint.Y.ToPlatformNumberString(),
            "&geometryType=esriGeometryPoint&tolerance=1&f=pjson&layers=visible:", this._id);

        postBodyData.Add("&imageDisplay=", _service.Map.ImageWidth, ",", _service.Map.ImageHeight, ",", (int)_service.Map.Dpi);
        postBodyData.Add("&mapExtent=", _service.Map.Extent.MinX.ToPlatformNumberString(), ",",
                                        _service.Map.Extent.MinY.ToPlatformNumberString(), ",",
                                        _service.Map.Extent.MaxX.ToPlatformNumberString(), ",",
                                        _service.Map.Extent.MaxY.ToPlatformNumberString());

        #region Projection

        int outSrefId = filter.FeatureSpatialReference?.Id ?? 0,
            inSrefId = (filter as SpatialFilter)?.FilterSpatialReference?.Id ?? 0;

        if (_service.ProjectionMethode == ServiceProjectionMethode.Map && _service.Map.SpatialReference != null)
        {
            postBodyData.Add("&sr=", (inSrefId > 0 ? inSrefId : _service.Map.SpatialReference.Id));
        }
        else if (_service.ProjectionMethode == ServiceProjectionMethode.Userdefined && _service.ProjectionId > 0)
        {
            postBodyData.Add("&sr=", (inSrefId > 0 ? inSrefId : _service.ProjectionId));
        }
        else
        {
            if (inSrefId > 0)
            {
                postBodyData.Add("&sr=", inSrefId);
            }
        }
        postBodyData.Add("&returnGeometry=true");

        #endregion

        string featuresReqUrl = $"{_service.Service}/identify";
        var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();

        string featuresResponse = await authHandler.TryPostAsync(_service, featuresReqUrl, postBodyData.ToString());
        var jsonRasterResponse = JsonConvert.DeserializeObject<JsonRasterResponse>(featuresResponse);

        if (jsonRasterResponse.Results != null)
        {
            foreach (var iResult in jsonRasterResponse.Results)
            {
                if (iResult == null || !(iResult.ResultAttributes is Newtonsoft.Json.Linq.JObject))
                {
                    continue;
                }

                var attributes = (Newtonsoft.Json.Linq.JObject)iResult.ResultAttributes;

                Feature feature = new Feature();

                foreach (var prop in attributes)
                {
                    feature.Attributes.Add(new Core.Attribute(prop.Key, GetJsonValue(prop.Value)));
                }

                if (iResult.Geometry != null && iResult.Geometry.X.HasValue && iResult.Geometry.Y.HasValue)
                {
                    var shape = new Point(iResult.Geometry.X.Value, iResult.Geometry.Y.Value);
                    using (var transformer = new GeometricTransformerPro(CoreApiGlobals.SRefStore, iResult.Geometry.SpatialReference?.EpsgCode ?? 0, outSrefId))
                    {
                        transformer.Transform(shape);
                    }
                    feature.Shape = shape;
                }

                result.Add(feature);
            }
        }

        return true;
    }

    override public ILayer Clone(IMapService parent)
    {
        if (parent is null)
        {
            return null;
        }

        RasterLayer clone = new RasterLayer(this.Name, this.ID, this.Type, parent, this.Queryable);
        clone.ClonePropertiesFrom(this);
        base.CloneParentLayerIdsTo(clone);

        return clone;
    }

    private string GetJsonValue(Newtonsoft.Json.Linq.JToken token)
    {
        if (token is Newtonsoft.Json.Linq.JArray && ((Newtonsoft.Json.Linq.JArray)token).Count > 0)
        {
            return GetJsonValue(((Newtonsoft.Json.Linq.JArray)token)[0]);
        }
        if (token is Newtonsoft.Json.Linq.JValue)
        {
            return token.ToString();
        }

        return String.Empty;
    }

    #region ILayer2

    public Task<int> HasFeaturesAsync(QueryFilter filter, IRequestContext requestContext)
    {
        //FeatureCollection features=new FeatureCollection();
        //GetFeatures(filter, features);
        //count = features.Count;

        int count = 1;
        return Task.FromResult(count);
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
}
