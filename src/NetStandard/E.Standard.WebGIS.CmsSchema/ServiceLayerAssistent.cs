using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.WebGIS.CmsSchema.UI;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

internal class ServiceLayerAssistent : SchemaNode, IAutoCreatable, IUI
{
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    private AddServiceLayersControl _ctrl = null;


    public ServiceLayerAssistent(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;
    }

    #region IAutoCreatable

    public bool AutoCreate()
    {
        if (_ctrl == null)
        {
            return false;
        }

        string absolutePath = $"{this.CmsManager.ConnectionString}/{Helper.TrimPathRight(this.RelativePath, 2)}";

        if (this.RelativePath.StartsWith("services/arcgisserver/mapserver"))
        {
            ArcServerService service = this.CmsManager.SchemaNodeInstance(_servicePack, Helper.TrimPathRight(this.RelativePath, 2), true) as ArcServerService;

            service.ImportLayers = _ctrl.SelectedLayerIds;
            service.RefreshAsync($"{absolutePath}/{service.CreateAs(false)}.xml", 1).Wait();
        }
        else if (this.RelativePath.StartsWith("services/ogc/wms"))
        {
            WMSService service = this.CmsManager.SchemaNodeInstance(_servicePack, Helper.TrimPathRight(this.RelativePath, 2), true) as WMSService;

            service.ImportLayers = _ctrl.SelectedLayerIds;
            service.RefreshAsync($"{absolutePath}/{service.CreateAs(false)}.xml", 1).Wait();
        }

        return true;
    }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        return "Assistent";
    }

    public Task<bool> CreatedAsync(string FullName)
    {
        return Task<bool>.FromResult(true);
    }

    #endregion

    #region IUI

    public IUIControl GetUIControl(bool create)
    {
        _ctrl = new AddServiceLayersControl(_servicePack, this);

        return _ctrl;
    }

    #endregion

    #region IPersistable Member

    public void Load(IStreamDocument stream)
    {
    }


    public void Save(IStreamDocument stream)
    {
    }

    #endregion
}
