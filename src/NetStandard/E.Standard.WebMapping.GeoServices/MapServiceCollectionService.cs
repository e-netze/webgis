using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.ServiceResponses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices;
public class MapServiceCollectionService : IMapService, IMapServiceCollection
{
    #region IMapService

    public string Name { get; set; }
    public string Url { get; set; }

    public string Server => "";

    public string Service => "";

    public string ServiceShortname => "";

    public string ID => "";

    public float InitialOpacity { get; set; }
    public float OpacityFactor { get; set; } = 1f;

    public bool CanBuffer => false;

    public bool UseToc { get; set; }

    public LayerCollection Layers => LayerCollection.Emtpy;

    public Envelope InitialExtent => new Envelope();

    public ServiceResponseType ResponseType => ServiceResponseType.Collection;

    public ServiceDiagnostic Diagnostics => ServiceDiagnostic.Empty;

    public ServiceDiagnosticsWarningLevel DiagnosticsWaringLevel { get; set; }
    public bool IsDirty { get; set; }
    public int Timeout { get; set; }

    public IMap Map { get; set; }

    public double MinScale { get; set; }
    public double MaxScale { get; set; }
    public bool ShowInToc { get; set; }
    public string CollectionId { get; set; }
    public bool CheckSpatialConstraints { get; set; }
    public bool IsBaseMap { get; set; }
    public BasemapType BasemapType { get; set; }
    public string BasemapPreviewImage { get; set; }

    public IMapService Clone(IMap parent)
        => new MapServiceCollectionService()
        {
            Items = this.Items?.Select(i => i.Clone(this)) ?? [],
            Map = parent
        };


    public Task<ServiceResponse> GetMapAsync(IRequestContext requestContext)
    {
        throw new NotImplementedException();
    }

    public Task<ServiceResponse> GetSelectionAsync(SelectionCollection collection, IRequestContext requestContext)
    {
        throw new NotImplementedException();
    }

    public Task<bool> InitAsync(IMap map, IRequestContext requestContext) => Task.FromResult(true);


    public bool PreInit(string serviceID, string server, string url, string authUser, string authPwd, string staticToken, string appConfigPath, ServiceTheme[] serviceThemes)
        => true;

    #endregion

    #region IMapServiceCollection

    public IEnumerable<IMapServerCollectionItem> Items
    {
        get; set;
    }

    #endregion

    #region Classes

    public class Item : IMapServerCollectionItem
    {
        public Item(string url, MapServiceLayerVisibility layerVisibility)
        {
            this.Url = url;
            this.LayerVisibility = layerVisibility;
        }

        public string Url { get; }

        public MapServiceLayerVisibility LayerVisibility { get; }

        public IMapServerCollectionItem Clone(IMapServiceCollection parent)
            => new Item(Url, LayerVisibility);

    }

    #endregion
}
