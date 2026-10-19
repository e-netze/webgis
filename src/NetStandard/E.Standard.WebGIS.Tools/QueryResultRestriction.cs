using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using E.Standard.Extensions.Compare;
using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Extensions;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebGIS.Tools.Helpers;
using E.Standard.WebGIS.Tools.Identify;
using E.Standard.WebGIS.Tools.Identify.Extensions;
using E.Standard.WebGIS.Tools.Presentation.Extensions;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Models;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using E.Standard.WebMapping.Core.Exceptions;
using E.Standard.WebMapping.Core.Extensions;

namespace E.Standard.WebGIS.Tools;

/// <summary>
/// Restricts a query result (result table) with the QueryBuilder.
/// Not a toolbar tool: it is only called via server commands from the result table.
/// The original query (query, effective geometry, form items, where, limit) comes as an encrypted token
/// (<see cref="QueryRestrictionDescriptor"/>, metadata "query_restriction" of the result) and is re-executed
/// with the additional QueryBuilder where clause. This is a result filter, not a display filter.
/// </summary>
[Export(typeof(IApiButton))]
[AdvancedToolProperties(VisFilterDependent = true)]
public class QueryResultRestriction : IApiServerButtonLocalizableAsync<QueryResultRestriction>
{
    public const string QueryBuilderId = "query-result-restriction";
    public const string RestrictCommand = "restrict";
    public const string QueryBuilderValuesCommand = "querybuilder-values";
    public const string RemoveRestrictionCommand = "remove-restriction";
    public const string ShowGeometryCommand = "show-geometry";

    #region IApiButton

    public string Name => "Restrict Query Result";

    public string Container => String.Empty;

    public string Image => String.Empty;

    public string ToolTip => "Restrict the query result with the query builder";

    public bool HasUI => false;

    public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<QueryResultRestriction> localizer)
        => Task.FromResult<ApiEventResponse>(null);

    #endregion

    #region Commands

    [ServerToolCommand(RestrictCommand)]
    async public Task<ApiEventResponse> OnRestrict(IBridge bridge, ApiToolEventArguments e, ILocalizer<QueryResultRestriction> localizer)
    {
        var token = e.ServerCommandArgument;
        var descriptor = QueryRestrictionDescriptor.FromToken(bridge.CryptoService, token);
        var (query, fields) = await QueryAndAllowedFields(bridge, descriptor);

        var uiQueryBuilder = new UIQueryBuilder()
        {
            id = QueryBuilderId,
            ShowGeometryOption = false,
            CallbackToolId = this.GetType().ToToolId(),
            CallbackArgument = token,
            CallbackVisFilterDependent = true,
            RemoveButtonLabel = String.IsNullOrWhiteSpace(descriptor.Restriction)
                ? null
                : localizer.Localize("remove-restriction"),
            ValuesSource = UIInputAutocomplete.MethodSource(bridge, typeof(QueryResultRestriction), QueryBuilderValuesCommand, new
            {
                qb_request = Uri.EscapeDataString(token)
            })
        };

        foreach (var field in fields)  // sorting comes from CMS
        {
            uiQueryBuilder.TryAddField(field.Name, field.Type, field.Alias);
        }

        if (!String.IsNullOrWhiteSpace(descriptor.Restriction))
        {
            uiQueryBuilder.TrySetWhereClause(descriptor.Restriction);
        }

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[]
            {
                new UIDiv()
                {
                    target = UIElementTarget.modaldialog.ToString(),
                    targettitle = localizer.Localize("title"),
                    targetwidth = "600px",
                    targetheight = "400px",
                    elements = new IUIElement[] { OriginInfo(descriptor, query, token, localizer), uiQueryBuilder }
                }
            }
        };
    }

    [ServerToolCommand(QueryBuilderValuesCommand)]
    async public Task<ApiEventResponse> OnQueryBuilderValues(IBridge bridge, ApiToolEventArguments e)
    {
        var descriptor = QueryRestrictionDescriptor.FromToken(bridge.CryptoService, e["qb_request"]);
        var (query, _) = await QueryAndAllowedFields(bridge, descriptor);

        return await QueryBuilderValuesHelper.ValuesResponseAsync(bridge, e,
            [(descriptor.ServiceId, query.GetLayerId())]);
    }

    [ServerToolCommand("querybuilder")]
    async public Task<ApiEventResponse> OnQueryBuilder(IBridge bridge, ApiToolEventArguments e, ILocalizer<QueryResultRestriction> localizer)
    {
        var descriptor = QueryRestrictionDescriptor.FromToken(bridge.CryptoService, e.ServerCommandArgument);
        var (query, fields) = await QueryAndAllowedFields(bridge, descriptor);

        var queryBuilderResult = UIQueryBuilder.Sanitize(e.QueryBuilderResult(QueryBuilderId), fields);

        e[IdentifyDefault.IdentifyWhereClause] = null;
        e.AppendIdentifyWhereClauseFromQueryBuilder(queryBuilderResult);
        var restriction = e[IdentifyDefault.IdentifyWhereClause];

        return await PerformAsync(bridge, e, descriptor, query, restriction,
                                  noResultsMessage: localizer.Localize("no-results"));
    }

    [ServerToolCommand(RemoveRestrictionCommand)]
    async public Task<ApiEventResponse> OnRemoveRestriction(IBridge bridge, ApiToolEventArguments e)
    {
        var descriptor = QueryRestrictionDescriptor.FromToken(bridge.CryptoService, e.ServerCommandArgument);
        var (query, _) = await QueryAndAllowedFields(bridge, descriptor);

        return await PerformAsync(bridge, e, descriptor, query, null);
    }

    /// <summary>
    /// Shows the query geometry of the original query for a moment (preview, not a graphics element)
    /// </summary>
    [ServerToolCommand(ShowGeometryCommand)]
    public ApiEventResponse OnShowGeometry(IBridge bridge, ApiToolEventArguments e)
    {
        var descriptor = QueryRestrictionDescriptor.FromToken(bridge.CryptoService, e.ServerCommandArgument);
        var shape = descriptor.QueryShape();

        if (shape == null || descriptor.Srs <= 0)
        {
            return null;
        }

        if (descriptor.Srs != 4326)
        {
            using (var transformer = bridge.GeometryTransformer(descriptor.Srs, 4326))
            {
                transformer.Transform(shape);
            }
        }

        var envelope = shape.ShapeEnvelope;

        return new ApiEventResponse()
        {
            ClientCommands = new[] { ApiClientButtonCommand.showpreviewgeometry },
            ClientCommandData = new
            {
                geometry = bridge.ToGeoJson(new WebMapping.Core.Collections.FeatureCollection(new WebMapping.Core.Feature() { Shape = shape })),
                bbox = envelope != null
                    ? new double[] { envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY }
                    : null
            }
        };
    }

    #endregion

    #region Helper

    /// <summary>
    /// Re-executes the original query (geometry, items, where, limit) with the restriction (null => unrestricted).
    /// noResultsMessage: an empty restricted result (no hits or an invalid condition) throws an InfoException,
    /// so the dialog stays open with the conditions and the current result is kept.
    /// </summary>
    async static private Task<ApiEventResponse> PerformAsync(IBridge bridge, ApiToolEventArguments e, QueryRestrictionDescriptor descriptor, IQueryBridge query, string restriction, string noResultsMessage = null)
    {
        descriptor.Restriction = String.IsNullOrWhiteSpace(restriction) ? null : restriction;

        var queryShape = descriptor.QueryShape();
        ApiQueryFilter filter = queryShape != null
            ? new ApiSpatialFilter()
            {
                QueryShape = queryShape,
                FilterSpatialReference = bridge.CreateSpatialReference(descriptor.Srs)
            }
            : new ApiQueryFilter();

        filter.QueryItems = descriptor.QueryItems();
        filter.QueryGeometry = true;
        filter.FeatureSpatialReference = bridge.GetSupportedSpatialReference(query, bridge.DefaultSrefId);

        var features = await query.PerformAsync(bridge.RequestContext, filter,
                                                appendFilterClause: descriptor.EffectiveWhereClause(descriptor.Restriction),
                                                limit: descriptor.Limit);

        if (noResultsMessage != null && descriptor.Restriction != null && (features == null || features.Count == 0))
        {
            var details = (features?.Warnings ?? []).Concat(features?.Informations ?? [])
                                                     .Where(m => !String.IsNullOrWhiteSpace(m));

            throw new InfoException(String.Join("\n\n", new[] { noResultsMessage }.Concat(details)));
        }

        var response = new ApiFeaturesEventResponse()
            .AddFeatures(features, FeatureResponseType.New, appendHoverShapes: Identify.Extensions.ApiToolEventArgumentsExtensions.UseDesktopBehavior(e))
            .AddFeaturesQuery(query)
            .AddFeaturesFilter(filter)
            .AddFeaturesSpatialReference(filter.FeatureSpatialReference)
            .SelectFeaturesResult(query.IsSelectable)
            .ZoomToFeaturesResult(false);

        response.QueryRestriction = descriptor;
        response.UIElements = new IUIElement[]
        {
            new UIEmpty().WithTarget(UIElementTarget.modaldialog.ToString())
        };

        return response;
    }

    /// <summary>
    /// What the result is based on (query, query geometry, detail search items, original where),
    /// so the user knows, that the QueryBuilder conditions restrict this result further
    /// </summary>
    static private IUIElement OriginInfo(QueryRestrictionDescriptor descriptor, IQueryBridge query, string token, ILocalizer<QueryResultRestriction> localizer)
    {
        var elements = new List<IUIElement>()
        {
            InfoLine(String.Format(localizer.Localize("info-query"), query.Name))
        };

        if (!String.IsNullOrEmpty(descriptor.Geometry))
        {
            elements.Add(new UIDiv()
            {
                elements = new IUIElement[]
                {
                    new UILiteral()
                    {
                        literal = localizer.Localize(descriptor.Source switch
                        {
                            QueryRestrictionDescriptor.Sources.Identify => "info-geometry-identify",
                            QueryRestrictionDescriptor.Sources.Buffer => "info-geometry-buffer",
                            QueryRestrictionDescriptor.Sources.MapExtent => "info-geometry-bbox",
                            _ => "info-geometry"
                        })
                    },
                    new UICallbackButton<QueryResultRestriction>(new QueryResultRestriction(), ShowGeometryCommand)
                    {
                        text = localizer.Localize("show-geometry"),
                        buttoncommand_argument = token,
                        css = UICss.ToClass(new[] { UICss.CancelButtonStyle, "webgis-query-restriction-show-geometry" })
                    }
                }
            });
        }

        if (descriptor.Source == QueryRestrictionDescriptor.Sources.Table)
        {
            elements.Add(InfoLine(localizer.Localize("info-table")));
        }

        if (descriptor.Items?.Any() == true)
        {
            elements.Add(InfoLine(String.Format(localizer.Localize("info-search-items"),
                String.Join(", ", descriptor.Items.Select(item =>
                    $"{(descriptor.ItemLabels?.TryGetValue(item.Key, out var label) == true ? label : item.Key)}: {item.Value}")))));
        }

        if (!String.IsNullOrWhiteSpace(descriptor.Where))
        {
            elements.Add(InfoLine(String.Format(localizer.Localize("info-where"), descriptor.Where)));
        }

        elements.Add(InfoLine(localizer.Localize("info-restrict")));

        return new UIDiv()
        {
            css = UICss.ToClass(new[] { "webgis-info" }),
            elements = elements.ToArray()
        };
    }

    static private IUIElement InfoLine(string text) => new UIDiv()
    {
        elements = new IUIElement[] { new UILiteral() { literal = text } }
    };

    async static private Task<(IQueryBridge query, IField[] fields)> QueryAndAllowedFields(IBridge bridge, QueryRestrictionDescriptor descriptor)
    {
        var query = (await bridge.GetQuery(descriptor.ServiceId, descriptor.QueryId))
            .ThrowIfNull(() => "Unknown query");

        var allowedQueryBuilderFields = bridge.GetAllowedQueryBuilderFields(descriptor.ServiceId);
        if (allowedQueryBuilderFields?.Any() != true)
        {
            throw new Exception("Query builder is not allowed for this service");
        }

        var layerFields = (await bridge.GetServiceLayerFields(descriptor.ServiceId, query.GetLayerId()))?
            .Where(f => f.Type != WebMapping.Core.FieldType.Shape);

        var fields = layerFields?.Any() == true
            ? allowedQueryBuilderFields.SelectAllowedFields(layerFields).ToArray()
            : [];

        if (fields.Length == 0)
        {
            throw new Exception("No query builder fields found");
        }

        return (query, fields);
    }

    #endregion
}
