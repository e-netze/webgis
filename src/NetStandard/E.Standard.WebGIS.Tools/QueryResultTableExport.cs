﻿using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Elements;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true, ClientDeviceDependent = true)]
public class QueryResultTableExport : IApiServerButtonLocalizable<QueryResultTableExport>  // is this tool still in use?
{
    #region IApiButton Member

    public string Name => "Export Query Results";

    public string Container => "";

    public string Image => "";

    public string ToolTip => "Export query results as a CSV file";

    public bool HasUI => false;

    #endregion

    #region IApiServerButton

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<QueryResultTableExport> localizer)
    {
        return new ApiEventResponse()
            .AddUIElements(
                new UIDiv()
                    .AsDialog()
                    .WithDialogTitle("Exportien (Csv, ...)")
                    .WithStyles(UICss.NarrowFormMarginAuto)
                    .AddChildren(
                        new UISelect()
                            .WithId("mapmarkup-download-format")
                            .AsToolParameter()
                            .AddOption(new UISelect.Option()
                                .WithValue("csv")
                                .WithLabel("CSV Datei")),

                        new UILabel()
                            .WithLabel(localizer.Localize("label1")),
                        new UIButtonContainer(
                            new UIButton(UIButton.UIButtonType.servertoolcommand, "export")
                                .WithStyles(UICss.DefaultButtonStyle)
                                .WithText(localizer.Localize("export")))));
    }

    #endregion

    #region Commands

    [ServerToolCommand("export")]
    public ApiEventResponse OnExport(IBridge bridge, ApiToolEventArguments e)
    {
        //var selectedFeatures = await e.FeaturesFromSelectionAsync(bridge);

        return null;
    }

    #endregion
}
