using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.ServiceResponses;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IMapService : IClone<IMapService, IMap>
{
    string Name { get; set; }
    string Url { get; set; }
    string Server { get; }
    string Service { get; }

    string ServiceShortname { get; }

    string ID { get; }

    float Opacity { get; set; }
    bool CanBuffer { get; }
    bool UseToc { get; set; }

    LayerCollection Layers { get; }
    Envelope InitialExtent { get; }
    ServiceResponseType ResponseType { get; }

    ServiceDiagnostic Diagnostics { get; }
    ServiceDiagnosticsWarningLevel DiagnosticsWaringLevel { get; set; }

    bool PreInit(string serviceID, string server, string url/*, string outputPath, string outputUrl*/, string authUser, string authPwd, string staticToken, string appConfigPath, ServiceTheme[] serviceThemes);
    //bool Init(IMap map);
    Task<bool> InitAsync(IMap map, IRequestContext requestContext);

    bool IsDirty { get; set; }
    //ServiceResponse GetMap();
    Task<ServiceResponse> GetMapAsync(IRequestContext requestContext);
    //ServiceResponse GetSelection(SelectionCollection collection);
    Task<ServiceResponse> GetSelectionAsync(SelectionCollection collection, IRequestContext requestContext);

    int Timeout { get; set; }

    IMap Map
    {
        get;
    }

    double MinScale { get; set; }
    double MaxScale { get; set; }

    bool ShowInToc { get; set; }

    string CollectionId { get; set; }
    bool CheckSpatialConstraints { get; set; }

    bool IsBaseMap { get; set; }
    BasemapType BasemapType { get; set; }
    string BasemapPreviewImage { get; set; }
}
