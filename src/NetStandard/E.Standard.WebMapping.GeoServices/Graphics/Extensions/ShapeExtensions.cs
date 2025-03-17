using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.ServiceResponses;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;
using gView.GraphicsEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.Graphics.Extensions;

static public class ShapeExtensions
{
    async static public Task<byte[]> CreateImage(this IEnumerable<Shape> shapes,
                                                 Core.Api.Bridge.IBridge bridge,
                                                 int imageWidth, int imageHeight,
                                                 Envelope bbox = null,
                                                 ArgbColor[] useColors = null)
    {
        var colors = useColors ?? new ArgbColor[]
        {
            ArgbColor.Red,
            ArgbColor.Green,
            ArgbColor.Blue,
            ArgbColor.Yellow,
            ArgbColor.Cyan,
            ArgbColor.AliceBlue,
            ArgbColor.Orange
        };

        var graphicsService = new GraphicsService();

        var map = new Map();
        map.Environment.SetUserValue(webgisConst.OutputPath, bridge.OutputPath);
        map.Environment.SetUserValue(webgisConst.OutputUrl, bridge.OutputUrl);

        map.Services.Add(graphicsService);
        await graphicsService.InitAsync(map, bridge.RequestContext);

        map.ImageWidth = imageWidth;
        map.ImageHeight = imageHeight;

        Envelope shapeEnvelope = bbox ?? shapes.BoundingBox();
        if (shapeEnvelope == null || shapeEnvelope.IsNull)
        {
            return null;
        }

        int colorCounter = 0;

        foreach (var shape in shapes)
        {
            var color = colors[colorCounter++ % colors.Length];
            if (shape is Point)
            {
                map.GraphicsContainer.Add(new PointElement((Point)shape, color, 10));
            }
            else if (shape is Polyline)
            {
                map.GraphicsContainer.Add(new PolylineElement((Polyline)shape, color, 3));
            }
            else if (shape is Polygon)
            {
                map.GraphicsContainer.Add(new PolygonElement((Polygon)shape, ArgbColor.Black, color, 2f));
            }
        }

        if (shapeEnvelope == null)
        {
            return null;
        }

        map.ZoomTo(shapeEnvelope);

        var imageResponse = await map.GetMapAsync(bridge.RequestContext);

        if (imageResponse is ImageLocation)
        {
            using (var ms = await ((ImageLocation)imageResponse).ImagePath.BytesFromUri(bridge.HttpService))
            {
                return ms.ToArray();
            }
        }

        return null;
    }

    static public Envelope BoundingBox(this IEnumerable<Shape> shapes)
    {
        Envelope bbox = null;

        if (shapes != null)
        {
            foreach (var shape in shapes)
            {
                if (shape?.ShapeEnvelope == null)
                {
                    continue;
                }

                if (bbox == null)
                {
                    bbox = new Envelope(shape.ShapeEnvelope);
                }
                else
                {
                    bbox.Union(shape.ShapeEnvelope);
                }
            }
        }

        return bbox;
    }

    static public Envelope UnionWith(this Envelope env1, Envelope env2)
    {
        if (env1 == null && env2 == null)
        {
            return null;
        }

        if (env1 == null || env1.IsNull)
        {
            return env2;
        }

        if (env2 == null || env2.IsNull)
        {
            return env1;
        }

        var unionEnvelope = new Envelope(env1);
        unionEnvelope.Union(env2);

        return unionEnvelope;
    }
}
