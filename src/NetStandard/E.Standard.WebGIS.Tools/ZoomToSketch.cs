﻿using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
public class ZoomToSketch : IApiClientButton
{
    #region IApiClientButton Member

    public ApiClientButtonCommand ClientCommand => ApiClientButtonCommand.zoom2sketch;

    #endregion

    #region IApiButton Member

    public string Name => "Current sketch";

    public string Container => "Navigation";

    public string Image => "zoom2sketch.png";

    public string ToolTip => "Zoom to current sketch";

    public bool HasUI => false;

    #endregion
}
