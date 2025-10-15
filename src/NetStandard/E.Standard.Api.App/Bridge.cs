using E.Standard.ActiveDirectory;
using E.Standard.Api.App.Configuration;
using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.Configuration.Services;
using E.Standard.Drawing.Models;
using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.Localization.Abstractions;
using E.Standard.Localization.Extensions;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.ThreadSafe;
using E.Standard.Web.Abstractions;
using E.Standard.WebGIS.Api.Abstractions;
using E.Standard.WebGIS.Core.Mvc.Wrapper;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.IO;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Editing;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Models;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace E.Standard.Api.App;

public class Bridge : IBridge
{
    private readonly CmsDocument.UserIdentification _userIdentification = null;

    private readonly CacheService _cacheService;
    private readonly HttpRequestContextService _httpRequestContext;
    private readonly ConfigurationService _config;
    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly MapServiceInitializerService _mapServiceInitializer;
    private readonly IUrlHelperService _urlHelper;
    private readonly IHttpService _http;
    private readonly IRequestContext _requestContext;
    private readonly ICryptoService _crypto;
    private readonly LookupService _lookup;
    private readonly IStringLocalizer _localizer;

    public Bridge(IHttpRequestWrapper request,
                  HttpRequestContextService httpRequestContext,
                  CacheService cacheService,
                  ConfigurationService config,
                  SubscriberDatabaseService subscriberDb,
                  MapServiceInitializerService mapServiceInitializer,
                  IUrlHelperService urlHelper,
                  IRequestContext requestContext,
                  ICryptoService crypto,
                  LookupService lookup,
                  IStringLocalizer localizer,
                  CmsDocument.UserIdentification userIdentification,
                  IApiButton currentTool = null,
                  string storagePath = "")
    {
        _cacheService = cacheService;
        _httpRequestContext = httpRequestContext;
        _subscriberDb = subscriberDb;
        _config = config;
        _mapServiceInitializer = mapServiceInitializer;
        _urlHelper = urlHelper;
        _requestContext = requestContext;
        _http = _requestContext.Http;
        _crypto = crypto;
        _lookup = lookup;
        _localizer = localizer;

        _userIdentification = userIdentification;
        this.Request = request;
        this.CurrentTool = currentTool;

        storagePath = storagePath.OrTake(_config.StorageRootPath().ToLower());
        if (storagePath.StartsWith("http://") || storagePath.StartsWith("https://"))
        {
            this.Storage = new IO.WebStorage(this, storagePath);
        }
        else
        {
            this.Storage = new IO.FileSystemStorage(this, storagePath);
        }

        if (request != null)
        {
            string filters = request.QueryString["filters"] ?? request.Form["filters"];
            if (!String.IsNullOrWhiteSpace(filters))
            {
                this.RequestFilters = JSerializer.Deserialize<VisFilterDefinitionDTO[]>(filters);
            }
            string timeEpoch = request.QueryString["timeEpoch"] ?? request.Form["timeEpoch"];
            if (!String.IsNullOrEmpty(timeEpoch))
            {
                this.RequestTimeEpoch = JSerializer.Deserialize<TimeEpochDefinitionDTO[]>(timeEpoch);
            }
        }

        this.DefaultSrefId = _config.DefaultQuerySrefId();

        #region Browser Detection



        #endregion
    }

    #region Properties

    internal IApiButton CurrentTool { get; private set; }

    internal VisFilterDefinitionDTO[] RequestFilters { get; set; }

    internal TimeEpochDefinitionDTO[] RequestTimeEpoch { get; set; }

    private string GdiCustomGdiScheme => _urlHelper.GetCustomGdiScheme();

    private IHttpRequestWrapper Request { get; set; }

    #endregion

    #region Methods

    internal string GetFilterDefinitionQuery(QueryDTO query)
    {
        if (query == null || query.Service == null)
        {
            return String.Empty;
        }

        string sql = String.Empty;

        try
        {
            var cmsRequestFilters = this.RequestFilters?.Where(f => f.IsTocVisFilter() == false).ToArray();
            var tocRequestFilters = this.RequestFilters?.Where(f => f.IsTocVisFilter(query.Service.Url)).ToArray();
            var serviceVisFilters = _cacheService.GetAllVisFilters(query.Service.Url, _userIdentification);

            #region TOC Filters

            if (tocRequestFilters != null && tocRequestFilters.Length > 0)
            {
                foreach (var tocVisFilter in tocRequestFilters.Where(f => f.TocVisFilterLayerId() == query.LayerId))
                {
                    tocVisFilter.CheckSignature(_crypto);

                    sql = sql.AppendWhereClause(tocVisFilter.TocVisFilterWhereClause());
                }
            }

            #endregion

            #region CMS (defined) Vis Filters 

            if (cmsRequestFilters != null && cmsRequestFilters.Length > 0)
            {
                if (serviceVisFilters.filters != null)
                {
                    #region Normale Filter

                    foreach (var visFilter in serviceVisFilters.filters.Where(f => f.FilterType != E.Standard.WebGIS.CMS.VisFilterType.locked))
                    {
                        if (visFilter.LayerNamesString.Split(';').Where(layerName =>
                        {
                            var layer = query.Service.Layers.FindByName(layerName);
                            if (layer == null)
                            {
                                return false;
                            }

                            return layer.ID == query.LayerId;
                        }).Count() == 0)
                        {
                            continue;
                        }

                        var requestFilter = cmsRequestFilters
                            .Where(f =>
                            {
                                if (f?.Id == null)
                                {
                                    return false;
                                }

                                string filterId = f.Id.Contains("~") ? $"{query.Service.Url}~{visFilter.Id}" : visFilter.Id;

                                return f.Id == filterId;
                            })
                            .FirstOrDefault();

                        if (requestFilter == null)
                        {
                            continue;
                        }

                        string filterClause = visFilter.Filter;
                        foreach (var arg in requestFilter.Arguments.OrEmpty())
                        {
                            filterClause = filterClause.Replace($"[{arg.Name}]", arg.Value);
                        }

                        if (!String.IsNullOrWhiteSpace(filterClause))
                        {
                            //if (sql.Length > 0)
                            //    sql.Append(" AND ");
                            //sql.Append(filterClause);

                            sql = sql.AppendWhereClause(filterClause);
                        }
                    }

                    #endregion
                }
            }

            #endregion

            #region Locked Vis Layers

            if (serviceVisFilters.HasLockedFilters)
            {
                var layer = query.Service.Layers.Where(l => l.ID == query.LayerId).FirstOrDefault();
                if (layer != null)
                {
                    string lockedVisFilterClause = String.Empty;

                    foreach (var lockedFilter in serviceVisFilters.LockedFilters.Where(f => f.LayerNamesString.Split(';').Contains(layer.Name)))
                    {
                        lockedVisFilterClause = lockedVisFilterClause.AppendWhereClause(E.Standard.WebGIS.CMS.CmsHlp.ReplaceFilterKeys(_httpRequestContext.OriginalUrlParameters, _userIdentification, lockedFilter.Filter));
                    }

                    if (!String.IsNullOrEmpty(lockedVisFilterClause))
                    {
                        sql = sql.AppendWhereClause(lockedVisFilterClause);
                    }
                }
            }

            #endregion
        }
        catch { }

        return sql.ToString();
    }

    internal TimeEpochDefinition GetTimeEpoch(QueryDTO query)
    {
        var epoch = RequestTimeEpoch?
                    .Where(t => t.ServiceId == query?.Service.Url)
                    .FirstOrDefault()?
                    .Epoch;

        if (epoch != null && epoch.Length == 2)
        {
            return new TimeEpochDefinition()
            {
                StartTime = epoch[0],
                EndTime = epoch[1]
            };
        }

        return null;
    }

    #endregion

    #region IBridge Member

    public IBridge Clone(IApiButton currentTool)
    {
        var clone = new Bridge(
                this.Request,
                _httpRequestContext,
                _cacheService,
                _config,
                _subscriberDb,
                _mapServiceInitializer,
                _urlHelper,
                _requestContext,
                _crypto,
                _lookup,
                _localizer,
                _userIdentification,
                currentTool
            );
        clone.CurrentEventArguments = this.CurrentEventArguments;

        return clone;
    }

    public string GetRequestParameter(string key)
    {
        return this.Request?.FormOrQuery(key);
    }

    async public Task<IServiceBridge> GetService(string serviceId)
    {
        var service = await TryGetService(serviceId, _mapServiceInitializer.Map(_requestContext, _userIdentification));
        if (service == null)
        {
            return null;
        }

        var info = new ServiceInfoDTO()
        {
            name = service.Name,
            id = service.Url,
            type = ServiceDTO.GetServiceType(service).ToString().ToLower(),
            opacity = service.InitialOpacity
        };

        List<ServiceInfoDTO.LayerInfoDTO> layers = new List<ServiceInfoDTO.LayerInfoDTO>();
        foreach (var layer in service.Layers)
        {
            layers.Add(new ServiceInfoDTO.LayerInfoDTO()
            {
                id = layer.ID,
                idfieldname = layer.IdFieldName,
                name = layer.Name,
                type = layer.Type.ToString().ToLower(),
                minscale = layer.MinScale,
                maxscale = layer.MaxScale
            });
        }
        info.layers = layers.ToArray();

        return info.ToServiceBridge();
    }

    async public Task<IQueryBridge> GetQuery(string serviceId, string queryId)
    {
        if (_mapServiceInitializer.IsCustomService(serviceId))
        {
            var service = await _mapServiceInitializer.GetCustomServiceByUrlAsync(serviceId, _mapServiceInitializer.Map(_requestContext, _userIdentification), _userIdentification, this.Request?.Form);
            if (service is IDynamicService && ((IDynamicService)service).CreateQueriesDynamic != ServiceDynamicQueries.Manually)
            {
                return ((IDynamicService)service).GetDynamicQuery(queryId);
            }
        }
        else
        {
            var query = await _cacheService.GetQuery(serviceId, queryId, _userIdentification, urlHelper: _urlHelper);
            if (query != null)
            {
                query.Bridge = this;  // You always get an fresh copy for query -> IAuthClone<Query>
            }

            return query;
        }

        return null;
    }

    async public Task<IQueryBridge> GetQueryTemplate(string serviceId, string queryId)
    {
        return await _cacheService.GetQueryTemplate(serviceId, queryId, _userIdentification, urlHelper: _urlHelper);
    }

    async public Task<IQueryBridge> GetQueryFromThemeId(string themeId)   // themeId=serviceId:queryId
    {
        string[] ids = themeId.Split(':');

        if (ids.Length != 2)
        {
            return null;
        }

        return await GetQuery(ids[0], ids[1]);
    }

    async public Task<IQueryBridge> GetFirstLayerQuery(string serviceId, string layerId)
    {
        var query = _cacheService.GetQueries(serviceId, _userIdentification)?
                .queries?
                .Where(q => q.LayerId == layerId)
                .FirstOrDefault();

        if (query == null)
        {
            return null;
        }

        return await GetQuery(serviceId, query.id);
    }

    async public Task<IEnumerable<string>> GetAssociatedServiceIds(string serviceId, string queryId)
    {
        var queryBridge = await GetQuery(serviceId, queryId);

        if (queryBridge is QueryDTO query)
        {
            return query.associatedlayers?
                        .Select(a => a.serviceid)
                        .Where(s => !String.IsNullOrEmpty(s))
                        .Distinct()
                        .ToArray();
        }

        return Array.Empty<string>();
    }

    public Task<IEnumerable<string>> GetAssociatedServiceIds(IQueryBridge queryBridge)
    {
        if (queryBridge is QueryDTO query)
        {
            return GetAssociatedServiceIds(query.Service.Url, query.id);
        }

        return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
    }

    async public Task<IEnumerable<IQueryBridge>> GetLayerQueries(string serviceId, string layerId)
    {
        var queries = _cacheService.GetQueries(serviceId, _userIdentification)?
                .queries?
                .Where(q => q.LayerId == layerId);

        if (queries == null || queries.Count() == 0)
        {
            return null;
        }

        List<IQueryBridge> result = new List<IQueryBridge>();

        foreach (var query in queries)
        {
            result.Add(await GetQuery(serviceId, query.id));
        }

        return result;
    }

    async public Task<IEnumerable<IQueryBridge>> GetLayerQueries(IQueryBridge query)
    {
        if (query == null)
        {
            return null;
        }

        var queries = _cacheService.GetQueries(query.GetServiceId(), _userIdentification)?
                .queries?
                .Where(q => q.LayerId == query.GetLayerId());

        if (queries == null || queries.Count() == 0)
        {
            return null;
        }

        List<IQueryBridge> result = new List<IQueryBridge>();

        foreach (var q in queries)
        {
            result.Add(await GetQuery(query.GetServiceId(), q.id));
        }

        return result;
    }

    async public Task<string> GetFieldDefaultValue(string serviceId, string layerId, string fieldName)
    {
        var service = await TryGetService(serviceId, _mapServiceInitializer.Map(_requestContext, _userIdentification));

        if (service == null)
        {
            throw new Exception($"Unknown service {serviceId}");
        }

        var layer = service.Layers.Where(l => l.ID == layerId).FirstOrDefault();
        if (layer == null)
        {
            throw new Exception($"Unknown layer {layerId} in service {serviceId}");
        }

        var field = layer.Fields.Where(f => f.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase))
                                .FirstOrDefault();

        if (field == null)
        {
            throw new Exception($"Unknown field {fieldName} in layer {layerId}  in service {serviceId}");
        }

        switch (field.Type)
        {
            case FieldType.BigInteger:
            case FieldType.Interger:
            case FieldType.Float:
            case FieldType.Double:
                return "0";
            case FieldType.Boolean:
                return "false";
            default:
                return String.Empty;
        }
    }

    #region Chainage

    public IEnumerable<IChainageThemeBridge> ServiceChainageThemes(string serviceId)
    {
        return _cacheService.GetServiceChainageThemes(serviceId, _userIdentification);
    }
    public IChainageThemeBridge ChainageTheme(string id)
    {
        return _cacheService.GetChainageTheme(id, _userIdentification);
    }

    #endregion

    #region VisFilter

    public IEnumerable<IVisFilterBridge> ServiceVisFilters(string serviceId)
    {
        var filters = _cacheService.GetAllVisFilters(serviceId, _userIdentification);
        if (filters == null || filters.filters == null)
        {
            return new IVisFilterBridge[0];
        }

        return filters.filters.Where(f => f.FilterType != E.Standard.WebGIS.CMS.VisFilterType.locked);
    }
    public IVisFilterBridge ServiceVisFilter(string serviceId, string filterId)
    {
        return ServiceVisFilters(serviceId).Where(f => f.Id == filterId).FirstOrDefault();
    }

    async public Task<IEnumerable<IVisFilterBridge>> ServiceQueryVisFilters(string serviceId, string queryId)
    {
        var query = await _cacheService.GetQuery(serviceId, queryId, _userIdentification, urlHelper: _urlHelper);

        if (query?.Service?.Layers != null)
        {
            var layer = query.Service.Layers.FindByLayerId(query.LayerId);
            if (layer != null)
            {
                List<IVisFilterBridge> visFilters = new List<IVisFilterBridge>();

                foreach (var visFilter in _cacheService.GetAllVisFilters(serviceId, _userIdentification).filters.Where(f => f.FilterType != E.Standard.WebGIS.CMS.VisFilterType.locked && !String.IsNullOrEmpty(f.LayerNamesString)))
                {
                    if (visFilter.LayerNamesString.Split(';').Contains(layer.Name))
                    {
                        bool hasAllParameters = true;
                        foreach (var parameter in visFilter.Parameters.Keys)
                        {
                            if (layer.Fields.Where(f => f.Name.Equals(parameter)).Count() == 0)
                            {
                                hasAllParameters = false;
                                break;
                            }
                        }

                        if (hasAllParameters)
                        {
                            visFilters.Add(visFilter);
                        }
                    }
                }

                return visFilters;
            }
        }

        return new IVisFilterBridge[0];
    }

    public IUnloadedRequestVisFiltersContext UnloadRequestVisFiltersContext()
    {
        return new UnloadedRequestVisFiltersContext(this);
    }

    public string ReplaceUserAndSessionDependentFilterKeys(string filter, string startingBracket = "[", string endingBracket = "]")
    {
        return E.Standard.WebGIS.CMS.CmsHlp.ReplaceFilterKeys(_httpRequestContext.OriginalUrlParameters, _userIdentification, filter, startingBracket: startingBracket, endingBracket: endingBracket);
    }

    public IEnumerable<VisFilterDefinitionDTO> RequestVisFilterDefintions() => RequestFilters;

    #endregion

    #region Labeling

    public IEnumerable<ILabelingBridge> ServiceLabeling(string serviceId)
    {
        var labelings = _cacheService.GetLabeling(serviceId, _userIdentification);
        if (labelings == null || labelings.labelings == null)
        {
            return new ILabelingBridge[0];
        }

        return labelings.labelings;
    }

    public ILabelingBridge ServiceLabeling(string serviceId, string labelingId)
    {
        return ServiceLabeling(serviceId).Where(l => l.Id == labelingId).FirstOrDefault();
    }

    #endregion

    public IEditThemeBridge GetEditTheme(string serviceId, string editThemeId)
    {
        return _cacheService.GetEditTheme(serviceId, editThemeId, _userIdentification);
    }

    async public Task<IEnumerable<EditThemeFixVerticesSnapping>> GetEditThemeFixToSnapping(string serviceId, string editThemeId)
    {
        List<EditThemeFixVerticesSnapping> result = new List<EditThemeFixVerticesSnapping>();

        var editTheme = _cacheService.GetEditTheme(serviceId, editThemeId, _userIdentification);
        if (editTheme?.Snapping != null)
        {
            foreach (var editThemeSnapping in editTheme.Snapping)
            {
                if (editThemeSnapping.FixTo != null && editThemeSnapping.FixTo.Length > 0)
                {
                    var snapping = _cacheService.GetSnapSchemes(editThemeSnapping.ServiceId, _userIdentification)
                                                .Where(s => s.Id == editThemeSnapping.Id)
                                                .FirstOrDefault();

                    var service = await TryGetService(editThemeSnapping.ServiceId);

                    if (snapping?.LayerIds != null && service != null)
                    {
                        foreach (var fixTo in editThemeSnapping.FixTo)
                        {
                            var layers = snapping.LayerIds.Select(l => service.Layers.FindByLayerId(l))
                                                          .Where(layer => layer != null);

                            string[] layerIds = null;
                            if (fixTo.Name == "*")
                            {
                                layerIds = layers.Select(layer => layer.ID)
                                                 .Distinct()
                                                 .ToArray();
                            }
                            else
                            {
                                layerIds = layers.Where(layer =>
                                {
                                    var layerName = layer.Name.Contains(@"\") ? layer.Name.Substring(layer.Name.LastIndexOf(@"\") + 1) : layer.Name;
                                    return layerName == fixTo.Name;
                                })
                                                 .Select(layer => layer.ID)
                                                 .ToArray();
                            }

                            if (layerIds != null && layerIds.Length > 0)
                            {
                                result.Add(new EditThemeFixVerticesSnapping(service, layerIds, fixTo.Types));
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    async public Task<IEnumerable<IQueryFeatureTransferBridge>> GetQueryFeatureTransfers(string serviceId, string queryId)
    {
        var query = await _cacheService.GetQuery(serviceId, queryId, _userIdentification, _urlHelper);

        if (query.HasFeatureTransfers == false)
        {
            return Array.Empty<IQueryFeatureTransferBridge>();
        }

        return query.FeatureTransfers;
    }

    public IEnumerable<IEditThemeBridge> GetEditThemes(string serviceId)
    {
        return _cacheService.GetEditThemes(serviceId, _userIdentification)?.editthemes ?? new IEditThemeBridge[0];
    }

    public System.Xml.XmlNode TryGetEditThemeMaskXmlNode(string serviceId, string editThemeId)
    {
        var editTheme = GetEditTheme(serviceId, editThemeId);
        if (editTheme is EditThemeDTO && ((EditThemeDTO)editTheme).CanGenerateMaskXml == true)
        {
            return ((EditThemeDTO)editTheme).GenerateMaskXml(_userIdentification);
        }

        return null;
    }

    public IPrintLayoutBridge GetPrintLayout(string layoutId)
    {
        return _cacheService.GetPrintLayouts(this.GdiCustomGdiScheme, _userIdentification).Where(l => l.Id == layoutId).FirstOrDefault();
    }

    public IEnumerable<IPrintLayoutBridge> GetPrintLayouts(IEnumerable<string> layoutIds)
    {
        if (layoutIds?.Any() != true)
        {
            return new IPrintLayoutBridge[0];
        }

        return _cacheService.GetPrintLayouts(this.GdiCustomGdiScheme, _userIdentification).Where(l => layoutIds.Contains(l.Id)).ToArray();
    }

    public IEnumerable<IPrintFormatBridge> GetPrintFormats(string layoutId = "")
    {
        if (String.IsNullOrEmpty(layoutId))
        {
            return _cacheService.GetPrintFormats(this.GdiCustomGdiScheme, _userIdentification);
        }

        var map = _mapServiceInitializer.Map(_requestContext, _userIdentification);
        var printLayout = _cacheService.GetPrintLayouts(this.GdiCustomGdiScheme, _userIdentification).Where(l => l.Id == layoutId).FirstOrDefault();

        if (printLayout == null)
        {
            throw new Exception($"Configuration Error: Print Layout with id '{layoutId}' not found. Check CMS configuration");
        }

        var layoutBuilder = new E.Standard.WebMapping.GeoServices.Print.LayoutBuilder(map,
            _http,
            System.IO.Path.Combine(ApiGlobals.AppEtcPath, "layouts", printLayout.LayoutFile),
            E.Standard.WebMapping.GeoServices.Print.PageSize.A4,
            E.Standard.WebMapping.GeoServices.Print.PageOrientation.Landscape,
            96D);

        var allowedFormats = layoutBuilder.GetAllowedFormats();
        if (allowedFormats == null || allowedFormats.Count() == 0)
        {
            return GetPrintFormats();
        }

        List<PrintFormatDTO> formats = new List<PrintFormatDTO>();
        foreach (var allowedFormat in allowedFormats)
        {
            formats.Add(new PrintFormatDTO()
            {
                Size = (PageSize)Enum.Parse(typeof(PageSize), allowedFormat.size.ToString(), true),
                Orientation = (PageOrientation)Enum.Parse(typeof(PageOrientation), allowedFormat.orientation.ToString(), true)
            });
        }
        return formats;
    }

    public IEnumerable<(string serviceId, string layerId)> GetPrintLayoutShowLayers(string layoutId)
    {
        var map = _mapServiceInitializer.Map(_requestContext, _userIdentification);
        var printLayout = _cacheService.GetPrintLayouts(this.GdiCustomGdiScheme, _userIdentification).Where(l => l.Id == layoutId).FirstOrDefault();

        if (printLayout == null)
        {
            throw new Exception($"Configuration Error: Print Layout with id '{layoutId}' not found. Check CMS configuration");
        }

        var layoutBuilder = new E.Standard.WebMapping.GeoServices.Print.LayoutBuilder(map, _http,
            System.IO.Path.Combine(ApiGlobals.AppEtcPath, "layouts", printLayout.LayoutFile),
            E.Standard.WebMapping.GeoServices.Print.PageSize.A4,
            E.Standard.WebMapping.GeoServices.Print.PageOrientation.Landscape,
            96D);

        List<(string serviceId, string layerId)> result = new List<(string serviceId, string layerId)>();
        var showLayers = layoutBuilder.GetShowLayers();
        if (showLayers != null && showLayers.Count() > 0)
        {
            result.AddRange(showLayers.Where(l => l.Contains(":"))
                                      .Select(l =>
                                          (
                                                l.Substring(0, l.IndexOf(":")),
                                                l.Substring(l.IndexOf(":") + 1)
                                          )
                                          ));
        }

        return result;
    }

    public IEnumerable<(string serviceId, string layerId)> GetPrintLayoutHideLayers(string layoutId)
    {
        var map = _mapServiceInitializer.Map(_requestContext, _userIdentification);
        var printLayout = _cacheService.GetPrintLayouts(this.GdiCustomGdiScheme, _userIdentification).Where(l => l.Id == layoutId).FirstOrDefault();

        if (printLayout == null)
        {
            throw new Exception($"Configuration Error: Print Layout with id '{layoutId}' not found. Check CMS configuration");
        }

        var layoutBuilder = new E.Standard.WebMapping.GeoServices.Print.LayoutBuilder(map, _http,
            System.IO.Path.Combine(ApiGlobals.AppEtcPath, "layouts", printLayout.LayoutFile),
            E.Standard.WebMapping.GeoServices.Print.PageSize.A4,
            E.Standard.WebMapping.GeoServices.Print.PageOrientation.Landscape,
            96D);

        List<(string serviceId, string layerId)> result = new List<(string serviceId, string layerId)>();
        var showLayers = layoutBuilder.GetHideLayers();
        if (showLayers != null && showLayers.Count() > 0)
        {
            result.AddRange(showLayers.Where(l => l.Contains(":"))
                                      .Select(l =>
                                          (
                                                l.Substring(0, l.IndexOf(":")),
                                                l.Substring(l.IndexOf(":") + 1)
                                          )
                                          ));
        }

        return result;
    }

    public PrintLayerProperties GetPrintLayoutProperties(string layoutId, PageSize pageSize, PageOrientation pageOrientation, double scale)
    {
        if (String.IsNullOrEmpty(layoutId))
        {
            throw new Exception("Configuration Error: No print layout definied. Check MapBuilder configuration for this map!");
        }

        var map = _mapServiceInitializer.Map(_requestContext, _userIdentification);
        var printLayout = _cacheService.GetPrintLayouts(this.GdiCustomGdiScheme, _userIdentification).Where(l => l.Id == layoutId).FirstOrDefault();

        if (printLayout == null)
        {
            throw new Exception($"Configuration Error: Print Layout with id '{layoutId}' not found. Check CMS configuration");
        }

        var layoutBuilder = new E.Standard.WebMapping.GeoServices.Print.LayoutBuilder(map, _http,
            System.IO.Path.Combine(ApiGlobals.AppEtcPath, "layouts", printLayout.LayoutFile),
            (E.Standard.WebMapping.GeoServices.Print.PageSize)pageSize,
            (E.Standard.WebMapping.GeoServices.Print.PageOrientation)pageOrientation,
            96D);

        int width = layoutBuilder.MapPixels.Width;
        int height = layoutBuilder.MapPixels.Height;

        var layoutProperties = new PrintLayerProperties()
        {
            WorldSize = new DimensionF(width * (float)scale / (96f / 0.0254f), height * (float)scale / (96f / 0.0254f)),
            IsQueryDependent = layoutBuilder.HasHeaderIDQuery,
            QueryDependencyQueryUrl = layoutBuilder.HeaderIDQueryUrl,
            QueryDependencyQueryField = layoutBuilder.HeaderIDQueryField
        };

        return layoutProperties;
    }

    public IEnumerable<int> GetPrintLayoutScales(string layoutId)
    {
        if (String.IsNullOrEmpty(layoutId))
        {
            throw new Exception("Configuration Error: No print layout definied. Check MapBuilder configuration for this map!");
        }

        var map = _mapServiceInitializer.Map(_requestContext, _userIdentification);
        var printLayout = _cacheService.GetPrintLayouts(this.GdiCustomGdiScheme, _userIdentification).Where(l => l.Id == layoutId).FirstOrDefault();

        if (printLayout == null)
        {
            throw new Exception($"Configuration Error: Print Layout with id '{layoutId}' not found. Check CMS configuration");
        }

        var layoutBuilder = new E.Standard.WebMapping.GeoServices.Print.LayoutBuilder(map, _http,
            System.IO.Path.Combine(ApiGlobals.AppEtcPath, "layouts", printLayout.LayoutFile),
            E.Standard.WebMapping.GeoServices.Print.PageSize.A4,
            E.Standard.WebMapping.GeoServices.Print.PageOrientation.Landscape,
            96D);

        return layoutBuilder.GetAllowedScales();
    }

    public IEnumerable<IPrintLayoutTextBridge> GetPrintLayoutTextElements(string layoutId, bool allowFileName = false)
    {
        var map = _mapServiceInitializer.Map(_requestContext, _userIdentification);

        var fileTitle = (allowFileName && layoutId.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                ? layoutId
                : _cacheService.GetPrintLayouts(this.GdiCustomGdiScheme, _userIdentification).Where(l => l.Id == layoutId).FirstOrDefault()?.LayoutFile;
        if(String.IsNullOrEmpty(fileTitle))
        {
            throw new Exception($"Configuration Error: Print Layout with id '{layoutId}' not found. Check CMS configuration");
        }

        var fileName = System.IO.Path.Combine(ApiGlobals.AppEtcPath, "layouts", fileTitle);

        var layoutBuilder = new E.Standard.WebMapping.GeoServices.Print.LayoutBuilder(map, _http,
            fileName,
            E.Standard.WebMapping.GeoServices.Print.PageSize.A4,
            E.Standard.WebMapping.GeoServices.Print.PageOrientation.Landscape,
            96D,
            System.IO.Path.Combine(ApiGlobals.AppEtcPath, "layouts", "data"));

        List<IPrintLayoutTextBridge> ret = new List<IPrintLayoutTextBridge>();
        foreach (var layoutText in layoutBuilder.UserText ?? new List<E.Standard.WebMapping.GeoServices.Print.LayoutUserText>())
        {
            ret.Add(new PrintLayoutTextDTO()
            {
                Name = layoutText.Name,
                AliasName = layoutText.Aliasname,
                Default = layoutText.Value,
                MaxLength = layoutText.MaxLength,
                Visible = layoutText.Visible
            });
        }

        return ret;
    }

    public string GetQueryThemeId(IQueryBridge query)
    {
        if (query is QueryDTO && ((QueryDTO)query).Service != null)
        {
            return ((QueryDTO)query).Service.Url + ":" + ((QueryDTO)query).id;
        }
        return String.Empty;
    }

    async public Task<FeatureCollection> QueryLayerAsync(string serviceId, string layerId, ApiQueryFilter filter, string appendFilterClause = "")
    {
        IMapService service = await TryGetOriginalService(serviceId);

        if (service == null)
        {
            throw new ArgumentException("Unknown Service: " + serviceId);
        }

        QueryDTO query = (await GetFirstLayerQuery(serviceId, layerId)) as QueryDTO ?? new QueryDTO()
        {
            Bridge = this,
            LayerId = layerId,
            items = new QueryDTO.ItemDTO[0],
            AllowEmptyQueries = true
        };
        query.Init(service);

        return await query.PerformAsync(_requestContext, filter, appendFilterClause: appendFilterClause);
    }

    async public Task<FeatureCollection> QueryLayerAsync(string serviceId, string layerId, string whereClause, QueryFields fields, SpatialReference featureSpatialReferernce = null, Shape queryShape = null)
    {
        var service = await TryGetService(serviceId);

        if (service == null)
        {
            throw new ArgumentException("Unknown Service: " + serviceId);
        }

        ILayer layer = service.Layers.FindByLayerId(layerId);
        if (layer == null)
        {
            throw new ArgumentException("Unknown Layer: " + layerId);
        }

        QueryFilter filter = queryShape == null ? new QueryFilter(layer.IdFieldName, 1000, 0) : new SpatialFilter(layer.IdFieldName, queryShape, 1000, 0);
        switch (fields)
        {
            case QueryFields.Id:
                filter.SubFields = layer.IdFieldName;
                break;
            case QueryFields.Shape:
                filter.SubFields = layer.IdFieldName;
                filter.QueryGeometry = true;
                break;
            case QueryFields.TableFields: // ToDo
            case QueryFields.All:
                filter.SubFields = "*";
                filter.QueryGeometry = true;
                break;
        }
        filter.Where = String.IsNullOrWhiteSpace(whereClause) ? layer.IdFieldName + ">0" : whereClause;
        filter.FeatureSpatialReference = featureSpatialReferernce;
        if (queryShape != null)
        {
            ((SpatialFilter)filter).FilterSpatialReference = featureSpatialReferernce;
        }

        FeatureCollection features = new FeatureCollection();
        if (!await layer.GetFeaturesAsync(filter, features, _requestContext))
        {
            throw new ArgumentException("error when quering layer: " + layerId + " " + whereClause);
        }

        return features;
    }

    async public Task<int> QueryCountLayerAsync(string serviceId, string layerId, string whereClause)
    {
        var service = (await TryGetService(serviceId)).ThrowIfNull(() => $"Unknown Service: {serviceId}");
        var layer = service.Layers?.FindByLayerId(layerId).ThrowIfNull(() => $"Unknown Layer: {layerId}");

        if (!(layer is ILayer2) && !(layer is ILayer3))
        {
            return -1; // no implemented or coutable
        }

        QueryFilter filter = new QueryFilter(layer.IdFieldName, 1000, 0);
        filter.SubFields = layer.IdFieldName;

        filter.Where = String.IsNullOrWhiteSpace(whereClause) ? layer.IdFieldName + ">0" : whereClause;

        int count = -1;
        if (layer is ILayer3)
        {
            count = await ((ILayer3)layer).FeaturesCountOnly(filter, _requestContext);
        }
        else if (layer is ILayer2)
        {
            count = await ((ILayer2)layer).HasFeaturesAsync(filter, _requestContext);
        }

        return count;
    }

    async public Task<IEnumerable<T>> QueryLayerDistinctAsync<T>(string serviceId, string layerId, string distinctField,
                                                                 string whereClause = "", string orderBy = "", int limit = 0)
    {
        var service = (await TryGetService(serviceId)).ThrowIfNull(() => $"Unknown Service: {serviceId}");
        var layer = service.Layers?.FindByLayerId(layerId).ThrowIfNull(() => $"Unknown Layer: {layerId}");

        if (!(layer is ILayerDistinctQuery))
        {
            return Array.Empty<T>();
        }

        return (await ((ILayerDistinctQuery)layer).QueryDistinctValues(_requestContext, distinctField, whereClause, orderBy, limit))
            .Select(v => (T)Convert.ChangeType(v, typeof(T)));
    }

    async public Task<IEnumerable<string>> GetHasFeatureAttachments(string serviceId, string layerId, IEnumerable<string> objectIds)
    {
        var service = await TryGetOriginalService(serviceId).ThrowIfNull(() => $"Unknown Service: {serviceId}");
        var layer = service.Layers?.FindByLayerId(layerId).ThrowIfNull(() => $"Unknown Layer: {layerId}");

        if (layer is IFeatureAttachmentProvider attachmentContainer)
        {
            return await attachmentContainer.HasAttachmentsFor(_requestContext, objectIds);
        }

        return [];
    }

    async public Task<IFeatureAttachments> GetFeatureAttachments(string serviceId, string layerId, string objectId)
    {
        var service = await TryGetOriginalService(serviceId).ThrowIfNull(() => $"Unknown Service: {serviceId}");
        var layer = service.Layers?.FindByLayerId(layerId).ThrowIfNull(() => $"Unknown Layer: {layerId}");

        if (layer is IFeatureAttachmentProvider attachmentContainer)
        {
            return await attachmentContainer.GetAttachmentsFor(_requestContext, objectId);
        }

        return null;
    }

    async public Task<byte[]> GetFeatureAttachmentData(string serviceId, string attachementUri)
    {
        var service = await TryGetOriginalService(serviceId).ThrowIfNull(() => $"Unknown Service: {serviceId}");

        if (service is IServiceAttachmentProvider attachmentService)
        {
            return await attachmentService.GetServiceAttachementData(_requestContext, attachementUri);
        }

        return null;
    }

    public SpatialReference CreateSpatialReference(int sRefId)
    {
        return ApiGlobals.SRefStore.SpatialReferences.ById(sRefId);
    }

    public IGeometricTransformer GeometryTransformer(int fromSrId, int toSrId)
    {
        return new GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, fromSrId, toSrId);
    }

    public SpatialReference GetSupportedSpatialReference(IQueryBridge query, int defaultSrefId = 4326)
    {
        if (query is QueryDTO && ((QueryDTO)query).Service != null)
        {
            var service = ((QueryDTO)query).Service;
            if (service is IMapServiceSupportedCrs &&
                ((IMapServiceSupportedCrs)service).SupportedCrs != null &&
                ((IMapServiceSupportedCrs)service).SupportedCrs.Length > 0)
            {
                if (!((IMapServiceSupportedCrs)service).SupportedCrs.Contains(4326))
                {
                    defaultSrefId = ((IMapServiceSupportedCrs)service).SupportedCrs[0];
                }
            }
        }
        return ApiGlobals.SRefStore.SpatialReferences.ById(defaultSrefId);
    }

    public int DefaultSrefId { get; private set; }

    public string AppRootPath => ApiGlobals.AppRootPath;

    public string AppAssemblyPath => ApiGlobals.AppAssemblyPath;

    public string WWWRootPath => ApiGlobals.WWWRootPath;
    public string AppConfigPath => ApiGlobals.AppConfigPath;
    public string AppEtcPath => ApiGlobals.AppEtcPath;

    public string AppRootUrl => _urlHelper.AppRootUrl();

    public string WebGisPortalUrl => _urlHelper.PortalUrl();

    public string OutputPath => _urlHelper.OutputPath();
    public string OutputUrl => _urlHelper.OutputUrl();

    public string ToolCommandUrl(string command, object parameters = null)
    {
        string url = AppRootUrl + "/rest/toolmethod?method=" + command + "&toolid=" + this.CurrentTool.GetType().ToToolId();

        if (parameters != null)
        {
            foreach (var propertyInfo in parameters.GetType().GetProperties())
            {
                object val = propertyInfo.GetValue(parameters);
                if (val != null)
                {
                    url += "&" + propertyInfo.Name + "=" + val.ToString();
                }
            }
        }

        return url;
    }

    public IBridgeUser CurrentUser
    {
        get
        {
            if (_userIdentification == null || String.IsNullOrWhiteSpace(_userIdentification.Username))
            {
                var anonymousUser = new BridgeUser(String.Empty);
                if (!_anonymousUserGuid.Equals(new Guid()))
                {
                    anonymousUser.SetAnonymousUserId(_anonymousUserGuid);
                }
                return anonymousUser;
            }

            return new BridgeUser(_userIdentification.Username, _userIdentification.Userroles, _userIdentification.UserrolesParameters);
        }
    }

    private Guid _anonymousUserGuid = new Guid();
    public void SetAnonymousUserGuid(Guid guid)
    {
        if (_anonymousUserGuid.Equals(new Guid()))
        {
            _anonymousUserGuid = guid;
        }
        else
        {
            throw new Exception("Anonymous Guid alread set");
        }
    }

    public T ToolConfigValue<T>(string toolConfigKey)
    {
        object val = _cacheService.ToolConfigValue(toolConfigKey, _userIdentification);

        if (val == null && (typeof(T).Equals(typeof(System.String))))
        {
            val = String.Empty;
        }
        else if (val == null)
        {
            val = Activator.CreateInstance(typeof(T));
        }

        return (T)Convert.ChangeType(val, typeof(T));
    }

    public T[] ToolConfigValues<T>(string toolConfigKey)
    {
        List<T> values = new List<T>();

        foreach (var val in _cacheService.ToolConfigValues(toolConfigKey, _userIdentification))
        {
            values.Add((T)Convert.ChangeType(val, typeof(T)));
        }

        return values.ToArray();
    }

    public IApiButton TryGetFriendApiButton(IApiButton sender, string id)
    {
        id = id.ToLower();
        string client = sender?.GetType().GetCustomAttribute<ToolClientAttribute>()?.ClientName;

        var button = _cacheService.GetApiTools(client).Where(b => b.GetType().ToToolId() == id).FirstOrDefault();

        if (button != null)
        {
            if (button.GetType().Assembly.ToString() == sender.GetType().Assembly.ToString())  // From same Assembly-> Friend
            {
                return button;
            }
            // DoTo: Reflection Attribute -> Friend..
        }

        return null;
    }

    #region IAppCryptography

    public string SecurityEncryptString(string input)
    {
        return _crypto.EncryptTextDefault(input, CryptoResultStringType.Hex);
    }

    public string SecurityDecryptString(string encInput)
    {
        return _crypto.DecryptTextDefault(encInput);
    }

    public string SecurityEncryptString(string input, int customPasswordIndex, CryptoStrength strength = CryptoStrength.AES256)
    {
        return _crypto.EncryptText(
            input,
            customPasswordIndex,
            CryptoStrength.AES256,
            true,
            CryptoResultStringType.Hex);
    }

    public string SecurityDecryptString(string encInput, int customPasswordIndex, CryptoStrength strength = CryptoStrength.AES256)
    {
        return _crypto.DecryptText(encInput, customPasswordIndex, strength, true);
    }

    #endregion

    public IStorage Storage
    {
        get;
        private set;
    }

    public ApiToolEventArguments CurrentEventArguments { get; /*internal*/ set; }

    async public Task<IEnumerable<IGeoReferenceItem>> GeoReferenceAsync(string term, string serviceIds = "", string categories = "", string exclude = "geojuhu")
    {
        var ret = await GeoReferenceAsync(new string[] { term }, serviceIds, categories, exclude);
        if (ret != null && ret.ContainsKey(term))
        {
            return ret[term];
        }

        return new IGeoReferenceItem[0];
    }

    async public Task<Dictionary<string, IEnumerable<IGeoReferenceItem>>> GeoReferenceAsync(IEnumerable<string> terms, string serviceIds = "", string categories = "", string exclude = "geojuhu")
    {
        var services = _cacheService.GetSearchServices(this._userIdentification,
            String.IsNullOrWhiteSpace(serviceIds) ? null : serviceIds.Split(',').Select(s => s.Trim()).ToArray());

        terms = terms.Distinct();
        var searchItemsDict = new Dictionary<string, ThreadSafeList<SearchServiceItem>>();

        foreach (string term in terms)
        {
            searchItemsDict[term] = new ThreadSafeList<SearchServiceItem>();
        }

        foreach (var service in services)
        {
            if (!String.IsNullOrWhiteSpace(exclude) && exclude.ToLower().Split(',').Contains(service.Name.ToLower()))
            {
                continue;
            }

            if (service is ISearchService2)
            {
                string[] categoriesArray = String.IsNullOrEmpty(categories) ? null : categories.Split(',');

                foreach (string term in terms)
                {
                    var searchItems = await ((ISearchService2)service).Query2Async(_http, term, 5, categoriesArray);
                    if (searchItems?.Items != null)
                    {
                        searchItemsDict[term].AddRange(searchItems.Items);
                    }
                }

                // Parallelisierung haut mit async noch nicht hin, frage: braucht man das?
                //Parallel.ForEach(terms,
                //    new ParallelOptions { MaxDegreeOfParallelism = 5 },
                //    async (term) =>
                //{
                //    var searchItems = await ((ISearchService2)service).Query2Async(term, 5, categoriesArray);
                //    if (searchItems?.Items != null)
                //        searchItemsDict[term].AddRange(searchItems.Items);
                //});
            }
            //else
            //{
            //    var searchItems = service.Query(term, 5);
            //    searchItemsList.AddRange(searchItems.Items);
            //}
        }

        if (services.Count() > 1)
        {
            foreach (string term in terms)
            {
                SortSearchItemByScore.Score(searchItemsDict[term], term);
            }
        }

        var ret = new ThreadSafeDictionary<string, IEnumerable<IGeoReferenceItem>>();

        foreach (var term in terms)
        {
            if (searchItemsDict[term].Where(i => i.Score != 0).Count() > 0)
            {
                searchItemsDict[term].Sort(new SortSearchItemByScore());
            }

            ret.Add(term, searchItemsDict[term].Take(5).Select(m => new GeoReferenceItemDTO(m)).ToArray());
        }

        return ret;
    }

    public string ToGeoJson(FeatureCollection featureCollection)
    {
        var features = new FeaturesDTO(featureCollection);

        return JSerializer.Serialize(features);
    }

    public IMap TemporaryMapObject()
    {
        IMap map = _mapServiceInitializer.Map(_requestContext, this._userIdentification, "temp");
        return map;
    }

    #region Lookup

    async public Task<IDictionary<T, T>> GetLookupValues<T>(IApiObjectBridge apiObject,
                                                            string term = "",
                                                            string parameter = "",
                                                            Dictionary<string, string> replacements = null,
                                                            string serviceId = "", string layerId = "")
    {
        ILookupConnection lookupConnection = null;

        if (apiObject is ILookupConnection)
        {
            lookupConnection = (ILookupConnection)apiObject;
        }

        if (apiObject is ILookup)
        {
            lookupConnection = ((ILookup)apiObject).GetLookupConnection(parameter);
        }

        ILayer layer = null;

        if (_lookup.RequiresLayer(lookupConnection))
        {
            var service = await this.TryGetService(serviceId);
            layer = service?.Layers?.FindByLayerId(layerId)
                        .ThrowIfNull(() => "Can't determine required service layer object to get lookup values");
        }

        return await _lookup.GetLookupValues<T, T>(lookupConnection, term, layer, parameter, replacements);
    }

    #endregion

    public IImpersonator Impersonator()
    {
        return new Impersonator(_config[ApiConfigKeys.ImpersonateUser]);
    }

    async public Task<IFeatureWorkspace> TryGetFeatureWorkspace(string serviceId, string layerId)
    {
        IMapService service = await TryGetOriginalService(serviceId);

        if (service is IFeatureWorkspaceProvider)
        {
            var featureWorkspace = ((IFeatureWorkspaceProvider)service).GetFeatureWorkspace(layerId);

            return featureWorkspace;
        }

        return null;
    }

    public WebProxy GetWebProxy(string server)
    {
        return _http.GetProxy(server);
    }

    async public Task<IEnumerable<LayerLegendItem>> GetLayerLegendItems(string serviceId, string layerId)
    {
        IMapService service = await TryGetOriginalService(serviceId);

        var layer = service?.Layers.FindByLayerId(layerId);
        if (layer != null && service is IMapServiceLegend2)
        {
            return await ((IMapServiceLegend2)service).GetLayerLegendItemsAsync(layerId, _requestContext);
        }

        return new LayerLegendItem[0];
    }

    async public Task<IEnumerable<KeyValuePair<string, string>>> GetLayerFieldDomains(string serviceId, string layerId, string fieldName)
    {
        IMapService service = await TryGetOriginalService(serviceId);

        var layer = service?.Layers.FindByLayerId(layerId);

        if (layer is ILayerFieldDomains)
        {
            return ((ILayerFieldDomains)layer).CodedValues(fieldName);
        }

        return new KeyValuePair<string, string>[0];
    }

    async public Task<FieldCollection> GetServiceLayerFields(string serviceId, string layerId)
    {
        IMapService service = await TryGetOriginalService(serviceId);
        ILayer layer = service?.Layers?.FindByLayerId(layerId);

        return layer?.Fields;
    }

    public bool IsInDeveloperMode
    {
        get
        {
            return ApiGlobals.IsInDeveloperMode;
        }
    }

    public void Trace(string message)
    {
        string fileName = $"{_urlHelper.OutputPath()}/api_trace.log";
        File.AppendAllLines(fileName,
                    new string[]
                    {
                        $"{DateTime.UtcNow.ToShortDateString()} {DateTime.UtcNow.ToLongTimeString()} (UTC) {message}",
                        "-"
                    });
    }

    public string GetCustomTextBlock(IApiButton tool, string name, string defaultTextBlock)
    {
        try
        {
            var fileInfo = new FileInfo($"{this.AppRootPath}/system/ui/labels/{tool.GetType()}-{name}.txt");
            if (fileInfo.Exists)
            {
                return File.ReadAllText(fileInfo.FullName);
            }
        }
        catch { }

        return defaultTextBlock;
    }

    public System.Text.Encoding DefaultTextEncoding => _config.DefaultTextDownloadEncoding();

    public string GetOriginalUrlParameterValue(string parameter, bool ignoreCase = true)
    {
        string urlParameterValue = String.Empty;

        if (_httpRequestContext.OriginalUrlParameters != null)
        {
            urlParameterValue = _httpRequestContext.OriginalUrlParameters.GetValue(parameter, ignoreCase);
        }

        return urlParameterValue;
    }

    #endregion

    #region IBridge Favorites

    async public Task<bool> SetUserFavoritesItemAsync(IApiButton tool, string methodName, string item)
    {
        if (_userIdentification == null || _userIdentification.IsAnonymous || String.IsNullOrWhiteSpace(_userIdentification.Task))  // egal
        {
            return true;
        }

        if (tool == null)
        {
            throw new ArgumentException("tool == null");
        }

        if (String.IsNullOrWhiteSpace(methodName))
        {
            throw new ArgumentException("methodName is empty");
        }

        var subscriberDb = _subscriberDb.CreateInstance();
        return await subscriberDb.SetFavItemAsync(_userIdentification.Username, _userIdentification.Task, tool.GetType().ToToolId() + "." + methodName, item);
    }

    async public Task<IEnumerable<string>> GetUserFavoriteItemsAsync(IApiButton tool, string methodName, int max = 12)
    {
        if (_userIdentification == null || _userIdentification.IsAnonymous || String.IsNullOrWhiteSpace(_userIdentification.Task))  // egal
        {
            return new string[0];
        }

        if (tool == null)
        {
            throw new ArgumentException("tool == null");
        }

        if (String.IsNullOrWhiteSpace(methodName))
        {
            throw new ArgumentException("methodName is empty");
        }

        var subscriberDb = _subscriberDb.CreateInstance();
        return (await subscriberDb.GetFavItemsAsync(_userIdentification.Username, _userIdentification.Task, tool.GetType().ToToolId() + "." + methodName) ?? new string[0]).Take(max);
    }

    #endregion

    #region IBridge WebGIS Cloud

    public bool HasWebGISCloudConnection => false; //!String.IsNullOrWhiteSpace(ApiGlobals.CloudWebPortalUrl);

    #endregion

    #region BridgeUser

    public string CreateNewAnoymousCliendsideUserId()
    {
        return CreateAnonymousClientSideUserId(Guid.NewGuid());
    }

    public string CreateAnonymousClientSideUserId(Guid guid)
    {
        // legacy
        //return _crypto.EncryptText(guid.ToString(), (int)CustomPasswords.ApiBridgeUserCryptoPassword, CryptoStrength.AES256, true);

        return _crypto.EncryptText(guid.ToString(), (int)CustomPasswords.ApiStoragePassword, CryptoStrength.AES256, true);
    }

    public Guid AnonymousUserGuid(string anonymousUserId)
    {
        // legacy: CustomPasswords.ApiBridgeUserCryptoPassword was used
        foreach (var pwIndex in new int[] { (int)CustomPasswords.ApiStoragePassword,
                                            (int)CustomPasswords.ApiBridgeUserCryptoPassword })
        {
            try
            {
                return new Guid(_crypto.DecryptText(anonymousUserId, pwIndex, CryptoStrength.AES256, true));
            }
            catch { }
        }

        throw new Exception("recovery key has an invalid format");
    }

    #endregion

    #region UserAgent

    private IUserAgent _userAgent = null;

    public IUserAgent UserAgent
    {
        get
        {
            if (_userAgent == null)
            {
                _userAgent = new UserAgentClass(this);
            }

            return _userAgent;
        }
    }

    #endregion

    public IHttpService HttpService => _http;
    public IRequestContext RequestContext => _requestContext;

    public ICryptoService CryptoService => _crypto;

    #region Environment



    #endregion

    public string CreateCustomSelection(Shape shape, SpatialReference shapesSRef)
    {
        if (shapesSRef != null)
        {
            #region Clone

            Shape clonedShape = null;

            if (shape is Point)
            {
                clonedShape = new Point();
            }
            else if (shape is Envelope)
            {
                clonedShape = new Envelope();
            }
            else if (shape is Polyline)
            {
                clonedShape = new Polyline();
            }
            else if (shape is Polygon)
            {
                clonedShape = new Polygon();
            }

            if (clonedShape == null)
            {
                return String.Empty;
            }

            using (var writerMs = new MemoryStream())
            using (var writer = new BinaryWriter(writerMs))
            {
                shape.Serialize(writer);

                using (var readerMs = new MemoryStream(writerMs.ToArray()))
                {
                    using (var reader = new BinaryReader(readerMs))
                    {
                        clonedShape.Deserialize(reader);
                    }
                }
            }

            #endregion

            using (GeometricTransformerPro transformer = new GeometricTransformerPro(shapesSRef, CoreApiGlobals.SRefStore.SpatialReferences.ById(4326)))
            {
                transformer.Transform(clonedShape);
            }

            var bufferJsonString = clonedShape.ToGeoJson(new WebMapping.Core.Attribute[]{
                        new WebMapping.Core.Attribute("stroke","#000"),
                        new WebMapping.Core.Attribute("stroke-opacity",".5"),
                        new WebMapping.Core.Attribute("fill","#aaa"),
                        new WebMapping.Core.Attribute("fill-opacity",".2"),
                    });

            string customSelectionId = $"buffer_{Guid.NewGuid().ToString("N").ToLower()}";
            File.WriteAllText($"{this.OutputPath}/{customSelectionId}.json", bufferJsonString);

            return customSelectionId;
        }

        return String.Empty;
    }

    public ILocalizer<T> GetLocalizer<T>()
        => new Localizer<T>(_localizer);

    #region Tools

    public IApiButton GetTool(string toolId)
    {
        return _cacheService.GetTool(toolId);
    }

    #endregion

    #region Sub Classes

    public class SortSearchItemByScore : IComparer<SearchServiceItem>
    {
        public int Compare(SearchServiceItem x, SearchServiceItem y)
        {
            if (x == null && y != null)
            {
                return 1;
            }

            if (y == null && x != null)
            {
                return -1;
            }

            if (x == null && y == null)
            {
                return 0;
            }

            if (x.DoYouMean == true && y.DoYouMean == false)
            {
                return 1;
            }

            if (x.DoYouMean == false && y.DoYouMean == true)
            {
                return -1;
            }

            if (x.Score == y.Score)  // Solr -> keine Ranking möglich -> Sortierung bleibt gleich
            {
                return 0;            // Wenn Scores gleich sind muss 0 zurück gegeben werden, sonst wirft List.Sort() eine Exception!!!
            }

            return x.Score.CompareTo(y.Score) * -1;
        }

        static public bool CanSort(IEnumerable<SearchServiceItem> items)
        {
            return items.Where(m => m.Score != 0).Count() > 0;
        }

        static public void Score(IEnumerable<SearchServiceItem> items, string term)
        {
            term = term.ToLower().Trim();

            foreach (var item in items)
            {
                string suggestedText = item.SuggestText.ToLower();

                if (suggestedText.Trim() == term)
                {
                    item.Score += 1000.0D;
                }
                else if (suggestedText.StartsWith(term + " "))
                {
                    item.Score += 100;
                }
                else if (suggestedText.StartsWith(term))
                {
                    item.Score += 30;
                }
                else if (suggestedText.Contains(term))
                {
                    item.Score += 20;
                }
            }
        }
    }

    private class UserAgentClass : IUserAgent
    {
        private readonly Bridge _bridge;

        public UserAgentClass(Bridge bridge)
        {
            _bridge = bridge;
        }

        private Standard.Web.UserAgents.Browsers.IBrowser _browser;

        private Standard.Web.UserAgents.Browsers.IBrowser DetectBrowser()
        {
            try
            {
                if (_browser == null)
                {
                    var userAgent = _bridge?.Request?.Headers["User-Agent"];
                    if (!String.IsNullOrEmpty(userAgent))
                    {
                        _browser = new Standard.Web.UserAgents.Detection.BrowserDetector().GetBrowser(userAgent);
                    }
                }
            }
            catch { }

            return _browser;
        }

        public bool IsInternetExplorer => DetectBrowser()?.Name == Standard.Web.UserAgents.Constants.BrowserNames.InternetExplorer;
        public bool IsChrome => DetectBrowser()?.Name == Standard.Web.UserAgents.Constants.BrowserNames.Chrome;
        public bool IsEdge => DetectBrowser()?.Name == Standard.Web.UserAgents.Constants.BrowserNames.Edge;
        public bool IsEdgeChromium => DetectBrowser()?.Name == Standard.Web.UserAgents.Constants.BrowserNames.EdgeChromium;
        public bool IsFirefox => DetectBrowser()?.Name == Standard.Web.UserAgents.Constants.BrowserNames.Firefox;
        public bool IsSafari => DetectBrowser()?.Name == Standard.Web.UserAgents.Constants.BrowserNames.Safari;
        public bool IsOpera => DetectBrowser()?.Name == Standard.Web.UserAgents.Constants.BrowserNames.Opera;
    }

    // garbage
    private class Localizer<T> : ILocalizer<T>
    {
        private readonly IStringLocalizer _stringLocalizer;
        private string _localizationNamespace;

        public Localizer(IStringLocalizer stringLocalizer)
        {
            _stringLocalizer = stringLocalizer;

            _localizationNamespace = typeof(T).GetLocalizationNamespace();
        }

        public ILocalizer<TClass> CreateFor<TClass>()
        {
            return new Localizer<TClass>(_stringLocalizer);
        }

        public string Localize(string key)
        {
            var val = _stringLocalizer[$"{_localizationNamespace}.{key}"];

            if (val.ResourceNotFound)  // fallback. Without namespace
            {
                val = _stringLocalizer[key];
            }

            return val.Value;
        }
    }

    #endregion

    #region Helper

    async private Task<IMapService> TryGetService(string serviceId, IMap parent = null)
    {
        try
        {
            var service =
                await _cacheService.GetService(serviceId, parent, this._userIdentification, _urlHelper) ??
                await _mapServiceInitializer.GetCustomServiceByUrlAsync(serviceId, parent ?? _mapServiceInitializer.Map(_requestContext, _userIdentification), _userIdentification, this.Request?.Form);

            return service;
        }
        catch
        {
            return null;
        }
    }

    async private Task<IMapService> TryGetOriginalService(string serviceId)
    {
        try
        {
            var service =
                await _cacheService.GetOriginalService(serviceId, _userIdentification, _urlHelper) ??
                await _mapServiceInitializer.GetCustomServiceByUrlAsync(serviceId, _mapServiceInitializer.Map(_requestContext, _userIdentification), _userIdentification, this.Request?.Form);

            return service;
        }
        catch
        {
            return null;
        }
    }

    #endregion
}

