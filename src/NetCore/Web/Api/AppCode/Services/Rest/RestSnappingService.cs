using Api.Core.AppCode.Mvc;
using E.Standard.Api.App;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.Configuration.Services;
using E.Standard.Extensions.Compare;
using E.Standard.Platform;
using E.Standard.WebGIS.Api.Abstractions;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.Rest;

public class RestSnappingService
{
    private readonly CacheService _cache;
    private readonly ConfigurationService _config;
    private readonly RestMappingHelperService _mapping;
    private readonly IRequestContext _requestContext;
    private readonly IUrlHelperService _urlHelper;

    public RestSnappingService(CacheService cache,
                               ConfigurationService config,
                               RestMappingHelperService mapping,
                               IRequestContext requestContext,
                               UrlHelperService urlHelper)
    {
        _cache = cache;
        _config = config;
        _mapping = mapping;
        _requestContext = requestContext;
        _urlHelper = urlHelper;
    }

    async public Task<IActionResult> PerformSnappingAsync(ApiBaseController controller,
                                                          CmsDocument.UserIdentification ui)
    {
        var httpRequest = controller.Request;

        double scale = httpRequest.Form["scale"].ToPlatformDouble();
        var env = new Envelope(httpRequest.Form["bbox"].ToString().Split(',').Select(d => d.ToPlatformDouble()).ToArray());
        var topo = new E.Standard.WebMapping.GeoServices.Topo.Topology<double>();

        foreach (string key in httpRequest.Form.Keys)
        {
            if (!key.Contains("~"))
            {
                continue;
            }

            string serviceId = key.Split('~')[0], snappingId = key.Split('~')[1];
            var service = await _cache.GetService(serviceId, null, ui, _urlHelper);
            var snapping = _cache.GetSnapSchemes(serviceId, ui).Where(s => s.Id == snappingId).FirstOrDefault();
            if (service == null || snapping == null || snapping.MinScale < scale)
            {
                continue;
            }

            _mapping.ApplyVisFilters(httpRequest, service, ui);

            foreach (var layerId in snapping.LayerIds)
            {
                var layer = service.Layers.FindByLayerId(layerId);
                if (layer == null)
                {
                    continue;
                }

                var layerIds = service.ResponseType == ServiceResponseType.Image || service.ResponseType == ServiceResponseType.Collection ?
                    new string[] { layerId } :  // Hier könnten in Zukunft auch mehrere Layer stehen (falls sichtbarkeit beispielsweise über einen anderen Layer geregelt wird)
                    null;


                object meta = new
                {
                    id = key,
                    name = layer.Name.Contains(@"\") ? layer.Name.Substring(layer.Name.LastIndexOf(@"\") + 1) : layer.Name,
                    layerIds = layerIds
                };

                var filter = new SpatialFilter(layer.IdFieldName, env, 1000, 1);
                filter.SubFields = layer.IdFieldName + " " + layer.ShapeFieldName;
                filter.QueryGeometry = true;

                var clipRect = env;
                int calcCrs = 0;
                int.TryParse(httpRequest.Form["calc_crs"], out calcCrs);
                var sRefId = calcCrs.OrTake(_config.DefaultQuerySrefId());
                topo.SrsId = sRefId;
                // DoTo: Supported Srs vom Dienst prüfen -> wenn nicht supported -> Kein Snapping für diesen Layer

                filter.FilterSpatialReference = filter.FeatureSpatialReference = ApiGlobals.SRefStore.SpatialReferences.ById(sRefId);
                using (var transformer = new GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences.ById(4326), filter.FilterSpatialReference))
                {
                    clipRect = new Envelope(env);
                    transformer.Transform(clipRect);
                    clipRect = clipRect.ShapeEnvelope;
                    filter.QueryShape = clipRect;

                    var features = new FeatureCollection();
                    if (await layer.GetFeaturesAsync(filter, features, _requestContext))
                    {
                        foreach (var feature in features)
                        {
                            if (feature != null && feature.Shape != null)
                            {
                                topo.AddShape(feature.Shape, true, meta, clipRect, transformer);
                            }
                        }
                    }
                }
            }
        }

        return await controller.JsonObject(topo.ToJsonObject());
    }
}
