using E.Standard.WebGIS.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Api.Core.AppCode.Extensions;

static public class QueryExtensions
{
    static public QueryGeometryType ToQueryGeometryType(this string parameterValue, QueryGeometryType defaultValue = QueryGeometryType.Simple)
    {
        if (String.IsNullOrWhiteSpace(parameterValue))
        {
            return defaultValue;
        }

        QueryGeometryType geometryType;
        if (!Enum.TryParse<QueryGeometryType>(parameterValue, true, out geometryType))
        {
            return defaultValue;
        }

        return geometryType;
    }

    static public IEnumerable<string> AllFieldNames(this E.Standard.Api.App.DTOs.QueryDTO query)
    {
        List<string> fieldNames = new List<string>();

        if (query?.Fields == null)
        {
            fieldNames.Add("*");
        }
        else
        {
            foreach (var field in query.Fields)
            {
                fieldNames.AddRange(field.FeatureFieldNames);
            }
        }

        return fieldNames.Distinct();
    }
}
