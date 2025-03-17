using E.Standard.ArcXml;
using E.Standard.ArcXml.Extensions;
using E.Standard.ArcXml.Models;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Profile.QueryEngines;

internal class ImsEngine
{
    async static public Task QueryPoints(IBridge bridge,
                            ProfileEnvironment.Profile profile, int index,
                            List<PointM> vertices, double stat,
                            Dictionary<string, List<PointM>> serviceVertices)
    {
        int resultIndex = profile.ResultIndex;

        var connectionProperties = new ArcAxlConnectionProperties()
        {
            AuthUsername = profile.ServiceUser,
            AuthPassword = profile.ServicePassword,
            Timeout = 25
        };

        var server = profile.Server[index];
        var service = profile.Service[index];
        serviceVertices[service] = new List<PointM>(vertices.ConvertAll(p => new PointM(p, stat)));

        var layerProps = await bridge.HttpService.GetAxlServiceLayerIdAsync(connectionProperties,
                                                                            server,
                                                                            service,
                                                                            profile.RasterTheme);

        if (String.IsNullOrWhiteSpace(layerProps.layerId))
        {
            throw new Exception("Can't determine LayerId for layer: " + profile.RasterTheme);
        }

        if (profile.UseRasterInfoPro)
        {
            List<ArcXmlPoint> points = new List<ArcXmlPoint>();
            foreach (var vertex in serviceVertices[service])
            {
                points.Add(new ArcXmlPoint(vertex.X, vertex.Y, double.NaN));
            }

            //if (await query.GetRasterInfoProAsync(layerId, points))
            if (await bridge.HttpService.GetAxlServiceRasterInfoProAsync(connectionProperties,
                                                                        server, service,
                                                                        layerProps.layerId,
                                                                        points,
                                                                        layerProps.commaFormat))
            {
                for (int i = 0; i < points.Count; i++)
                {
                    serviceVertices[service][i].Z = points[i].Z;
                }
            }
        }
        else
        {
            foreach (var vertex in serviceVertices[service])
            {
                //double[] res = await query.GetRasterInfoAsync(layerId, vertex.X, vertex.Y);
                double[] res = await bridge.HttpService.GetAxlServiceRasterInfoAsync(connectionProperties,
                                                                                     server, service,
                                                                                     layerProps.layerId,
                                                                                     vertex.X, vertex.Y,
                                                                                     layerProps.commaFormat);
                if (res == null || res.Length <= resultIndex)
                {
                    vertex.Z = double.NaN;
                }
                else
                {
                    vertex.Z = res[resultIndex];
                }
            }
        }
    }
}
