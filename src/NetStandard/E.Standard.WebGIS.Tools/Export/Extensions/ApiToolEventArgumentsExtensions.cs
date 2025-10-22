using E.Standard.Localization.Abstractions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry.Extensions;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Export.Extensions;

static internal class ApiToolEventArgumentsExtensions
{
    static public void ValidateSketch(this ApiToolEventArguments e, ILocalizer localizer)
    {
        if (e?.Sketch?.HasPoints() != true)
        {
            throw new Exception(localizer.Localize("io.exception-no-sketch-defined:body"));
        }
    }

    async static public Task<FeatureCollection> GetFeatureCollectionForMapSeries(this ApiToolEventArguments e, IBridge bridge)
    {
        string queryServiceId = e[MapSeriesPrint.ParameterServiceId];
        string queryId = e[MapSeriesPrint.ParameterQueryId];
        long[] featureIds = e[MapSeriesPrint.ParameterFeatureIds].Split(',').Select(id => long.Parse(id)).ToArray();

        var query = await bridge.GetQuery(queryServiceId, queryId);
        var filter = new ApiOidsFilter(featureIds)
        {
            QueryGeometry = true,
            Fields = QueryFields.Id,
            FeatureSpatialReference = WebMapping.Core.CoreApiGlobals.SRefStore.SpatialReferences.ById(e.CalcCrs.Value)
        };

        var features = await bridge.QueryLayerAsync(queryServiceId, query.GetLayerId(), filter);

        return features;
    }

    static public IUIElement[] AddRequiredMapSeriesPrintCreateFromFeaturesHiddenElements(this ApiToolEventArguments e)
        => new IUIElement[] {
           new UIHidden()
                    .WithId(MapSeriesPrint.ParameterServiceId)
                    .WithStyles(UICss.ToolParameter)
                    .WithValue(e[MapSeriesPrint.ParameterServiceId]),
                new UIHidden()
                    .WithId(MapSeriesPrint.ParameterQueryId)
                    .WithStyles(UICss.ToolParameter)
                    .WithValue(e[MapSeriesPrint.ParameterQueryId]),
                new UIHidden()
                    .WithId(MapSeriesPrint.ParameterFeatureIds)
                    .WithStyles(UICss.ToolParameter)
                    .WithValue(e[MapSeriesPrint.ParameterFeatureIds])
        };

    static public int GetMaxMapSeriesPages(this ApiToolEventArguments e)
        => e.GetConfigInt(MapSeriesPrint.ConfigMaxPages, 5);
}
