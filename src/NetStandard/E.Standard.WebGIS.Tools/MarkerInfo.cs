using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.UI.Elements;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
public class MarkerInfo : IApiClientButton, IApiButtonResources, IApiButtonDependency
{
    #region IApiClientButton Member

    public ApiClientButtonCommand ClientCommand => ApiClientButtonCommand.showmarkerinfo;

    #endregion

    #region IApiButton Member

    public string Name => "Marker Info";

    public string Container => "Karte";

    public string Image => UIImageButton.ToolResourceImage(this, "markerinfo");

    public string ToolTip => "Information zu Graphic-Markern in der Karte";

    public bool HasUI => false;

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("markerinfo", Properties.Resources.marker_info);
    }

    #endregion

    #region IApiButtonDependency Member

    public VisibilityDependency ButtonDependencies => VisibilityDependency.HasMarkerInfo;

    #endregion
}
