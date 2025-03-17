using E.Standard.Platform;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Profile.QueryEngines;

internal class AgsEngine
{
    async static public Task QueryPoints(IBridge bridge,
                            ProfileEnvironment.Profile profile, int index,
                            List<PointM> vertices, double stat,
                            Dictionary<string, List<PointM>> serviceVertices)
    {
        int resultIndex = profile.ResultIndex;
        var map = bridge.TemporaryMapObject();

        string user = profile.ServiceUser;
        string pwd = profile.ServicePassword;

        var server = profile.Server[index];
        var service = profile.Service[index];

        serviceVertices[service] = new List<PointM>(vertices.ConvertAll(p => new PointM(p, stat)));

        var agsService = new E.Standard.WebMapping.GeoServices.ArcServer.Rest.MapService()
        {
            TokenExpiration = 600
        };

        agsService.PreInit(String.Empty, server, service, user, pwd, string.Empty, map.Environment.UserString(webgisConst.AppConfigPath), null);
        await agsService.InitAsync(map, bridge.RequestContext);

        agsService.ProjectionId = profile.SrsId;
        agsService.ProjectionMethode = WebMapping.Core.ServiceProjectionMethode.Userdefined;

        if (agsService.Layers == null)
        {
            return;
        }

        foreach (var layer in agsService.Layers)
        {
            if (layer.Name.Equals(profile.RasterTheme, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var vertex in serviceVertices[service])
                {
                    var features = new WebMapping.Core.Collections.FeatureCollection();
                    var filter = new WebMapping.Core.Filters.SpatialFilter(layer.IdFieldName, vertex, 1, 1);
                    await layer.GetFeaturesAsync(filter, features, bridge.RequestContext);

                    var rasterFeatures = features.ToArray();

                    if (profile.ServiceType == "ags-mosaic")
                    {
                        rasterFeatures = features
                                .Where(f => f.Attributes != null && f.Attributes.Where(a => a.Name.Equals("pixel value", StringComparison.InvariantCultureIgnoreCase)).Count() > 0)
                                .Select(f => new E.Standard.WebMapping.Core.Feature(f.Attributes.Where(a => a.Name.Equals("pixel value", StringComparison.InvariantCultureIgnoreCase))))
                                .ToArray();
                    }

                    vertex.Z = rasterFeatures?.FirstOrDefault()?.Attributes[resultIndex]?.Value?.ToPlatformDouble() ?? double.NaN;
                }
            }
        }
    }
}
