using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.UI.Elements;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
public class BoxZoomIn : IApiClientButton, IApiButtonResources
{
    #region IApiClientButton Member

    public ApiClientButtonCommand ClientCommand => ApiClientButtonCommand.boxzoomin;

    #endregion

    #region IApiButton Member

    public string Name => "Zoom In";


    public string Container => "Navigation";

    public string Image => UIImageButton.ToolResourceImage(this, "zoomin");


    public string ToolTip => "Zoom in with a box";

    public bool HasUI => false;

    #endregion

    #region IApiButtonResources

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("zoomin", E.Standard.WebGIS.Tools.Properties.Resources.zoomin);
    }

    #endregion
}
