using System;
using System.Collections.Specialized;
using System.Linq;

using E.Standard.Api.App.Data;
using E.Standard.Api.App.DTOs;
using E.Standard.Extensions.Compare;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Collections;

namespace Api.Core.AppCode.Extensions;

static public class FeatureCollectionExtensions
{
    static public void Append1toNLinks(this FeatureCollection returnFeatures,
                FeatureCollection queryFeatures,
                QueryDTO query,
                bool renderFields = true,
                QueryFieldRenderingContext renderingContext = null,
                NameValueCollection requestHeaders = null,
                bool usePayload = false,
                ICryptoService crypto = null)
    {
        if (renderFields && query?.Fields != null)
        {
            foreach (TableFieldHotlink hotlinkField in query.Fields.Where(f => f is TableFieldHotlink field && field.One2N == true))
            {
                try
                {
                    CollectionFeature collFeature = new CollectionFeature(queryFeatures, hotlinkField.One2NSeperator);
                    var hotlinkContext = renderingContext?.GetFieldContext(hotlinkField);

                    var hotLinkUrl = String.Empty;

                    hotLinkUrl = hotlinkContext is not null
                        ? hotlinkField.SolveHotlinkUrl(collFeature, hotlinkContext)
                        : Globals.SolveExpression(
                            collFeature,
                            hotlinkField.HotlinkUrl.ReplaceUrlHeaderPlaceholders(requestHeaders));
                    hotLinkUrl = ReplaceObjectId(query, collFeature, hotLinkUrl);

                    if (usePayload && crypto is not null)
                    {
                        hotLinkUrl = $"payload:{crypto.EncryptTextDefault(hotLinkUrl, CryptoResultStringType.Hex)}";
                    }

                    if (!String.IsNullOrEmpty(hotLinkUrl))
                    {
                        returnFeatures.Links ??= new();
                        returnFeatures.LinkTargets ??= new();


                        var colName = hotlinkField.HotlinkName.OrTake(hotlinkField.ColumnName);
                        returnFeatures.Links[colName] = hotLinkUrl;
                        returnFeatures.LinkTargets[colName] = hotlinkField.Target.ToString().ToLowerInvariant();

                        var preparedImageExpression = hotlinkContext is not null
                            ? hotlinkField.PreparedImageExpression(hotlinkContext)
                            : hotlinkField.ImageExpression;
                        var imageHasParameters = hotlinkContext is not null
                            ? hotlinkField.PreparedImageExpressionHasParameters(hotlinkContext)
                            : ExpressionHasParameters(hotlinkField.ImageExpression);

                        if (!String.IsNullOrEmpty(preparedImageExpression) && !imageHasParameters)
                        {
                            returnFeatures.LinkImages ??= new();
                            returnFeatures.LinkImages[colName] = preparedImageExpression;
                        }

                    }
                }
                catch { }
            }
        }
    }


    static private string ReplaceObjectId(
            QueryDTO query,
            E.Standard.WebMapping.Core.Feature feature,
            string expression
        )
    {
        if (query != null)
        {
            if (expression.Contains("{OBJECTID}") && query.Service?.Layers?.FindById(query.LayerId) != null)
            {
                expression = expression.Replace(
                    "{OBJECTID}",
                    feature[query.Service?.Layers?.FindById(query.LayerId).IdFieldName]);
            }
        }

        return expression;
    }

    static private bool ExpressionHasParameters(string expression)
    {
        return expression?.Contains("[") == true
            && expression?.Contains("]") == true;
    }
}
