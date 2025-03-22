using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.UI.Elements;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
public class ServiceOrder : IApiClientButton, IApiButtonResources
{
    #region IApiButton Member

    public string Name => "Service Order";

    public string Container => "Map";

    public string Image => UIImageButton.ToolResourceImage(this, "service_order");

    public string ToolTip => "Set service order and transparency for services.";

    public bool HasUI => false;

    #endregion

    #region IApiClientButton Member

    public ApiClientButtonCommand ClientCommand => ApiClientButtonCommand.serviceorder;

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("service_order", Properties.Resources.layers);
    }

    #endregion
}
