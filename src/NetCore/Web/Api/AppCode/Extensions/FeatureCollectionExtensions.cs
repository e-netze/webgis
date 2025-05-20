using E.Standard.Api.App.DTOs;
using E.Standard.Extensions.Compare;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Api.Core.AppCode.Extensions;

static public class FeatureCollectionExtensions
{
    static public void Append1toNLinks(this FeatureCollection returnFeatures,
                FeatureCollection queryFeatures,
                QueryDTO query,
                bool renderFields = true,
                NameValueCollection requestHeaders = null)
    {
        if (renderFields && query?.Fields != null)
        {
            foreach (TableFieldHotlinkDTO hotlinkField in query.Fields.Where(f => f is TableFieldHotlinkDTO field && field.One2N == true))
            {
                try
                {
                    CollectionFeature collFeature = new CollectionFeature(queryFeatures, hotlinkField.One2NSeperator);

                    var hotLinkUrl = String.Empty;

                    hotLinkUrl = SolveExpression(query,
                                         collFeature,
                                         hotlinkField.HotlinkUrl.ReplaceUrlHeaderPlaceholders(requestHeaders));

                    
                    if (!String.IsNullOrEmpty(hotLinkUrl))
                    {
                        returnFeatures.Links ??= new();
                        returnFeatures.LinkTargets ??= new();

                        var colName = hotlinkField.HotlinkName.OrTake(hotlinkField.ColumnName);
                        returnFeatures.Links[colName] = hotLinkUrl;
                        returnFeatures.LinkTargets[colName] = hotlinkField.Target.ToString().ToLowerInvariant();
                    }
                }
                catch { }
            }
        }
    }


    static private string SolveExpression(
            QueryDTO query,
            E.Standard.WebMapping.Core.Feature feature,
            string expression
        )
    {
        string expr = Globals.SolveExpression(feature, expression);

        if (query != null)
        {
            if (expr.Contains("{OBJECTID}") && query.Service?.Layers?.FindById(query.LayerId) != null)
            {
                expr = expr.Replace("{OBJECTID}", feature[query.Service?.Layers?.FindById(query.LayerId).IdFieldName]);
            }
        }
        return expr;
    }
}
