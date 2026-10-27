using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
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
using E.Standard.WebMapping.Core.Models;

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

    public const string StatisticsCommand = "statistics";
    public const string StatisticsFieldCommand = "statistics-field";
    public const string StatisticsModeCommand = "statistics-mode";
    public const string StatisticsSortCommand = "statistics-sort";
    public const string StatisticsCsvCommand = "statistics-csv";
    public const string StatisticsRestrictCommand = "statistics-restrict";

    private const string StatisticsTokenId = "webgis-qr-statistics-token";
    private const string StatisticsFieldId = "webgis-qr-statistics-field";
    private const string StatisticsModeId = "webgis-qr-statistics-mode";
    private const string StatisticsSortId = "webgis-qr-statistics-sort";
    private const string StatisticsResultId = "webgis-qr-statistics-result";

    private const string StatisticsModeValues = "values";
    private const string StatisticsModeGroups = "groups";
    private const string StatisticsSortAscending = "asc";
    private const string StatisticsSortDescending = "desc";
    private const string StatisticsRankItemCommand = "rank";
    private const int MaxStatisticsRankValues = 1000;  // e.g. Oracle: max. 1000 expressions in a list


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

    #region Statistics

    /// <summary>
    /// Statistics dialog: server side statistics of one (QueryBuilder allowed) field over the whole query result
    /// (all matching features, not only the loaded ones => correct also with has_more)
    /// </summary>
    [ServerToolCommand(StatisticsCommand)]
    async public Task<ApiEventResponse> OnStatistics(IBridge bridge, ApiToolEventArguments e, ILocalizer<QueryResultRestriction> localizer)
    {
        var token = e.ServerCommandArgument;
        var descriptor = QueryRestrictionDescriptor.FromToken(bridge.CryptoService, token);
        var (query, fields) = await QueryAndAllowedFields(bridge, descriptor);
        var field = fields.First();

        var fieldSelect = new UISelect(UIButton.UIButtonType.servertoolcommand_ext, StatisticsFieldCommand)
        {
            id = StatisticsFieldId,
            css = UICss.ToClass(new[] { UICss.ToolParameter }),
            changetool = this.GetType().ToToolId(),
            defaultvalue = field.Name,
            options = fields
                .Select(f => new UISelect.Option()
                {
                    value = f.Name,
                    label = WebUtility.HtmlEncode(String.IsNullOrWhiteSpace(f.Alias) ? f.Name : f.Alias)
                })
                .ToArray()
        };

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[]
            {
                new UIDiv()
                {
                    target = UIElementTarget.modaldialog.ToString(),
                    targettitle = localizer.Localize("statistics-title"),
                    targetwidth = "600px",
                    targetheight = "600px",
                    elements = new IUIElement[]
                    {
                        OriginInfo(descriptor, query, token, localizer, forStatistics: true),
                        new UIHidden()
                        {
                            id = StatisticsTokenId,
                            css = UICss.ToClass(new[] { UICss.ToolParameter }),
                            value = token
                        },
                        StatisticsFormRow(localizer.Localize("statistics-field"), fieldSelect),
                        new UIDiv()
                        {
                            id = StatisticsResultId,
                            elements = new IUIElement[]
                            {
                                await StatisticsResultAsync(bridge, descriptor, query, field, StatisticsModeValues, StatisticsSortDescending, localizer)
                            }
                        }
                    }
                }
            }
        };
    }

    [ServerToolCommand(StatisticsFieldCommand)]
    async public Task<ApiEventResponse> OnStatisticsField(IBridge bridge, ApiToolEventArguments e, ILocalizer<QueryResultRestriction> localizer)
        => await StatisticsRefreshAsync(bridge, e, StatisticsModeValues, localizer);

    [ServerToolCommand(StatisticsModeCommand)]
    async public Task<ApiEventResponse> OnStatisticsMode(IBridge bridge, ApiToolEventArguments e, ILocalizer<QueryResultRestriction> localizer)
        => await StatisticsRefreshAsync(bridge, e, e[StatisticsModeId], localizer);

    [ServerToolCommand(StatisticsSortCommand)]
    async public Task<ApiEventResponse> OnStatisticsSort(IBridge bridge, ApiToolEventArguments e, ILocalizer<QueryResultRestriction> localizer)
        => await StatisticsRefreshAsync(bridge, e, e[StatisticsModeId], localizer);

    /// <summary>
    /// Downloads the currently shown statistics (field, mode, sort order) as CSV
    /// </summary>
    [ServerToolCommand(StatisticsCsvCommand)]
    async public Task<ApiEventResponse> OnStatisticsCsv(IBridge bridge, ApiToolEventArguments e, ILocalizer<QueryResultRestriction> localizer)
    {
        var descriptor = QueryRestrictionDescriptor.FromToken(bridge.CryptoService, e[StatisticsTokenId]);
        var (query, fields) = await QueryAndAllowedFields(bridge, descriptor);
        var field = StatisticsField(fields, e[StatisticsFieldId]);
        var fieldKind = StatisticsFieldKind(field);
        var groupByValues = fieldKind == LayerStatisticsFieldKind.Text
                         || (fieldKind == LayerStatisticsFieldKind.Number && e[StatisticsModeId] == StatisticsModeGroups);

        var statistics = await QueryStatisticsAsync(bridge, descriptor, query, field, fieldKind, groupByValues, localizer);
        var culture = StatisticsCulture(localizer);
        var fieldTitle = String.IsNullOrWhiteSpace(field.Alias) ? field.Name : field.Alias;

        IEnumerable<string[]> rows;
        if (groupByValues)
        {
            var groupRows = StatisticsGroupRows(statistics.Groups,
                                                sortDescending: e[StatisticsSortId] != StatisticsSortAscending,
                                                g => StatisticsGroupCsvValue(g, culture, localizer),
                                                culture);
            rows = new[]
            {
                new[]
                {
                    localizer.Localize("statistics-column-index"),
                    localizer.Localize("statistics-column-rank"),
                    fieldTitle,
                    localizer.Localize("statistics-column-count"),
                    localizer.Localize("statistics-column-share")
                }
            }
            .Concat(groupRows.Select(g => new[]
            {
                g.Index.ToString(CultureInfo.InvariantCulture),
                g.Rank.ToString(CultureInfo.InvariantCulture),
                g.Text,
                g.Group.Count.ToString(CultureInfo.InvariantCulture),
                (100D * StatisticsShare(g.Group.Count, statistics.TotalCount)).ToString("0.##", culture)
            }));
        }
        else
        {
            rows = new[] { new[] { fieldTitle, localizer.Localize("statistics-column-value") } }
                .Concat(StatisticsValueRows(statistics, fieldKind, culture, localizer).Select(r => new[] { r.label, r.value }));
        }

        return new ApiRawDownloadEventResponse($"statistics-{StatisticsFileName(field.Name)}.csv",
                                               bridge.DefaultTextEncoding.GetBytes(StatisticsCsv(rows)));  // UTF8-BOM => umlauts in Excel
    }

    /// <summary>
    /// Click on a group (value) => restricts the result to this value (ANDed with an existing restriction)
    /// </summary>
    [ServerToolCommand(StatisticsRestrictCommand)]
    async public Task<ApiEventResponse> OnStatisticsRestrict(IBridge bridge, ApiToolEventArguments e, ILocalizer<QueryResultRestriction> localizer)
    {
        var descriptor = QueryRestrictionDescriptor.FromToken(bridge.CryptoService, e[StatisticsTokenId]);
        var (query, fields) = await QueryAndAllowedFields(bridge, descriptor);
        var field = StatisticsField(fields, e[StatisticsFieldId]);

        if (StatisticsFieldKind(field) == LayerStatisticsFieldKind.Date)
        {
            throw new ArgumentException($"Restricting by group values is not supported for field {field.Name}");
        }

        var condition = StatisticsGroupCondition(field.Name, e["menuitem-value"]);

        if (e["menuitem-item-command"] == StatisticsRankItemCommand)
        {
            // click on the rank => all values with the same count (recalculated, the client sends only the clicked value)
            var fieldKind = StatisticsFieldKind(field);
            var statistics = await QueryStatisticsAsync(bridge, descriptor, query, field, fieldKind, groupByValues: true, localizer);
            var clicked = statistics.Groups.FirstOrDefault(g => System.Text.Json.JsonSerializer.Serialize(g.Value) == e["menuitem-value"]);

            if (clicked != null)
            {
                condition = StatisticsRankCondition(field.Name, statistics.Groups.Where(g => g.Count == clicked.Count), localizer);
            }
        }

        return await PerformAsync(bridge, e, descriptor, query,
                                  CombineRestriction(descriptor.Restriction, condition),
                                  noResultsMessage: localizer.Localize("no-results"));
    }

    #endregion

    #endregion

    #region Helper

    async private Task<ApiEventResponse> StatisticsRefreshAsync(IBridge bridge, ApiToolEventArguments e, string mode, ILocalizer<QueryResultRestriction> localizer)
    {
        var descriptor = QueryRestrictionDescriptor.FromToken(bridge.CryptoService, e[StatisticsTokenId]);
        var (query, fields) = await QueryAndAllowedFields(bridge, descriptor);
        var field = StatisticsField(fields, e[StatisticsFieldId]);

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[]
            {
                new UIDiv()
                {
                    target = $"#{StatisticsResultId}",
                    elements = new IUIElement[]
                    {
                        await StatisticsResultAsync(bridge, descriptor, query, field, mode, e[StatisticsSortId], localizer)
                    }
                }
            }
        };
    }

    async private Task<IUIElement> StatisticsResultAsync(IBridge bridge, QueryRestrictionDescriptor descriptor, IQueryBridge query, IField field, string mode, string sort, ILocalizer<QueryResultRestriction> localizer)
    {
        var fieldKind = StatisticsFieldKind(field);
        var groupByValues = fieldKind == LayerStatisticsFieldKind.Text
                         || (fieldKind == LayerStatisticsFieldKind.Number && mode == StatisticsModeGroups);

        var statistics = await QueryStatisticsAsync(bridge, descriptor, query, field, fieldKind, groupByValues, localizer);

        var culture = StatisticsCulture(localizer);
        var elements = new List<IUIElement>();

        if (fieldKind == LayerStatisticsFieldKind.Number)
        {
            elements.Add(StatisticsFormRow(localizer.Localize("statistics-mode"), new UISelect(UIButton.UIButtonType.servertoolcommand_ext, StatisticsModeCommand)
            {
                id = StatisticsModeId,
                css = UICss.ToClass(new[] { UICss.ToolParameter }),
                changetool = this.GetType().ToToolId(),
                defaultvalue = groupByValues ? StatisticsModeGroups : StatisticsModeValues,
                options = new[]
                {
                    new UISelect.Option() { value = StatisticsModeValues, label = WebUtility.HtmlEncode(localizer.Localize("statistics-mode-values")) },
                    new UISelect.Option() { value = StatisticsModeGroups, label = WebUtility.HtmlEncode(localizer.Localize("statistics-mode-groups")) }
                }
            }));
        }

        var sortDescending = sort != StatisticsSortAscending;  // default: descending (rank 1 on top)
        if (groupByValues)
        {
            elements.Add(StatisticsFormRow(localizer.Localize("statistics-sort"), new UISelect(UIButton.UIButtonType.servertoolcommand_ext, StatisticsSortCommand)
            {
                id = StatisticsSortId,
                css = UICss.ToClass(new[] { UICss.ToolParameter }),
                changetool = this.GetType().ToToolId(),
                defaultvalue = sortDescending ? StatisticsSortDescending : StatisticsSortAscending,
                options = new[]
                {
                    new UISelect.Option() { value = StatisticsSortDescending, label = WebUtility.HtmlEncode(localizer.Localize("statistics-sort-desc")) },
                    new UISelect.Option() { value = StatisticsSortAscending, label = WebUtility.HtmlEncode(localizer.Localize("statistics-sort-asc")) }
                }
            }));
        }

        var summary = new List<IUIElement>()
        {
            new UILiteralBold() { literal = String.Format(localizer.Localize("statistics-header"), statistics.TotalCount.ToString("N0", culture)) }
        };
        if (groupByValues)
        {
            summary.Add(new UIDiv()
            {
                css = "webgis-statistics-classes",
                elements = new IUIElement[] { new UILiteral() { literal = String.Format(localizer.Localize("statistics-group-classes"), statistics.Groups.Count().ToString("N0", culture)) } }
            });
            summary.Add(new UIDiv()
            {
                css = "webgis-statistics-hint",
                elements = new IUIElement[] { new UILiteral() { literal = localizer.Localize("statistics-restrict-hint") } }
            });
        }
        // after the literals: a literal rewrites the parent html (=> events of previous siblings get lost)
        summary.Add(new UIButton(UIButton.UIButtonType.servertoolcommand_ext, StatisticsCsvCommand)
        {
            id = this.GetType().ToToolId(),  // servertoolcommand_ext: the button id is the target tool id
            css = UICss.ToClass(new[] { UICss.DefaultButtonStyle, "webgis-statistics-download" }),
            text = localizer.Localize("statistics-download-csv")
        });
        elements.Add(new UIDiv()
        {
            css = "webgis-statistics-summary",
            elements = summary.ToArray()
        });

        if (!groupByValues)
        {
            var rows = StatisticsValueRows(statistics, fieldKind, culture, localizer);

            elements.Add(new UITable(rows
                .Select(r => new UITableRow(new IUIElement[]
                {
                    new UILiteral() { literal = r.label },
                    new UILiteral() { literal = r.value }
                }))
                .ToArray())
            {
                css = UICss.ToClass(new[] { "webgis-statistics-values", UICss.TableAlternateRowColor })
            });
        }
        else
        {
            var maxCount = Math.Max(1L, statistics.Groups.Select(g => g.Count).DefaultIfEmpty(0).Max());
            var groupList = StatisticsGroupRows(statistics.Groups, sortDescending, g => StatisticsGroupText(g, culture, localizer), culture);

            var menu = new UIMenu();
            menu.elements = groupList
                .Select(g => (IUIElement)new UIMenuItem(this, null, UIButton.UIButtonType.servertoolcommand_ext, StatisticsRestrictCommand)
                {
                    css = "webgis-statistics-group",
                    // relative bar length (css variable, see default.css)
                    style = $"--webgis-statistics-share:{(100D * g.Group.Count / maxCount).ToString("0.#", CultureInfo.InvariantCulture)}%",
                    text = String.Concat(
                        $"<span class='webgis-statistics-index'>{g.Index.ToString(culture)}</span>",
                        StatisticsRankHtml(g.Rank, g.RankSize, g.InheritedRank, culture, localizer),
                        $"<span class='webgis-statistics-value'>{WebUtility.HtmlEncode(g.Text)}</span>"),
                    subtext = String.Format(localizer.Localize("statistics-group-count"),
                                            g.Group.Count.ToString("N0", culture),
                                            StatisticsShare(g.Group.Count, statistics.TotalCount).ToString("P1", culture)),
                    value = System.Text.Json.JsonSerializer.Serialize(g.Group.Value)
                })
                .ToArray();

            if (statistics.OtherFeaturesCount > 0)  // the service returned not all groups (maxRecordCount)
            {
                elements.Add(InfoLine(String.Format(localizer.Localize("statistics-other-groups"),
                    statistics.Groups.Count().ToString("N0", culture),
                    statistics.OtherFeaturesCount.ToString("N0", culture))));
            }

            elements.Add(new UIDiv()
            {
                css = "webgis-statistics-groups",
                elements = new IUIElement[]
                {
                    new UIDiv()
                    {
                        css = "webgis-statistics-groups-header",
                        elements = new IUIElement[]
                        {
                            new UIDiv() { css = "webgis-statistics-index", elements = new IUIElement[] { new UILiteral() { literal = localizer.Localize("statistics-column-index") } } },
                            new UIDiv() { css = "webgis-statistics-rank", elements = new IUIElement[] { new UILiteral() { literal = localizer.Localize("statistics-column-rank") } } },
                            new UIDiv() { css = "webgis-statistics-value", elements = new IUIElement[] { new UILiteral() { literal = localizer.Localize("statistics-column-value") } } },
                            new UIDiv() { css = "webgis-statistics-count", elements = new IUIElement[] { new UILiteral() { literal = localizer.Localize("statistics-column-count") } } }
                        }
                    },
                    menu
                }
            });
        }

        return new UIDiv()
        {
            elements = elements.ToArray()
        };
    }

    async private Task<LayerStatistics> QueryStatisticsAsync(IBridge bridge, QueryRestrictionDescriptor descriptor, IQueryBridge query, IField field, LayerStatisticsFieldKind fieldKind, bool groupByValues, ILocalizer<QueryResultRestriction> localizer)
        => await query.QueryStatisticsAsync(bridge.RequestContext,
                                            CreateFilter(bridge, descriptor, query, queryGeometry: false),
                                            new LayerStatisticsRequest()
                                            {
                                                Field = field.Name,
                                                FieldKind = fieldKind,
                                                GroupByValues = groupByValues,
                                                MaxGroups = 0  // all groups the service returns (limited by its maxRecordCount)
                                            },
                                            appendFilterClause: descriptor.EffectiveWhereClause(descriptor.Restriction))
            ?? throw new InfoException(localizer.Localize("statistics-not-supported"));

    /// <summary>
    /// Competition ranking by frequency (1, 2, 2, 4): rank 1 = most frequent value
    /// </summary>
    static private int StatisticsRank(IEnumerable<long> counts, long count)
        => 1 + counts.Count(c => c > count);

    internal record StatisticsGroupRow(int Index, int Rank, int RankSize, bool InheritedRank, LayerStatisticsGroup Group, string Text);

    /// <summary>
    /// Groups sorted by count (then by text) with running index and rank. The rank is independent of the sort direction.
    /// InheritedRank: same rank as the previous row
    /// </summary>
    static internal IReadOnlyList<StatisticsGroupRow> StatisticsGroupRows(IEnumerable<LayerStatisticsGroup> groups, bool sortDescending, Func<LayerStatisticsGroup, string> text, CultureInfo culture)
    {
        var groupArray = groups.ToArray();
        var counts = groupArray.Select(g => g.Count).ToArray();
        var textComparer = StringComparer.Create(culture, true);

        var items = groupArray.Select(g => (Group: g, Text: text(g)));
        var sorted = (sortDescending
            ? items.OrderByDescending(g => g.Group.Count).ThenBy(g => g.Text, textComparer)
            : items.OrderBy(g => g.Group.Count).ThenBy(g => g.Text, textComparer)).ToArray();

        return sorted
            .Select((g, index) => new StatisticsGroupRow(
                Index: index + 1,
                Rank: StatisticsRank(counts, g.Group.Count),
                RankSize: counts.Count(c => c == g.Group.Count),
                InheritedRank: index > 0 && sorted[index - 1].Group.Count == g.Group.Count,
                Group: g.Group,
                Text: g.Text))
            .ToList();
    }

    static private string StatisticsGroupText(LayerStatisticsGroup group, CultureInfo culture, ILocalizer<QueryResultRestriction> localizer)
        => group.Value is null
            ? localizer.Localize("statistics-null-value")
            : group.Label.OrTake(StatisticsValueText(group.Value, culture));

    /// <summary>
    /// CSV: numbers without group separators, so spreadsheets can parse them
    /// </summary>
    static private string StatisticsGroupCsvValue(LayerStatisticsGroup group, CultureInfo culture, ILocalizer<QueryResultRestriction> localizer)
        => group.Value switch
        {
            null => localizer.Localize("statistics-null-value"),
            _ when !String.IsNullOrEmpty(group.Label) => group.Label,
            IFormattable formattable when group.Value is not string => formattable.ToString(null, culture),
            _ => group.Value.ToString()
        };

    static private double StatisticsShare(long count, long totalCount)
        => totalCount > 0 ? (double)count / totalCount : 0D;

    static private List<(string label, string value)> StatisticsValueRows(LayerStatistics statistics, LayerStatisticsFieldKind fieldKind, CultureInfo culture, ILocalizer<QueryResultRestriction> localizer)
    {
        var rows = new List<(string label, string value)>()
        {
            (localizer.Localize("statistics-count"), (statistics.TotalCount - statistics.NullCount).ToString("N0", culture)),
            (localizer.Localize("statistics-count-null"), statistics.NullCount.ToString("N0", culture)),
            (localizer.Localize("statistics-min"), StatisticsValueText(statistics.Min, culture)),
            (localizer.Localize("statistics-max"), StatisticsValueText(statistics.Max, culture))
        };

        if (fieldKind == LayerStatisticsFieldKind.Number)
        {
            rows.Add((localizer.Localize("statistics-avg"), StatisticsValueText(statistics.Average, culture)));
            rows.Add((localizer.Localize("statistics-sum"), StatisticsValueText(statistics.Sum, culture)));
        }

        return rows;
    }

    /// <summary>
    /// Semicolon separated (Excel default for de/at), quoted if required.
    /// Cells starting with =, +, -, @ are prefixed with ' (no formula injection), except numbers
    /// </summary>
    static internal string StatisticsCsv(IEnumerable<string[]> rows)
    {
        var csv = new System.Text.StringBuilder();

        foreach (var row in rows)
        {
            csv.Append(String.Join(";", row.Select(StatisticsCsvCell)));
            csv.Append("\r\n");
        }

        return csv.ToString();
    }

    static private string StatisticsCsvCell(string cell)
    {
        cell ??= "";

        if (cell.Length > 0 && "=+-@\t\r".Contains(cell[0])
            && !double.TryParse(cell, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
            && !double.TryParse(cell, NumberStyles.Any, CultureInfo.GetCultureInfo("de-AT"), out _))
        {
            cell = $"'{cell}";
        }

        if (cell.IndexOfAny(new[] { ';', '"', '\r', '\n' }) >= 0)
        {
            cell = $"\"{cell.Replace("\"", "\"\"")}\"";
        }

        return cell;
    }

    static private string StatisticsFileName(string fieldName)
    {
        var name = new string(fieldName.Select(c => Char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());
        return String.IsNullOrEmpty(name) ? "field" : name;
    }

    /// <summary>
    /// The rank is clickable (data-item-command, see webgis.ui.appendCallback), if more values share it.
    /// Repeated ranks (same rank as the previous row) are rendered faded (css class "inherited")
    /// </summary>
    static private string StatisticsRankHtml(int rank, int rankSize, bool inherited, CultureInfo culture, ILocalizer<QueryResultRestriction> localizer)
    {
        var css = "webgis-statistics-rank" + (inherited ? " inherited" : "");
        var rankText = $"{rank.ToString(culture)}.";

        if (rankSize < 2)
        {
            return $"<span class='{css}'>{rankText}</span>";
        }

        var title = WebUtility.HtmlEncode(String.Format(localizer.Localize("statistics-rank-tooltip"), rankSize.ToString("N0", culture), rankText));
        return $"<span class='{css} clickable' data-item-command='{StatisticsRankItemCommand}' title='{title}'>{rankText}</span>";
    }

    /// <summary>
    /// Condition for all values of a rank: field in (v1, v2, ...). The values are recalculated on the server (never sent by the client)
    /// </summary>
    static internal string StatisticsRankCondition(string fieldName, IEnumerable<LayerStatisticsGroup> groups, ILocalizer<QueryResultRestriction> localizer)
    {
        var values = groups.Select(g => g.Value).ToArray();

        if (values.Length == 1)
        {
            return StatisticsGroupCondition(fieldName, System.Text.Json.JsonSerializer.Serialize(values[0]));
        }

        // the flat QueryBuilder can't express "in (...) or is null" and splits "in" values by comma
        if (values.Length > MaxStatisticsRankValues
            || values.Any(v => v is null || (v is string s && s.Contains(','))))
        {
            throw new InfoException(String.Format(localizer.Localize("statistics-rank-not-possible"), MaxStatisticsRankValues));
        }

        var literals = values.Select(v => v switch
        {
            string s => $"'{s.Replace("'", "''")}'",
            _ => Convert.ToDouble(v, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture)
        });

        return $"{fieldName}{UIQueryBuilder.OperatorIn}({String.Join(",", literals)})";
    }

    static private IUIElement StatisticsFormRow(string label, IUIElement input)
        => new UIDiv()
        {
            css = "webgis-statistics-row",
            elements = new IUIElement[]
            {
                new UILabel() { label = label },
                input
            }
        };

    static private IField StatisticsField(IEnumerable<IField> allowedFields, string fieldName)
        => allowedFields.FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
           ?? throw new ArgumentException($"The field '{fieldName}' is not allowed");

    static private LayerStatisticsFieldKind StatisticsFieldKind(IField field)
        => field.Type switch
        {
            WebMapping.Core.FieldType.SmallInteger or
            WebMapping.Core.FieldType.Interger or
            WebMapping.Core.FieldType.BigInteger or
            WebMapping.Core.FieldType.Float or
            WebMapping.Core.FieldType.Double => LayerStatisticsFieldKind.Number,
            WebMapping.Core.FieldType.Date => LayerStatisticsFieldKind.Date,
            _ => LayerStatisticsFieldKind.Text
        };

    static private CultureInfo StatisticsCulture(ILocalizer localizer)
    {
        try
        {
            return CultureInfo.GetCultureInfo(localizer.Localize("statistics-culture"));
        }
        catch
        {
            return CultureInfo.InvariantCulture;
        }
    }

    static private string StatisticsValueText(object value, CultureInfo culture)
        => value switch
        {
            null => "-",
            double d => d.ToString("#,##0.###", culture),
            float f => f.ToString("#,##0.###", culture),
            decimal m => m.ToString("#,##0.###", culture),
            int or long or short => Convert.ToInt64(value).ToString("N0", culture),
            _ => value.ToString()
        };

    /// <summary>
    /// Condition for a clicked group: the (raw) value comes back as JSON from the client.
    /// The literal depends on the raw value type (a coded value domain field can have numeric raw values)
    /// </summary>
    static internal string StatisticsGroupCondition(string fieldName, string jsonValue)
    {
        using var document = JsonDocument.Parse(String.IsNullOrWhiteSpace(jsonValue) ? "null" : jsonValue);
        var element = document.RootElement;

        return element.ValueKind switch
        {
            JsonValueKind.Null => $"{fieldName}{UIQueryBuilder.OperatorIsNull}",
            JsonValueKind.Number when double.TryParse(element.GetRawText(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                => $"{fieldName}={number.ToString("R", CultureInfo.InvariantCulture)}",
            JsonValueKind.String => $"{fieldName}='{element.GetString().Replace("'", "''")}'",
            _ => throw new ArgumentException($"Invalid value for field {fieldName}")
        };
    }

    /// <summary>
    /// ANDs a condition to an existing (flat QueryBuilder) restriction. The QueryBuilder knows no parentheses,
    /// so the condition is appended to every OR term: a or b => a and c or b and c (AND binds stronger than OR)
    /// </summary>
    static internal string CombineRestriction(string restriction, string condition)
    {
        if (String.IsNullOrWhiteSpace(restriction))
        {
            return condition;
        }

        return String.Join(" or ", SplitTopLevelOr(restriction).Select(term => $"{term} and {condition}"));
    }

    static private IEnumerable<string> SplitTopLevelOr(string where)
    {
        var terms = new List<string>();
        int start = 0, depth = 0;
        bool inString = false;

        for (int i = 0; i < where.Length; i++)
        {
            char c = where[i];

            if (c == '\'')
            {
                inString = !inString;  // '' (escaped quote) toggles twice
            }
            else if (!inString && c == '(')
            {
                depth++;
            }
            else if (!inString && c == ')')
            {
                depth--;
            }
            else if (!inString && depth == 0
                     && Char.IsWhiteSpace(c)
                     && i + 3 < where.Length
                     && (where[i + 1] == 'o' || where[i + 1] == 'O')
                     && (where[i + 2] == 'r' || where[i + 2] == 'R')
                     && Char.IsWhiteSpace(where[i + 3]))
            {
                terms.Add(where.Substring(start, i - start).Trim());
                start = i + 3;
                i += 3;
            }
        }

        terms.Add(where.Substring(start).Trim());

        return terms.Where(t => t.Length > 0);
    }

    static private ApiQueryFilter CreateFilter(IBridge bridge, QueryRestrictionDescriptor descriptor, IQueryBridge query, bool queryGeometry = true)
    {
        var queryShape = descriptor.QueryShape();
        ApiQueryFilter filter = queryShape != null
            ? new ApiSpatialFilter()
            {
                QueryShape = queryShape,
                FilterSpatialReference = bridge.CreateSpatialReference(descriptor.Srs)
            }
            : new ApiQueryFilter();

        filter.QueryItems = descriptor.QueryItems();
        filter.QueryGeometry = queryGeometry;
        filter.FeatureSpatialReference = bridge.GetSupportedSpatialReference(query, bridge.DefaultSrefId);

        return filter;
    }

    /// <summary>
    /// Re-executes the original query (geometry, items, where, limit) with the restriction (null => unrestricted).
    /// noResultsMessage: an empty restricted result (no hits or an invalid condition) throws an InfoException,
    /// so the dialog stays open with the conditions and the current result is kept.
    /// </summary>
    async static private Task<ApiEventResponse> PerformAsync(IBridge bridge, ApiToolEventArguments e, QueryRestrictionDescriptor descriptor, IQueryBridge query, string restriction, string noResultsMessage = null)
    {
        descriptor.Restriction = String.IsNullOrWhiteSpace(restriction) ? null : restriction;

        var filter = CreateFilter(bridge, descriptor, query);

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
    static private IUIElement OriginInfo(QueryRestrictionDescriptor descriptor, IQueryBridge query, string token, ILocalizer<QueryResultRestriction> localizer, bool forStatistics = false)
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

        if (forStatistics)
        {
            if (!String.IsNullOrWhiteSpace(descriptor.Restriction))
            {
                elements.Add(InfoLine(String.Format(localizer.Localize("statistics-info-restriction"), descriptor.Restriction)));
            }
        }
        else
        {
            elements.Add(InfoLine(localizer.Localize("info-restrict")));
        }

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
