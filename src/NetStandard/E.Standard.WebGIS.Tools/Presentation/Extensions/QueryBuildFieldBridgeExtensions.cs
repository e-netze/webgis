#nullable enable

using E.Standard.Extensions.Compare;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Presentation.Extensions;

internal static class QueryBuildFieldBridgeExtensions
{
    static public IEnumerable<IField> SelectAllowedFields(
        this IEnumerable<IQueryBuilderFieldBridge> allowedFields,
        IEnumerable<IField> layerFields)
    {
        if (allowedFields.Any(f => f.Name.Equals("*")))
        {
            return layerFields;
        }

        List<IField> result = new();

        foreach (var allowedField in allowedFields)
        {
            var candidate = layerFields.FirstOrDefault(f => allowedField.Name.Equals(f.Name, StringComparison.OrdinalIgnoreCase));
            
            if (candidate != null)
            {
                result.Add(new Field(
                        candidate.Name,
                        alias: allowedField.Aliasname.OrTake(null),
                        type: candidate.Type)
                    );
            }
        }

        return result;
    }

    static public void CheckIfOnlyAllowedQueryFieldsIncludedInResult(
        this IEnumerable<IQueryBuilderFieldBridge> allowedFields,
        UIQueryBuilder.Result queryBuilderResult)
    {
        if (allowedFields?.Any(qbField => qbField.Name == "*") == true)
        {
            // All fields are allowed
            return;
        }

        foreach (var queryDef in queryBuilderResult.QueryDefs ?? [])
        {
            if (allowedFields?.Any(qbField => qbField.Name == queryDef.Field) != true)
            {
                // Handle the case where an allowed field is not included in the result
                throw new Exception($"The field '{queryDef.Field}' is not allowed in the query result.");
            }
        }
    }
}
