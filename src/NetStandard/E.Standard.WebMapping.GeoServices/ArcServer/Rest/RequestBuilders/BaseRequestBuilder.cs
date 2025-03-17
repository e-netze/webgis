#nullable enable

using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.DynamicLayers;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.RequestBuilders;

public class BaseRequestBuilder<T> where T : BaseRequestBuilder<T>
{
    private readonly StringBuilder _builder = new StringBuilder();
    protected T _self = default!;

    //internal RestRequestBuilder(T self) => (_self) = (self);

    #region Helper

    private T Append(string str)
    {
        if (_builder.Length > 0)
        {
            _builder.Append("&");
        }

        _builder.Append(str);

        return _self;
    }

    #endregion

    #region General

    protected T WithFormat(string format) => Append($"f={format}");


    #endregion

    #region Export 

    protected T WithBBox(Envelope bbox) => Append($"bbox={bbox.ToBBox()}");

    protected T WithTransparency() => Append("transparent=true");

    protected T WithLayers(IEnumerable<int>? layerIds, string prefix = "show")
        => layerIds switch
        {
            null => Append("layers="),  // ignore use service defaults
            _ => Append($"layers={prefix}:{string.Join(",", layerIds)}")
        };

    protected T WithLayers(IEnumerable<string>? layerIds, string prefix = "show")
        => layerIds switch
        {
            null => Append("layers="),  // ignore use service defaults
            _ => Append($"layers={prefix}:{string.Join(",", layerIds)}")
        };

    protected T WithLayerDefintions(IDictionary<string, string>? layerDefs)
        => layerDefs switch
        {
            null => Append("layerDefs="),  // no layer defs
            _ => Append($"layerDefs={JSerializer.Serialize(layerDefs)}")
        };

    protected T WithMapRotation(IMap map)
    {
        if (map.DisplayRotation != 0D)
        {
            return Append($"rotation={map.DisplayRotation.ToString().ToPlatformDouble()}");
        }

        return _self;
    }

    protected T WithImageFormat(string imageFormat)
        => Append($"format={imageFormat}");

    protected T WithImageSizeAndDpi(int imageWidth, int imageHeight, int dpi)
        => Append($"size={imageWidth},{imageHeight}")
           .Append($"dpi={dpi}");

    protected T WithImageSRef(int sRefId)
        => sRefId switch
        {
            > 0 => Append($"imageSR={sRefId}"),
            _ => _self
        };

    protected T WithBBoxSRef(int sRefId)
        => sRefId switch
        {
            > 0 => Append($"bboxSR={sRefId}"),
            _ => _self
        };

    protected T WithDatumTransformations(int[] datumTransformations)
    {
        if (datumTransformations != null && datumTransformations.Length > 0)
        {
            Append($"datumTransformations=[{string.Join(",", datumTransformations)}]");
        }

        return _self;
    }

    protected T WithDynamicLayers(List<DynamicLayer>? dynamicLayers)
    {
        if (dynamicLayers is not null && dynamicLayers.Count > 0)
        {
            return Append($"dynamicLayers={JSerializer.Serialize(dynamicLayers.ToArray())}");
        }

        return _self;
    }

    #endregion

    #region Geometry

    protected T WithGeometry(Shape shape, int shapeSrefId = 0)
    {
        string geometry = "", geometryType = "";

        shapeSrefId = shapeSrefId.OrTake(shape.SrsId);

        if (shape is Point point)
        {
            geometry = RestHelper.ConvertGeometryToJson(point, shapeSrefId);
            geometryType = RestHelper.GetGeometryTypeString(point);
        }
        else if (shape is MultiPoint multiPoint)
        {
            geometry = RestHelper.ConvertGeometryToJson(multiPoint, shapeSrefId);
            geometryType = RestHelper.GetGeometryTypeString(multiPoint);
        }
        else if (shape is Polyline polyline)
        {
            geometry = RestHelper.ConvertGeometryToJson(polyline, shapeSrefId); // HELPER CLASS FOR STRING CREATION -> POINTCOLLECTION TO JSON STRING...
            geometryType = RestHelper.GetGeometryTypeString(polyline);
        }

        else if (shape is Polygon polygon)
        {
            geometry = RestHelper.ConvertGeometryToJson(polygon, shapeSrefId); // HELPER CLASS FOR STRING CREATION -> POINTCOLLECTION TO JSON STRING...
            geometryType = RestHelper.GetGeometryTypeString(polygon);
        }
        else if (shape is Envelope envelope)
        {
            geometry = RestHelper.ConvertGeometryToJson(envelope, shapeSrefId);
            geometryType = RestHelper.GetGeometryTypeString(envelope);
        }
        else { }

        return Append($"geometry={geometry}").Append($"geometryType={geometryType}");
    }

    #endregion

    #region Query

    protected T WithSpatialRelation(string spatialRelation)
        => Append($"spatialRel={spatialRelation}");

    protected T WithWhereClause(string where)
        => Append($"where={where}");

    protected T WithOrderByFields(string fields)
        => Append($"orderByFields={fields}");

    protected T WithOutFields(string outFields)
        => Append($"outFields={outFields}");

    protected T WithResultRecordCount(int? count)
        => Append($"resultRecordCount={count}");

    protected T WithInSpatialReferenceId(int inSrefId)
    {
        if (inSrefId > 0)
        {
            return Append($"inSR={inSrefId}");
        }

        return _self;
    }

    protected T WithOutSpatialReferenceId(int outSrefId)
    {
        if (outSrefId > 0)
        {
            return Append($"outSR={outSrefId}");
        }

        return _self;
    }

    protected T WithDatumTransformation(int datumTransformationId)
        => datumTransformationId switch
        {
            > 0 => Append($"datumTransformation={datumTransformationId}"),
            _ => _self
        };


    protected T WithReturnZ(bool returnZ = true)
        => returnZ switch
        {
            true => Append($"returnZ=true"),
            false => Append($"returnZ=false")
        };

    protected T WithReturnM(bool returnM = true)
        => returnM switch
        {
            true => Append($"returnM=true"),
            false => Append($"returnM=false")
        };

    protected T WithReturnGeometry(bool returnGeometry = true)
        => returnGeometry switch
        {
            true => Append("returnGeometry=true"),
            false => Append("returnGeometry=false")
        };

    protected T WithReturnCountOnly(bool returnCountOnly = true)
        => returnCountOnly switch
        {
            true => Append("returnCountOnly=true"),
            false => Append("returnCountOnly=false")
        };

    protected T WithReturnIdsOnly(bool returnIdsOnly = true)
        => returnIdsOnly switch
        {
            true => Append("returnIdsOnly=true"),
            false => Append("returnIdsOnly=false")
        };

    protected T WithReturnDistinctValues(bool returnDistinctValues = true)
        => returnDistinctValues switch
        {
            true => Append("returnDistinctValues=true"),
            false => Append("returnDistinctValues=false")
        };

    #endregion

    public string Build() => _builder.ToString();
}