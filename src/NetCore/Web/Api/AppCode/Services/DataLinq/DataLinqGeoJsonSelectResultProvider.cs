using Api.Core.Models.DataLinq;
using E.DataLinq.Core.Services.Abstraction;
using E.Standard.Api.App.DTOs;
using System.Collections.Generic;
using static E.Standard.Api.App.DTOs.FeatureDTO;

namespace Api.Core.AppCode.Services.DataLinq;

public class DataLinqGeoJsonSelectResultProvider : ISelectResultProvider
{
    #region ISelectResultProvider

    public string ResultViewId => "geojson";

    public (object result, string contentType) Transform(IDictionary<string, object>[] records)
    {
        string contentType = "application/json";

        var featuresList = new List<FeatureDTO>();

        foreach (var record in records)
        {
            var feature = new FeatureDTO();
            var properties = new Dictionary<string, object>();

            foreach (var key in record.Keys)
            {
                if (key == "_location" && record[key] is RecordLocation)
                {
                    var location = (RecordLocation)record[key];
                    feature.geometry = new JsonPointGeometry()
                    {
                        coordinates = new double[] { location.Longitude, location.Latitude }
                    };
                }
                else if (key == "_location_srid")
                {
                    // ignore
                }
                else
                {
                    properties[key] = record[key];
                }
            }

            feature.properties = properties;
            featuresList.Add(feature);
        }

        return (result: new
        {
            type = "FeatureCollection",
            features = featuresList.ToArray()
        }, contentType: contentType);
    }

    #endregion
}
