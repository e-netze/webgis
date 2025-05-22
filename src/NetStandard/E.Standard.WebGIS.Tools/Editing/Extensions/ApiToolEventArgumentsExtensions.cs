using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebGIS.Tools.Identify;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Extensions;

static internal class ApiToolEventArgumentsExtensions
{
    public static bool UseDesktopBehavior(this ApiToolEventArguments e)
    {
        return e.HasElement("div", new[] { "query-results-tab-control-container" });
    }

    public static IEditToolService EditToolServiceInstance(this ApiToolEventArguments e, IApiTool sender, ILocalizer localizer)
    {
        if (e.UseDesktopBehavior())
        {
            return new EditToolServiceDesktop(sender, localizer);
        }

        return new EditToolServiceMobile(sender, localizer);
    }

    #region Selection

    async public static Task<EditFeatureDefinition> GetEditFeatureDefinitionFromSelection(this ApiToolEventArguments e, IBridge bridge)
    {
        var features = await e.FeaturesFromSelectionAsync(bridge);
        if (features.Count == 0)
        {
            throw new ArgumentException("No feature selected!");
        }

        if (features.Count > 1)
        {
            throw new ArgumentException("More than one feature selected!");
        }

        var editTheme = e.EditThemeFromSelection(bridge);
        if (editTheme == null)
        {
            throw new ArgumentException("Can't find edit theme for selelction");
        }

        var editThemeDef = e.EditFeatureDefinitionFromSelection(bridge, features[0]);
        if (editThemeDef == null)
        {
            throw new ArgumentException("Can't find edit theme for selelction");
        }

        EditFeatureDefinition editFeatureDef = new EditFeatureDefinition()
        {
            EditThemeId = editThemeDef.EditThemeId,
            EditThemeName = editThemeDef.EditThemeName,
            Feature = features[0],
            FeatureOid = features[0].Oid,
            LayerId = editThemeDef.LayerId,
            ServiceId = editThemeDef.ServiceId
        };

        // For PostEvents
        e["_editfield_edittheme_def"] = ApiToolEventArguments.ToArgument(editFeatureDef.ToEditThemeDefinition());

        return editFeatureDef;
    }

    #endregion

    #region EditThemes/Queries

    public static IEnumerable<EditThemeDefinition> GetEditThemeDefintions(this ApiToolEventArguments e, ServiceLayerVisibility visibility)
    {
        List<EditThemeDefinition> result = new List<EditThemeDefinition>();

        string allThemes = e[Edit.EditAllThemesId];

        foreach (var editThemeString in allThemes.Split(';'))
        {
            if (visibility == ServiceLayerVisibility.Visible && !editThemeString.EndsWith(",1"))  // last argument 1 => visible
            {
                continue;
            }

            if (visibility == ServiceLayerVisibility.Invisible && !editThemeString.EndsWith(",0"))  // last argument 0 => invisible
            {
                continue;
            }

            result.Add(ApiToolEventArguments.FromArgument<EditThemeDefinition>(editThemeString));
        }

        return result;
    }

    async public static Task<IEnumerable<IQueryBridge>> GetVisibleQueries(this ApiToolEventArguments e, IBridge bridge, ServiceLayerVisibility visibility)
    {
        var result = new List<IQueryBridge>();

        foreach (var editThemeDefinition in e.GetEditThemeDefintions(visibility))
        {
            var query = await bridge.GetFirstLayerQuery(editThemeDefinition.ServiceId, editThemeDefinition.LayerId);

            if (query != null)
            {
                result.Add(query);
            }
        }

        return result;
    }

    async public static Task<string> GetQueriesString(this ApiToolEventArguments e, IBridge bridge, double? scale = null)
    {
        var visibleQueries = await e.GetVisibleQueries(bridge, ServiceLayerVisibility.Visible);
        var invisibleQueries = await e.GetVisibleQueries(bridge, ServiceLayerVisibility.Invisible);

        var selectedEditTheme = e.GetSelectedEditThemeDefintion();
        if (!String.IsNullOrEmpty(selectedEditTheme?.ServiceId))
        {
            visibleQueries = visibleQueries.Where(q => q.GetServiceId() == selectedEditTheme.ServiceId);
            invisibleQueries = invisibleQueries.Where(q => q.GetServiceId() == selectedEditTheme.ServiceId);
        }

        if (scale.HasValue)
        {
            #region Collection and Distinct ServiceIds

            var serviceIds = new List<string>(visibleQueries.Select(q => q.GetServiceId()).Distinct());
            serviceIds.AddRange(invisibleQueries.Select(q => q.GetServiceId()).Distinct());
            serviceIds = serviceIds.Distinct().ToList();

            #endregion

            #region Iterate Services, get Layer and check Scales

            foreach (var serviceId in serviceIds)
            {
                var service = (await bridge.GetService(serviceId)).ThrowIfNull(() => $"Can't get service with serviceId: {serviceId}");

                visibleQueries = visibleQueries.Where(q => q.GetServiceId() != serviceId || service.FindLayer(q.GetLayerId()).InScale(scale.Value)).ToArray();
                invisibleQueries = invisibleQueries.Where(q => q.GetServiceId() != serviceId || service.FindLayer(q.GetLayerId()).InScale(scale.Value)).ToArray();
            }

            #endregion
        }

        var visibleQueryiesString = String.Join(";", visibleQueries.Select(query => $"{query.QueryGlobalId.Replace(":", ",")},1").ToArray());
        var invisibleQueryiesString = String.Join(";", invisibleQueries.Select(query => $"{query.QueryGlobalId.Replace(":", ",")},0").ToArray());

        if (String.IsNullOrEmpty(invisibleQueryiesString))
        {
            return visibleQueryiesString;
        }

        if (String.IsNullOrEmpty(visibleQueryiesString))
        {
            return invisibleQueryiesString;
        }

        return String.Join(";", new[] { visibleQueryiesString, invisibleQueryiesString });
    }

    static public EditThemeDefinition GetSelectedEditThemeDefintion(this ApiToolEventArguments e)
    {
        var selectedThemeString = e[EditToolServiceDesktop.WebGisEditSelectionThemeId];
        var selectedTheme = !String.IsNullOrEmpty(selectedThemeString) ? ApiToolEventArguments.FromArgument<EditThemeDefinition>(selectedThemeString) : null;

        return selectedTheme;
    }

    async static public Task<IQueryBridge> GetSelectedEditThemeQuery(this ApiToolEventArguments e, IBridge bridge)
    {
        var selectedEditTheme = e.GetSelectedEditThemeDefintion();

        if (String.IsNullOrEmpty(selectedEditTheme?.LayerId))
        {
            return null;
        }

        var query = await bridge.GetFirstLayerQuery(selectedEditTheme.ServiceId, selectedEditTheme.LayerId);

        return query;
    }

    static public ApiToolEventArguments TryAppendEditThemeDefintion(
            this ApiToolEventArguments e,
            string allThemesParameter = Edit.EditAllThemesId,
            string editThemeParameter = "edittheme",
            string targetParameter = "_editfield_edittheme_def")
    {
        try
        {
            string allThemes = e[allThemesParameter];
            string editTheme = e[editThemeParameter];

            string editThemeString = allThemes.Split(';')
                                              .Where(s => s.Split(',').Skip(2).FirstOrDefault() == editTheme)
                                              .FirstOrDefault();
            var editThemeDef = ApiToolEventArguments.FromArgument<EditThemeDefinition>(editThemeString);

            e[targetParameter] = ApiToolEventArguments.ToArgument(editThemeDef);
        }
        catch { }

        return e;
    }

    static public ApiToolEventArguments TryAppendEditThemeOid(
            this ApiToolEventArguments e,
            string featureOidParameter = "feature-oid",
            string targetParameter = "_edittheme_oid")
    {
        try
        {
            var featureOid = e[featureOidParameter].ParseFeatureGlobalOid();

            e[targetParameter] = featureOid.featureId.ToString();
        }
        catch { }

        return e;
    }

    #endregion

    #region QueryBuilder

    public static bool HasQueryBuilderQueryDefinition(this ApiToolEventArguments e, string queryBuilderElementId)
        => !e.IsEmpty($"{queryBuilderElementId}-result");


    public static UIQueryBuilder.Result QueryBuilderResult(this ApiToolEventArguments e, string queryBuilderElementId)
    {
        if (!e.HasQueryBuilderQueryDefinition(queryBuilderElementId))
        {
            return null;
        }

        return JSerializer.Deserialize<UIQueryBuilder.Result>(e[$"{queryBuilderElementId}-result"]);
    }

    #endregion

    #region Append 

    #region QueryBuilder

    public static ApiToolEventArguments AppendIdentifyWhereClauseFromQueryBuilder(this ApiToolEventArguments e, string queryBuilderElementId)
    {
        return e.AppendIdentifyWhereClauseFromQueryBuilder(e.QueryBuilderResult(queryBuilderElementId));
    }

    public static ApiToolEventArguments AppendIdentifyWhereClauseFromQueryBuilder(this ApiToolEventArguments e, UIQueryBuilder.Result queryBuilderResult)
    {
        if (queryBuilderResult?.QueryDefs == null)
        {
            return e;
        }

        StringBuilder whereClause = new StringBuilder();

        foreach (var queryDef in queryBuilderResult.QueryDefs)
        {
            string value = String.Format(queryDef.ValueTemplate.OrTake("{0}"), queryDef.Value);

            whereClause.Append($"{queryDef.Field}{queryDef.Operator}{value}");
            if (!String.IsNullOrEmpty(queryDef.LogicalOperator))
            {
                whereClause.Append($" {queryDef.LogicalOperator} ");
            }
        }

        if (whereClause.Length > 0)
        {
            e[IdentifyDefault.IdentifyWhereClause] = whereClause.ToString();
        }

        return e;
    }

    #endregion

    async static public Task<ApiToolEventArguments> AppendIdentifyAllQueriesString(this ApiToolEventArguments e, IBridge bridge, bool applyScale = false)
    {
        e[IdentifyDefault.IdentifyAllQueriesId] = await e.GetQueriesString(bridge, applyScale ? e.MapScale : null);

        return e;
    }

    static public ApiToolEventArguments AppendIdentifyQueryThemeId(this ApiToolEventArguments e, IQueryBridge queryBridge, string queryThemeConstId = IdentifyConst.QueryVisibleIgnoreFavorites)
    {
        e[IdentifyDefault.IdentifyQueryThemeId] = queryBridge != null ? queryBridge.QueryGlobalId : queryThemeConstId;

        return e;
    }

    static public ApiToolEventArguments AppendIdentifyMultiResultTarget(this ApiToolEventArguments e, string target)
    {
        e[IdentifyDefault.IdentifyMultiResultTarget] = target;

        return e;
    }

    static public ApiToolEventArguments AppendIdentifyForceCheckboxes(this ApiToolEventArguments e)
    {
        e[IdentifyDefault.IdentifyForceCheckboxes] = "true";

        return e;
    }

    static public ApiToolEventArguments AppendIdentifyUiPrefix(this ApiToolEventArguments e, string prefix)
    {
        e[IdentifyDefault.IdentifyUiPrefix] = prefix;

        return e;
    }

    static public ApiToolEventArguments AppendIdentifyFeatureLimit(this ApiToolEventArguments e, int limit)
    {
        e[IdentifyDefault.IdentifyFeatureLimiit] = limit.ToString();

        return e;
    }

    static public ApiToolEventArguments AppendIdentiyIgnoreQueryShape(this ApiToolEventArguments e, bool ignore = true)
    {
        e[IdentifyDefault.IdentifyIgnoreQueryShape] = ignore ? "true" : "false";

        return e;
    }

    #endregion

    public static bool RequireCrsP4Parameters(this ApiToolEventArguments e, int sketchCrsId)
    {
        return sketchCrsId != e.MapCrs ||
               sketchCrsId != e.CalcCrs ||
               e.CalcCrsIsDynamic == true;
    }
}
