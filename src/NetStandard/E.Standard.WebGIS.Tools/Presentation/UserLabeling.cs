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
using E.Standard.WebMapping.Core.Renderer.UserLabels;

namespace E.Standard.WebGIS.Tools.Presentation;

/// <summary>
/// User-defined layer labeling ("Beschriftung" in the TOC context menu).
/// Not a toolbar tool: it is only called via server commands from the TOC.
/// The labeling itself is kept and serialized by the client (service.setLayerLabeling)
/// and sent with every map/print request (parameter "userlabels").
/// </summary>
[Export(typeof(IApiButton))]
public class UserLabeling : IApiServerButtonLocalizableAsync<UserLabeling>
{
    #region IApiButton

    public string Name => "Layer Labeling";

    public string Container => String.Empty;

    public string Image => String.Empty;

    public string ToolTip => "Change the labeling of a layer";

    public bool HasUI => false;

    public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<UserLabeling> localizer)
        => Task.FromResult<ApiEventResponse>(null);

    #endregion

    #region Commands

    [ServerToolCommand("set_toc_layer_labeling")]
    async public Task<ApiEventResponse> OnSetTocLayerLabeling(IBridge bridge, ApiToolEventArguments e, ILocalizer<UserLabeling> localizer)
    {
        var request = JSerializer.Deserialize<TocUserRendererRequestDTO>(e.ServerCommandArgument)
            .ThrowIfNull(() => "Invalid argument");

        (await bridge.IsLayerUserLabelingAllowed(request.ServiceId, request.LayerId))
            .ThrowIfFalse(() => "User labeling is not allowed for this layer");

        var layer = ((await bridge.GetService(request.ServiceId))?.FindLayer(request.LayerId))
            .ThrowIfNull(() => $"Unknown layer {request.LayerId}");

        if (layer.GeometryType != LayerGeometryType.point &&
            layer.GeometryType != LayerGeometryType.line &&
            layer.GeometryType != LayerGeometryType.polygon)
        {
            throw new Exception($"Labeling is not supported for geometry type {layer.GeometryType}");
        }

        var editor = new UILabelEditor()
        {
            id = "set-toc-layer-labeling",
            ServiceId = request.ServiceId,
            LayerId = request.LayerId,
            MaxExpressionLength = UserLabelingValidator.MaxExpressionLength,
            MaxExpressionLines = UserLabelingValidator.MaxExpressionLines
        };

        foreach (var field in await AllowedFields(bridge, request))
        {
            editor.TryAddField(field.Name, field.Alias);
        }

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[]
            {
                new UIDiv()
                {
                    target = UIElementTarget.modaldialog.ToString(),
                    targettitle = $"{localizer.Localize("title")}: {layer.Name}",
                    targetwidth = "560px",
                    targetheight = "520px",
                    elements = new IUIElement[] { editor }
                }
            }
        };
    }

    #endregion

    #region Helper

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
