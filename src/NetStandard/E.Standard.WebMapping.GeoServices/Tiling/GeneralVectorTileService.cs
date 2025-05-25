using E.Standard.CMS.Core;
using E.Standard.Extensions.Compare;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.ServiceResponses;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.Tiling;

public class GeneralVectorTileService : IMapService, IMapServiceSupportedCrs
{
    private const string DefaultLayerName = "_tilecache";
    private LayerCollection _layers;

    public GeneralVectorTileService(CmsNode propNode, CmsNode layerNode, string fallbackService)
    {
        BasemapType = BasemapType.Normal;

        if (propNode != null)
        {
            this.Server = propNode.LoadString("styles_json");
            this.PreviewImageUrl = propNode.LoadString("preview_image_url");
        }

        _layers = new LayerCollection(this)
        {
            new OGC.WMS.OgcWmsLayer(layerNode?.Name.OrTake(DefaultLayerName), "0", this, queryable: false)
        };

        FallbackService = fallbackService;
    }

    public string FallbackService { get; private set; }

    public string PreviewImageUrl { get; private set; }

    public string Name { get; set; }
    public string Url { get; set; }

    public string Server { get; }

    public string Service { get; }

    public string ServiceShortname { get; }

    public string ID { get; }

    public float InitialOpacity { get; set; }
    public float OpacityFactor { get; set; } = 1f;

    public bool CanBuffer => false;

    public bool UseToc { get; set; }

    public LayerCollection Layers => _layers;

    public Envelope InitialExtent => new Envelope();

    public ServiceResponseType ResponseType => ServiceResponseType.Html;

    public ServiceDiagnostic Diagnostics { get; set; }

    public ServiceDiagnosticsWarningLevel DiagnosticsWaringLevel { get; set; }
    public bool IsDirty { get; set; }
    public int Timeout { get; set; }

    public IMap Map { get; private set; }

    public double MinScale { get; set; }
    public double MaxScale { get; set; }
    public bool ShowInToc { get; set; }
    public string CollectionId { get; set; }
    public bool CheckSpatialConstraints { get; set; }
    public bool IsBaseMap { get; set; }
    public BasemapType BasemapType { get; set; }
    public string BasemapPreviewImage { get; set; }

    public IMapService Clone(IMap parent)
    {
        return this;
    }

    public Task<ServiceResponse> GetMapAsync(IRequestContext requestContext)
    {
        return Task.FromResult<ServiceResponse>(null);
    }

    public Task<ServiceResponse> GetSelectionAsync(SelectionCollection collection, IRequestContext requestContext)
    {
        return Task.FromResult<ServiceResponse>(null);
    }

    public Task<bool> InitAsync(IMap map, IRequestContext requestContext)
    {
        return Task.FromResult(true);
    }

    public bool PreInit(string serviceID, string server, string url, string authUser, string authPwd, string token, string appConfigPath, ServiceTheme[] serviceThemes)
    {
        return true;
    }

    #region IServiceSupportedCrs Member

    public int[] SupportedCrs
    {
        get => new[] { 3857 };
        set { }
    }

    #endregion
}
