using System;
using System.Linq;
using System.Threading.Tasks;

using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Presentation.Extensions;
using E.Standard.WebGIS.Tools.Presentation.Models;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Renderer.UserRenderers;

namespace E.Standard.WebGIS.Tools.Presentation;

/// <summary>
/// User-defined layer renderers ("Darstellung" in the TOC context menu).
/// Not a toolbar tool: it is only called via server commands from the TOC.
/// The renderer itself is kept and serialized by the client (service.setLayerRenderer)
/// and sent with every map/legend/print request (parameter "renderers").
/// VisFilterDependent: "load values"/"field range" only use the values of the currently filtered features.
/// </summary>
[Export(typeof(IApiButton))]
[AdvancedToolProperties(VisFilterDependent = true)]
public class UserRenderer : IApiServerButtonLocalizableAsync<UserRenderer>
{
    #region IApiButton

    public string Name => "Layer Presentation";

    public string Container => String.Empty;

    public string Image => String.Empty;

    public string ToolTip => "Change the presentation of a layer";

    public bool HasUI => false;

    public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<UserRenderer> localizer)
        => Task.FromResult<ApiEventResponse>(null);

    #endregion

    #region Commands

    [ServerToolCommand("set_toc_layer_renderer")]
    async public Task<ApiEventResponse> OnSetTocLayerRenderer(IBridge bridge, ApiToolEventArguments e, ILocalizer<UserRenderer> localizer)
    {
        var request = await ParseAndCheckRequest(bridge, e);

        var layer = ((await bridge.GetService(request.ServiceId))?.FindLayer(request.LayerId))
            .ThrowIfNull(() => $"Unknown layer {request.LayerId}");

        string geometryType = layer.GeometryType switch
        {
            LayerGeometryType.point => "point",
            LayerGeometryType.line => "line",
            LayerGeometryType.polygon => "polygon",
            _ => throw new Exception($"Renderers are not supported for geometry type {layer.GeometryType}")
        };

        var editor = new UIRendererEditor()
        {
            id = "set-toc-layer-renderer",
            ServiceId = request.ServiceId,
            LayerId = request.LayerId,
            GeometryType = geometryType,
            MaxClasses = UserRendererSettings.MaxUniqueValueClasses,
            CallbackToolId = this.GetType().ToToolId()
        };

        foreach (var field in await AllowedFields(bridge, request))
        {
            editor.TryAddField(field.Name, field.Type, field.Alias);
        }

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[]
            {
                new UIDiv()
                {
                    target = UIElementTarget.modaldialog.ToString(),
                    targettitle = $"{localizer.Localize("title")}: {layer.Name}",
                    targetwidth = "640px",
                    targetheight = "560px",
                    elements = new IUIElement[] { editor }
                }
            }
        };
    }

    [ServerToolCommand("distinct_values")]
    async public Task<ApiEventResponse> OnDistinctValues(IBridge bridge, ApiToolEventArguments e, ILocalizer<UserRenderer> localizer)
    {
        var request = await ParseAndCheckRequest(bridge, e);

        var field = (await AllowedFields(bridge, request))
            .FirstOrDefault(f => f.Name.Equals(request.Field, StringComparison.OrdinalIgnoreCase))
            .ThrowIfNull(() => $"Field {request.Field} is not allowed");

        int maxClasses = UserRendererSettings.MaxUniqueValueClasses;
        var values = (await bridge.QueryLayerDistinctAsync<object>(
                            request.ServiceId, request.LayerId, field.Name,
                            orderBy: field.Name, limit: maxClasses + 1,
                            applyRequestVisFilters: true))
                     .Where(v => v != null && v is not DBNull)
                     .Select(v => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture))
                     .Where(v => !String.IsNullOrEmpty(v))
                     .Distinct()
                     .ToArray();

        return new ApiRawJsonEventResponse(new
        {
            values = values.Take(maxClasses).ToArray(),
            truncated = values.Length > maxClasses
        });
    }

    [ServerToolCommand("field_range")]
    async public Task<ApiEventResponse> OnFieldRange(IBridge bridge, ApiToolEventArguments e, ILocalizer<UserRenderer> localizer)
    {
        var request = await ParseAndCheckRequest(bridge, e);

        var field = (await AllowedFields(bridge, request))
            .FirstOrDefault(f => f.Name.Equals(request.Field, StringComparison.OrdinalIgnoreCase)
                              && UserRendererValidator.IsNumericFieldType(f.Type))
            .ThrowIfNull(() => $"Field {request.Field} is not allowed");

        async Task<double?> QueryLimit(string direction)
        {
            var values = await bridge.QueryLayerDistinctAsync<object>(
                                request.ServiceId, request.LayerId, field.Name,
                                whereClause: $"{field.Name} IS NOT NULL",
                                orderBy: $"{field.Name} {direction}", limit: 1,
                                applyRequestVisFilters: true);

            return values
                .Select(v => ParseNumber(v))
                .FirstOrDefault(v => v.HasValue);
        }

        var min = await QueryLimit("ASC");
        var max = await QueryLimit("DESC");

        return new ApiRawJsonEventResponse(new
        {
            min,
            max
        });
    }

    #endregion

    #region Helper

    private static double? ParseNumber(object value)
    {
        switch (value)
        {
            case null:
            case DBNull:
                return null;
            case IConvertible convertible when value is not string:
                return convertible.ToDouble(System.Globalization.CultureInfo.InvariantCulture);
        }

        var text = value.ToString();
        if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double result) ||
            double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out result))
        {
            return result;
        }

        return null;
    }

    async private Task<TocUserRendererRequestDTO> ParseAndCheckRequest(IBridge bridge, ApiToolEventArguments e)
    {
        var request = JSerializer.Deserialize<TocUserRendererRequestDTO>(e.ServerCommandArgument)
            .ThrowIfNull(() => "Invalid argument");

        (await bridge.IsLayerUserRendererAllowed(request.ServiceId, request.LayerId))
            .ThrowIfFalse(() => "User renderers are not allowed for this layer");

        return request;
    }

    async private Task<IField[]> AllowedFields(IBridge bridge, TocUserRendererRequestDTO request)
    {
        var allowedQueryBuilderFields = bridge.GetAllowedQueryBuilderFields(request.ServiceId);
        if (allowedQueryBuilderFields?.Any() != true)
        {
            return [];
        }

        var layerFields = (await bridge.GetServiceLayerFields(request.ServiceId, request.LayerId))?
            .Where(f => f.Type != WebMapping.Core.FieldType.Shape);
        if (layerFields?.Any() != true)
        {
            return [];
        }

        return allowedQueryBuilderFields.SelectAllowedFields(layerFields).ToArray();
    }

    #endregion
}
