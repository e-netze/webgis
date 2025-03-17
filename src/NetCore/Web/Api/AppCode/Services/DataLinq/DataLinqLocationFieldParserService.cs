using Api.Core.Models.DataLinq;
using E.DataLinq.Core.Engines.Abstraction;
using E.DataLinq.Core.Services.Abstraction;
using E.Standard.Api.App;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using System;

namespace Api.Core.AppCode.Services.DataLinq;

public class DataLinqLocationFieldParserService : IEngineFieldParserService
{
    public object Parse(IRecordReader reader, string columnName)
    {
        if ("_location".Equals(columnName, StringComparison.OrdinalIgnoreCase))
        {
            Shape location = reader.GetValue("_location").ToString().ShapeFromWKT();
            if (location == null || !(location is Point))
            {
                throw new ArgumentException("Field '_location' has to return a point feature in Well-Known-Text format.");
            }

            if (reader.HasCoumn("_location_srid"))
            {
                using (var transformer = new GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, (int)reader.GetValue("_location_srid"), 4326))
                {
                    transformer.Transform(location);
                }
            }

            return new RecordLocation()
            {
                Longitude = (location as Point).X,
                Latitude = (location as Point).Y
            };
        }

        return null;
    }
}
