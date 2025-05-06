using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;

static internal class JsonFeatureLayerExtensions
{
    static public string IdFieldName(this JsonFeatureServerLayer layer)
    {
        var idFieldName = layer.Fields.FirstOrDefault(f => f.Type == "esriFieldTypeOID").Name;

        if (String.IsNullOrEmpty(idFieldName))
        {
            throw new Exception("Internal Error: IdFieldName not found");
        }

        return idFieldName;
    }

    static public string ShapeFileName(this JsonFeatureServerLayer layer)
    {
        var shapeFieldName = 
            layer.GeometryField?.Name ??
            layer.Fields?.FirstOrDefault(f => f.Type == "esriFieldTypeGeometry")?.Name;

        if (String.IsNullOrEmpty(shapeFieldName))
        {
            throw new Exception("Internal Error: ShapeFieldName not found");
        }

        return shapeFieldName;
    }
}
