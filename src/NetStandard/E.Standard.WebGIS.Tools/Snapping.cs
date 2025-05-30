﻿using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.UI.Elements;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
public class Snapping : IApiClientButton, IApiButtonResources
{
    public string Container => "Tools";

    public string Image => UIImageButton.ToolResourceImage(this, "snapping");

    public string Name => "Snapping";

    public string ToolTip => "Snapping settings";

    public ApiClientButtonCommand ClientCommand => ApiClientButtonCommand.snapping;

    public bool HasUI => false;

    #region IApiToolResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("snapping", Properties.Resources.snapping);
    }

    #endregion
}
