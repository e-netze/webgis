using E.Standard.Json;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using E.Standard.WebMapping.GeoServices.ArcServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest;

class AnnotationLayer : RestLayer
{
    private new readonly MapService _service;
    private readonly string _id = String.Empty;

    public AnnotationLayer(string name, string id, IMapService service)
        : base(name, id, service, queryable: false)
    {
        _service = (MapService)service;
        _id = id;
    }
    public AnnotationLayer(string name, string id, LayerType type, IMapService service)
        : base(name, id, type, service, queryable: false)
    {
        _service = (MapService)service;
        _id = id;
    }

    //public int ParentId
    //{
    //    get { return _parentId; }
    //}

    async public override Task<bool> GetFeaturesAsync(QueryFilter filter, FeatureCollection features, IRequestContext requestContext)
    {
        if (this.ParentLayerIds == null || this.ParentLayerIds.Length == 0)
        {
            return false;
        }

        string geometryType = String.Empty, geometry = String.Empty;
        //string outSref = (filter.FeatureSpatialReference != null) ? @"{""wkid"":" + filter.FeatureSpatialReference.Id + "}" : String.Empty;
        int outSrefId = (filter.FeatureSpatialReference != null) ? filter.FeatureSpatialReference.Id : 0,
            inSrefId = 0;

        if (filter is SpatialFilter)
        {
            var sFilter = (SpatialFilter)filter;
            inSrefId = sFilter.FilterSpatialReference != null ? sFilter.FilterSpatialReference.Id : 0;
            if (sFilter.QueryShape is Point)
            {
                geometry = RestHelper.ConvertGeometryToJson(((Point)sFilter.QueryShape), inSrefId); // "{x:" + ((Point)sFilter.QueryShape).X.ToString(webgisCMS.Core.Globals.Nhi) + ",y:" + ((Point)sFilter.QueryShape).Y.ToString(webgisCMS.Core.Globals.Nhi) + "}";
                geometryType = RestHelper.GetGeometryTypeString(((Point)sFilter.QueryShape));
            }
            else if (sFilter.QueryShape is MultiPoint)
            {
                geometry = RestHelper.ConvertGeometryToJson(((MultiPoint)sFilter.QueryShape), inSrefId);
                geometryType = RestHelper.GetGeometryTypeString(((MultiPoint)sFilter.QueryShape));
            } // TODO - if needed
            else if (sFilter.QueryShape is Polyline)
            {
                geometry = RestHelper.ConvertGeometryToJson(((Polyline)sFilter.QueryShape), inSrefId); // HELPER CLASS FOR STRING CREATION -> POINTCOLLECTION TO JSON STRING...
                geometryType = RestHelper.GetGeometryTypeString(((Polyline)sFilter.QueryShape));
            }

            else if (sFilter.QueryShape is Polygon)
            {
                geometry = RestHelper.ConvertGeometryToJson(((Polygon)sFilter.QueryShape), inSrefId); // HELPER CLASS FOR STRING CREATION -> POINTCOLLECTION TO JSON STRING...
                geometryType = RestHelper.GetGeometryTypeString(((Polygon)sFilter.QueryShape));
            }
            else if (sFilter.QueryShape is Envelope)
            {
                geometry = RestHelper.ConvertGeometryToJson(((Envelope)sFilter.QueryShape), inSrefId);
                geometryType = RestHelper.GetGeometryTypeString(((Envelope)sFilter.QueryShape));
            }
            else { }
        }

        string separatedSubFieldsString = String.IsNullOrWhiteSpace(filter.SubFields) ? "*" : filter.SubFields.Replace(" ", ",");

        // Bitte nicht ändern -> Verursacht Pagination Error vom AGS und Performance Problem wenn daten im SQL Spatial liegen
        string featureLimit = String.Empty; //(filter.FeatureLimit <= 0 ? "" : filter.FeatureLimit.ToString());

        string featuresReqUrl =
            $"{_service.Service}/{this.ParentLayerIds[0]}/query";

        var jsonFeatureResponse = new JsonFeatureResponse();

        string where = filter.Where;

        if (!String.IsNullOrWhiteSpace(this.Filter))
        {
            where = (String.IsNullOrEmpty(where) ?
                this.Filter : $"{where} and {this.Filter}");
        }

        where = FeatureLayer.UrlEncodeWhere(where);

        if (String.IsNullOrWhiteSpace(where))
        {
            if (!String.IsNullOrEmpty(this.IdFieldName))
            {
                // Empty where is not allowed in REST
                where = $"{this.IdFieldName}>0";  // or 1=1
            }
        }

        string orderBy = filter.OrderBy;

        StringBuilder postBodyData = new StringBuilder();
        postBodyData.Append($"&geometry={geometry}&geometryType={geometryType}&spatialRel=esriSpatialRelIntersects&relationParam=&objectIds=");
        postBodyData.Append($"&where={where}&time=&maxAllowableOffset=&outFields={separatedSubFieldsString}&orderByFields={orderBy}&resultRecordCount={featureLimit}&f=json");

        #region Projection

        if (_service.ProjectionMethode == ServiceProjectionMethode.Map && _service.Map.SpatialReference != null)
        {
            postBodyData.Append($"&inSR={(inSrefId > 0 ? inSrefId : _service.Map.SpatialReference.Id)}&outSR={(outSrefId > 0 ? outSrefId : _service.Map.SpatialReference.Id)}");
        }
        else if (_service.ProjectionMethode == ServiceProjectionMethode.Userdefined && _service.ProjectionId > 0)
        {
            postBodyData.Append($"&inSR={(inSrefId > 0 ? inSrefId : _service.ProjectionId)}&outSR={(outSrefId > 0 ? outSrefId : _service.ProjectionId)}");
        }
        else
        {
            if (inSrefId > 0)
            {
                postBodyData.Append($"&inSR={inSrefId}");
            }

            if (outSrefId > 0)
            {
                postBodyData.Append($"&outSR={outSrefId}");
            }
        }


        if (_service.DatumTransformations?.Length > 0)
        {
            // datumTransformations not in ESRI REST Standard anymore
            // https://developers.arcgis.com/rest/services-reference/enterprise/query-map-service-layer/
            // => use datumTransformation and only the first transformation

            // previous: postBodyData.Append($"&datumTransformations=[{String.Join(",", _service.DatumTransformations)}]");

            postBodyData.Append($"&datumTransformation={_service.DatumTransformations.First()}");
        }

        #endregion

        //if (countOnly)
        //{
        //    postBodyData.Append("&returnCountOnly=true&returnIdsOnly=true&returnGeometry=false");
        //    string featuresResponse = await _service.TryPostAsync(httpService, featuresReqUrl, postBodyData.ToString());

        //    var jsonFeatureCountResponse = JSerializer.Deserialize<JsonFeatureCountResponse>(featuresResponse);

        //    return jsonFeatureCountResponse.Count;
        //}
        //else
        {
            var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();

            postBodyData.Append($"&returnCountOnly=false&returnIdsOnly=false&returnGeometry={filter.QueryGeometry}");
            string featuresResponse = await authHandler.TryPostAsync(_service, featuresReqUrl, postBodyData.ToString());

            jsonFeatureResponse = JSerializer.Deserialize<JsonFeatureResponse>(featuresResponse);
        }

        List<string> dateColumns = new List<string>();
        foreach (var fieldType in jsonFeatureResponse.Fields)
        {
            if (fieldType.Name != null && fieldType.Type != null && fieldType.Type == "esriFieldTypeDate")
            {
                dateColumns.Add(fieldType.Name);
            }
        }

        foreach (var jsonFeature in jsonFeatureResponse.Features)
        {
            Feature feature = new Feature();

            if (jsonFeature.Geometry?.Rings != null)
            {
                Polygon polygon = new Polygon();
                for (int r = 0, to = jsonFeature.Geometry.Rings.Length; r < to; r++)
                {
                    Ring ring = new Ring();

                    var ringsPointsArray = jsonFeature.Geometry.Rings[r];
                    var dimension = ringsPointsArray.GetLength(1); // 2D 3D 3D+M ?
                    int ringsPointsArrayLength = (ringsPointsArray.Length / dimension);

                    for (int multiArrayIndex = 0; multiArrayIndex < ringsPointsArrayLength; multiArrayIndex++)
                    {
                        //Point point = new Point();
                        //point.X = ringsPointsArray[multiArrayIndex, 0];
                        //point.Y = ringsPointsArray[multiArrayIndex, 1];

                        ring.AddPoint(ArrayToPoint(ringsPointsArray, multiArrayIndex, dimension));
                    }
                    polygon.AddRing(ring);
                }

                feature.Shape = polygon;
            }

            var featureAttributes = (IDictionary<string, object>)jsonFeature.Attributes ?? new Dictionary<string, object>();

            foreach (var featureProperty in featureAttributes)
            {
                string name = featureProperty.Key;
                var value = featureProperty.Value;

                if (dateColumns.Contains(name))
                {
                    try
                    {
                        long esriDate = Convert.ToInt64(featureProperty.Value);
                        DateTime td = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(esriDate);
                        feature.Attributes.Add(new Core.Attribute(name, td.ToString()));
                    }
                    catch // Wenn Datum kein long?
                    {
                        feature.Attributes.Add(new Core.Attribute(name, value?.ToString()));
                    }
                }
                else
                {
                    feature.Attributes.Add(new Core.Attribute(name, value?.ToString()));
                }

                if (name == this.IdFieldName)
                {
                    feature.Oid = int.Parse(value.ToString());
                }
            }

            features.Add(feature);
        }

        features.Query = filter;
        features.Layer = this;

        features.HasMore = jsonFeatureResponse.ExceededTransferLimit;

        return true;
    }

    override public ILayer Clone(IMapService parent)
    {
        if (parent is null)
        {
            return null;
        }

        AnnotationLayer clone = new AnnotationLayer(this.Name, this.ID, this.Type, parent);
        clone.ClonePropertiesFrom(this);
        base.CloneParentLayerIdsTo(clone);

        return clone;
    }

    #region Helper

    private Point ArrayToPoint(double?[,] pointArray, int index, int dimension)
    {
        var point = new Point();

        int dimensionIndex = 0;

        point.X = pointArray[index, dimensionIndex++] ?? 0D;
        point.Y = pointArray[index, dimensionIndex++] ?? 0D;

        return point;
    }

    #endregion
}
