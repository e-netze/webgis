using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Mobile;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true)]
[ToolId("webgis.tools.editing.deletefeature")]
public class DeleteFeature : UpdateFeature
{
    public DeleteFeature()
        : base()
    {
        this.Operation = EditOperation.Delete;
    }

    async internal static Task InitAsync(
                    DeleteFeature tool, 
                    IBridge bridge, 
                    ApiToolEventArguments e, 
                    EditFeatureDefinition editFeatureDef,
                    ApiEventResponse response,
                    ILocalizer<Edit> localizer,
                    int mapCrsId = 4326)
    {
        await UpdateFeature.InitAsync(tool, bridge, e, editFeatureDef, response, localizer, mapCrsId, EditOperation.Delete);

        tool.Name = string.Format(localizer.Localize("delete-in-layer"), editFeatureDef.EditThemeName);
    }

    #region Overrides

    protected override void AddUIElements(List<IUIElement> uiElements, ILocalizer<Edit> localizer)
    {
        uiElements.AddRange(new IUIElement[]
        {
            new UIButton(UIButton.UIButtonType.servertoolcommand, "delete") {
                    text = localizer.Localize("delete")
                }
        });
    }

    public override ApiToolConfirmation[] ToolConfirmations => null;

    #endregion
}
