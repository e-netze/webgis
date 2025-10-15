using E.Standard.ActiveDirectory;
using E.Standard.Drawing.Models;
using E.Standard.Localization.Abstractions;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Web.Abstractions;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Api.IO;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Editing;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Api.Bridge;

public interface IBridge : IAppCryptography
{
    IBridge Clone(IApiButton currentTool);

    string GetRequestParameter(string parameter);

    Task<IServiceBridge> GetService(string serviceId);

    Task<IQueryBridge> GetQuery(string setviceId, string queryId);

    // Faster, no initialisation of service etc, no dynamic services
    Task<IQueryBridge> GetQueryTemplate(string setviceId, string queryId);

    Task<IEnumerable<string>> GetAssociatedServiceIds(string serviceId, string queryId);
    Task<IEnumerable<string>> GetAssociatedServiceIds(IQueryBridge queryBridge);

    Task<IQueryBridge> GetQueryFromThemeId(string themeId); // themeId=serviceId:queryId

    Task<IQueryBridge> GetFirstLayerQuery(string serviceId, string layerId);
    Task<IEnumerable<IQueryBridge>> GetLayerQueries(string serviceId, string layerId);
    Task<IEnumerable<IQueryBridge>> GetLayerQueries(IQueryBridge query);

    Task<string> GetFieldDefaultValue(string serviceId, string layerId, string fieldName);

    IEditThemeBridge GetEditTheme(string serviceId, string editThemeId);

    Task<IEnumerable<EditThemeFixVerticesSnapping>> GetEditThemeFixToSnapping(string serviceId, string editThemeId);

    Task<IEnumerable<IQueryFeatureTransferBridge>> GetQueryFeatureTransfers(string serviceId, string queryId);

    IEnumerable<IEditThemeBridge> GetEditThemes(string serviceId);

    System.Xml.XmlNode TryGetEditThemeMaskXmlNode(string serviceId, string editThemeId);

    IPrintLayoutBridge GetPrintLayout(string layoutId);

    IEnumerable<IPrintLayoutBridge> GetPrintLayouts(IEnumerable<string> layoutIds);

    IEnumerable<IPrintFormatBridge> GetPrintFormats(string layoutId = "");

    IEnumerable<(string serviceId, string layerId)> GetPrintLayoutShowLayers(string layoutId);
    IEnumerable<(string serviceId, string layerId)> GetPrintLayoutHideLayers(string layoutId);

    PrintLayerProperties GetPrintLayoutProperties(string layoutId, PageSize pageSize, PageOrientation pageOrientation, double scale);

    IEnumerable<int> GetPrintLayoutScales(string layoutId);

    IEnumerable<IPrintLayoutTextBridge> GetPrintLayoutTextElements(string layoutId, bool allowFileName = false);

    string GetQueryThemeId(IQueryBridge query);

    Task<FeatureCollection> QueryLayerAsync(string serviceId, string layerId, ApiQueryFilter filter, string appendFilterClause = "");
    Task<FeatureCollection> QueryLayerAsync(string serviceId, string layerId, string whereClause, QueryFields fields, SpatialReference featureSpatialReferernce = null, Shape queryShape = null);

    Task<int> QueryCountLayerAsync(string serviceId, string layerId, string whereClause);

    Task<IEnumerable<T>> QueryLayerDistinctAsync<T>(string serviceId, string layerId, string distinctField,
                                                    string whereClause = "", string orderBy = "", int limit = 0);

    Task<IEnumerable<string>> GetHasFeatureAttachments(string serviceId, string layerId, IEnumerable<string> objectIds);
    Task<IFeatureAttachments> GetFeatureAttachments(string serviceId, string layerId, string objectId);
    Task<byte[]> GetFeatureAttachmentData(string serviceId, string attachementUri);

    IEnumerable<IChainageThemeBridge> ServiceChainageThemes(string serviceId);
    IChainageThemeBridge ChainageTheme(string id);

    IEnumerable<IVisFilterBridge> ServiceVisFilters(string serviceId);
    IVisFilterBridge ServiceVisFilter(string serviceId, string filterId);
    Task<IEnumerable<IVisFilterBridge>> ServiceQueryVisFilters(string serviceId, string queryId);
    IUnloadedRequestVisFiltersContext UnloadRequestVisFiltersContext();

    IEnumerable<VisFilterDefinitionDTO> RequestVisFilterDefintions();

    IEnumerable<ILabelingBridge> ServiceLabeling(string serviceId);
    ILabelingBridge ServiceLabeling(string serviceId, string labelingId);

    SpatialReference CreateSpatialReference(int sRefId);

    E.Standard.WebMapping.Core.Geometry.IGeometricTransformer GeometryTransformer(int fromSrId, int toSrId);

    SpatialReference GetSupportedSpatialReference(IQueryBridge query, int defaultSrefId = 4326);

    int DefaultSrefId { get; }

    string AppRootPath { get; }
    string AppAssemblyPath { get; }

    string WWWRootPath { get; }
    string AppRootUrl { get; }
    string AppConfigPath { get; }
    string AppEtcPath { get; }

    string OutputPath { get; }
    string OutputUrl { get; }

    string ToolCommandUrl(string command, object parameters = null);

    IBridgeUser CurrentUser { get; }

    void SetAnonymousUserGuid(Guid guid);

    T ToolConfigValue<T>(string toolConfigKey);
    T[] ToolConfigValues<T>(string toolConfigKey);

    IApiButton TryGetFriendApiButton(IApiButton sender, string id);

    IStorage Storage { get; }

    ApiToolEventArguments CurrentEventArguments { get; }

    Task<IEnumerable<IGeoReferenceItem>> GeoReferenceAsync(string term, string serviceIds = "", string categories = "", string exclude = "geojuhu");
    Task<Dictionary<string, IEnumerable<IGeoReferenceItem>>> GeoReferenceAsync(IEnumerable<string> terms, string serviceIds = "", string categories = "", string exclude = "geojuhu");

    string ToGeoJson(FeatureCollection featureCollection);

    Task<IDictionary<T, T>> GetLookupValues<T>(IApiObjectBridge apiObject, string term = "", string parameter = "", Dictionary<string, string> replacements = null, string serviceId = "", string layerId = "");

    IMap TemporaryMapObject();

    IImpersonator Impersonator();

    Task<IFeatureWorkspace> TryGetFeatureWorkspace(string serviceId, string layerId);

    Task<IEnumerable<LayerLegendItem>> GetLayerLegendItems(string serviceId, string layerId);
    Task<IEnumerable<KeyValuePair<string, string>>> GetLayerFieldDomains(string serviceId, string layerId, string fieldName);
    Task<FieldCollection> GetServiceLayerFields(string serviceId, string layerId);

    WebProxy GetWebProxy(string server);

    bool IsInDeveloperMode { get; }

    IApiButton GetTool(string toolId);

    void Trace(string message);

    Task<bool> SetUserFavoritesItemAsync(IApiButton tool, string methodName, string item);
    Task<IEnumerable<string>> GetUserFavoriteItemsAsync(IApiButton tool, string methodName, int max = 12);

    [Obsolete]
    string GetCustomTextBlock(IApiButton tool, string name, string defaultTextBlock);

    string WebGisPortalUrl { get; }

    string ReplaceUserAndSessionDependentFilterKeys(string filter, string startingBracket = "[", string endingBracket = "]");

    #region WebGIS Cloud

    bool HasWebGISCloudConnection { get; }

    #endregion

    System.Text.Encoding DefaultTextEncoding { get; }

    IHttpService HttpService { get; }
    IRequestContext RequestContext { get; }
    ICryptoService CryptoService { get; }

    string CreateNewAnoymousCliendsideUserId();

    string CreateAnonymousClientSideUserId(Guid guid);

    Guid AnonymousUserGuid(string anonymousUserId);

    IUserAgent UserAgent { get; }

    string GetOriginalUrlParameterValue(string parameter, bool ignoreCase = true);

    string CreateCustomSelection(Shape shape, SpatialReference shapesSRef);

    ILocalizer<T> GetLocalizer<T>();
}

public interface IBridgeUser
{
    string Username { get; }
    string[] UserRoles { get; }
    string[] UserRoleParameters { get; }

    bool IsAnonymous { get; }

    Guid? AnonymousUserId { get; }
}

public class BridgeUser : IBridgeUser
{
    public BridgeUser(string username, string[] userRoles = null, string[] userRoleParameters = null)
    {
        this.Username = username;
        this.UserRoles = userRoles;
        this.UserRoleParameters = userRoleParameters;
    }
    public string Username { get; private set; }
    public string[] UserRoles { get; }
    public string[] UserRoleParameters { get; }

    internal const string AnonymousUserNamePrefix = @"_anonym\";

    public bool IsAnonymous
    {
        get
        {
            return String.IsNullOrWhiteSpace(this.Username) ||
                   this.Username.StartsWith(AnonymousUserNamePrefix);
        }
    }

    public void SetAnonymousUserId(Guid guid)
    {
        this.Username = AnonymousUserNamePrefix + guid.ToString("N").ToString();
    }

    public Guid? AnonymousUserId
    {
        get
        {
            if (!IsAnonymous || String.IsNullOrEmpty(this.Username) || !this.Username.StartsWith(AnonymousUserNamePrefix))
            {
                return null;
            }

            return new Guid(this.Username.Substring(AnonymousUserNamePrefix.Length));
        }
    }
}

public class PrintLayerProperties
{
    public DimensionF WorldSize { get; set; }

    public bool IsQueryDependent { get; set; }

    public string QueryDependencyQueryUrl { get; set; }

    public string QueryDependencyQueryField { get; set; }
}