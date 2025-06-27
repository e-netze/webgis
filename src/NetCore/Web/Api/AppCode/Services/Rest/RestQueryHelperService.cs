using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Mvc;
using E.Standard.Api.App;
using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.Configuration.Services;
using E.Standard.Extensions.Compare;
using E.Standard.Platform;
using E.Standard.Security.Core;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.Core;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Exceptions;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.Rest;

public class RestQueryHelperService
{
    private readonly RestHelperService _restHelper;
    private readonly HttpRequestContextService _httpRequestContext;
    private readonly BridgeService _bridge;
    private readonly CacheService _cache;
    private readonly ConfigurationService _config;
    private readonly UrlHelperService _urlHelper;
    private readonly MapServiceInitializerService _mapServiceInitializer;
    private readonly IRequestContext _requestContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CancellationTokenService _cancellationToken;

    public RestQueryHelperService(RestHelperService restHelper,
                                  HttpRequestContextService httpRequestContext,
                                  BridgeService bridge,
                                  CacheService cache,
                                  ConfigurationService config,
                                  UrlHelperService urlHelper,
                                  MapServiceInitializerService mapServiceInitializer,
                                  IRequestContext requestContext,
                                  IHttpContextAccessor httpContextAccessor,
                                  CancellationTokenService cancellationToken)
    {
        _restHelper = restHelper;
        _httpRequestContext = httpRequestContext;
        _bridge = bridge;
        _cache = cache;
        _config = config;
        _urlHelper = urlHelper;
        _mapServiceInitializer = mapServiceInitializer;
        _requestContext = requestContext;
        _httpContextAccessor = httpContextAccessor;
        _cancellationToken = cancellationToken;
    }

    #region Query

    async public Task<IActionResult> PerformQueryAsync(ApiBaseController controller,
                                                       string serviceId,
                                                       string queryId,
                                                       System.Collections.Specialized.NameValueCollection form,
                                                       bool json, QueryGeometryType geometryType,
                                                       CmsDocument.UserIdentification ui,
                                                       bool renderFields = true,
                                                       bool select = false,
                                                       int srs = 0,
                                                       bool appendHoverShape = true)
    {
        var watch = new StopWatch(serviceId);

        var query = await this.GetQuery(serviceId, queryId, ui);
        if (query == null)
        {
            throw new ArgumentException("Query " + queryId + " not found in service " + serviceId);
        }

        var service = query.Service;
        var layer = service.Layers.Find(l => l.ID == query.LayerId);
        if (layer == null)
        {
            throw new Exception($"unknown layer: {query.LayerId}");
        }

        int queryScale = String.IsNullOrEmpty(form?["_scale"]) ? 0 : Convert.ToInt32(form["_scale"]);

        #region VisFilters

        string visFilterClause = LayerVisFilterClause(controller.Request, service, layer, ui);

        #endregion

        int srefId = srs.OrTake(_config.DefaultQuerySrefId());
        if (service is IMapServiceSupportedCrs &&
            ((IMapServiceSupportedCrs)service).SupportedCrs != null &&
            ((IMapServiceSupportedCrs)service).SupportedCrs.Length > 0)
        {
            if (!((IMapServiceSupportedCrs)service).SupportedCrs.Contains(srefId))
            {
                srefId = ((IMapServiceSupportedCrs)service).SupportedCrs[0];
            }
        }

        SpatialReference sRef = E.Standard.Api.App.ApiGlobals.SRefStore.SpatialReferences.ById(srefId), filterSref = null;
        Shape queryShape = null;
        Point orderByDistancePoint = null;

        string orderBy = String.IsNullOrEmpty(form?["_orderby"]) ? "" : form["_orderby"];
        int limit = String.IsNullOrEmpty(form?["_limit"]) ? query.MaxFeatures : Convert.ToInt32(form["_limit"]);
        int targetSRefId = String.IsNullOrEmpty(form?["_outsrs"]) ? 4326 : Convert.ToInt32(form["_outsrs"]);
        if (targetSRefId == 0)
        {
            targetSRefId = 4326;
        }
        if (!String.IsNullOrEmpty(form?["_bbox"]))
        {
            queryShape = new Envelope(form["_bbox"].Split(',').Select(s => s.ToPlatformDouble()).ToArray());
            filterSref = E.Standard.Api.App.ApiGlobals.SRefStore.SpatialReferences.ById(4326);

            if (!String.IsNullOrEmpty(form?["_bbox_rotation"]))
            {
                var rotation = form["_bbox_rotation"].ToPlatformDouble();
                queryShape = ((Envelope)queryShape).ToPolygon();
                using (var geotransformer = new GeometricTransformerPro(sRef, filterSref))
                {
                    geotransformer.InvTransform(queryShape);
                    SpatialAlgorithms.Rotate(queryShape, queryShape.ShapeEnvelope.CenterPoint, rotation);
                    geotransformer.Transform(queryShape);
                }
            }

            orderByDistancePoint = queryShape.ShapeEnvelope.CenterPoint;
            using (var geotransformer = new GeometricTransformerPro(filterSref, sRef))
            {
                geotransformer.Transform(orderByDistancePoint);
            }
        }

        var engine = new QueryEngine();
        bool foundFeatuers = await engine.PerformAsync(_requestContext,
                                      query,
                                      form,
                                      geometryType != QueryGeometryType.None,
                                      sRef,
                                      queryShape: queryShape,
                                      filterSref: filterSref,
                                      visFilterClause: visFilterClause,
                                      limit: limit,
                                      mapScale: queryScale,
                                      orderBy: orderBy);

        FeatureCollection queryFeatures = engine.FeaturesOrEmtpyFeatureCollection;

        if (query.Distinct == true)
        {
            queryFeatures.Distinct(true, query.AllFieldNames());
        }

        if (orderByDistancePoint != null)
        {
            queryFeatures.OrderByPointDistance(orderByDistancePoint);
        }

        FeatureCollection returnFeatures = await _restHelper.PrepareFeatureCollection(queryFeatures, query, sRef, ui, null, geometryType, renderFields,
            targetSRefId: targetSRefId, appendHoverShape: appendHoverShape);

        var resultFeatures = new FeaturesDTO(returnFeatures, FeaturesDTO.Meta.Tool.Query, sortColumns: query.AutoSortFields, select: select);

        if (query.Union == true)  // union
        {
            resultFeatures.Union();
        }

        #region Check if visfilters can applied

        try
        {
            if (resultFeatures.metadata != null)
            {
                resultFeatures.metadata.ServiceId = serviceId;
                resultFeatures.metadata.QueryId = queryId;
                resultFeatures.metadata.HashCode = resultFeatures.CalcHashCode();
            }

            if (resultFeatures.metadata != null && query?.Service?.Layers != null)
            {
                if (layer != null)
                {
                    List<VisFilterDTO> applicableVisFilters = new List<VisFilterDTO>();
                    foreach (var visFilter in _cache.GetAllVisFilters(query.Service.Url, ui).filters.Where(f => f.FilterType != VisFilterType.locked))
                    {
                        if ((visFilter.LayerNamesString ?? String.Empty).Split(';').Contains(layer.Name))
                        {
                            var firstFeature = queryFeatures.FirstOrDefault();
                            if (firstFeature != null)
                            {
                                bool hasAllParameters = visFilter.Parameters.Keys.Count > 0;
                                foreach (var parameter in visFilter.Parameters.Keys)
                                {
                                    if (firstFeature.Attributes.Where(a => a.Name == parameter).Count() == 0)
                                    {
                                        hasAllParameters = false;
                                        break;
                                    }
                                }

                                if (hasAllParameters)
                                {
                                    applicableVisFilters.Add(visFilter);
                                }
                            }
                        }
                    }

                    if (applicableVisFilters.Count > 0)
                    {
                        resultFeatures.metadata.ApplicableVisFilters =
                            String.Join(",", applicableVisFilters.Select(f => f.Id.ToUniqueFilterId(query.Service.Url)).ToArray());
                    }
                }
            }
        }
        catch { }

        #endregion

        if (json)
        {
            return await controller.JsonObject(watch.Apply<FeaturesDTO>(resultFeatures));
        }

        return await controller.ApiObject(resultFeatures);
    }

    async public Task<IActionResult> PerformIdentify(ApiBaseController controller, string serviceId, string queryId, System.Collections.Specialized.NameValueCollection form, bool json, QueryGeometryType geometryType, CmsDocument.UserIdentification ui, bool renderFields = true)
    {
        var watch = new StopWatch(serviceId);

        var query = await this.GetQuery(serviceId, queryId, ui);
        if (query == null)
        {
            throw new ArgumentException("Query " + queryId + " not found in service " + serviceId);
        }

        var service = query.Service;
        var layer = service.Layers.Find(l => l.ID == query.LayerId);
        if (layer == null)
        {
            throw new Exception($"unknown layer: {query.LayerId}");
        }

        #region VisFilters

        var visFilterClause = LayerVisFilterClause(controller.Request, service, layer, ui);

        #endregion

        int srefId = !String.IsNullOrWhiteSpace(form["calc_srs"]) ? int.Parse(form["calc_srs"]) : 4326;
        bool transformToSref = false;

        if (service is IMapServiceSupportedCrs &&
            ((IMapServiceSupportedCrs)service).SupportedCrs != null &&
            ((IMapServiceSupportedCrs)service).SupportedCrs.Length > 0)
        {
            if (!((IMapServiceSupportedCrs)service).SupportedCrs.Contains(4326))
            {
                srefId = ((IMapServiceSupportedCrs)service).SupportedCrs[0];
                transformToSref = true;
            }
        }

        SpatialReference sRef = E.Standard.Api.App.ApiGlobals.SRefStore.SpatialReferences.ById(srefId);

        if (String.IsNullOrWhiteSpace(form["shape"]))
        {
            throw new ArgumentException("Shape parameter missing");
        }

        Shape queryShape = form["shape"].ShapeFromWKT()
            .ThrowIfNull(() => $"parameter shape is not a valid WKT geometry: {form["shape"]}");

        SpatialReference shapeSRef = null;
        if (!String.IsNullOrWhiteSpace(form["shape_srs"]))
        {
            shapeSRef = E.Standard.Api.App.ApiGlobals.SRefStore.SpatialReferences.ById(int.Parse(form["shape_srs"]));
        }
        if (!String.IsNullOrWhiteSpace(form["shape_buffer"]))
        {
            if (sRef.Id != shapeSRef.Id)
            {
                using (var transformer = new GeometricTransformerPro(shapeSRef, sRef))
                {
                    shapeSRef = sRef;
                    transformer.Transform(queryShape);
                }
            }

            using (var cts = new CancellationTokenSource())
            {
                queryShape = queryShape.CalcBuffer(form["shape_buffer"].ToPlatformDouble(), cts);
            }
        }

        if (transformToSref && shapeSRef != null && shapeSRef.Id != sRef.Id)
        {
            using (var transformer = new GeometricTransformerPro(shapeSRef, sRef))
            {
                shapeSRef = sRef;
                transformer.Transform(queryShape);
            }
        }

        var advancedQueryMethod = QueryEngine.AdvancedQueryMethod.Normal;
        if (!String.IsNullOrWhiteSpace(form["query_method"]))
        {
            if (!Enum.TryParse<QueryEngine.AdvancedQueryMethod>(form["query_method"], true, out advancedQueryMethod))
            {
                throw new Exception("Unknown query_method");
            }
        }

        var engine = new QueryEngine();

        bool foundFeatures = await engine.PerformAsync(_requestContext,
                                      query,
                                      form,
                                      geometryType != QueryGeometryType.None,
                                      sRef,
                                      queryShape: queryShape,
                                      filterSref: shapeSRef,
                                      visFilterClause: visFilterClause.ToString(),
                                      advancedQueryMethod: advancedQueryMethod);

        FeatureCollection queryFeatures = engine.FeaturesOrEmtpyFeatureCollection;
        if (advancedQueryMethod == QueryEngine.AdvancedQueryMethod.HasFeatures)
        {
            return await controller.JsonObject(watch.Apply<FeaturesCount>(new FeaturesCount()
            {
                success = true,
                hasfeatures = engine.FeatureCount > 0,
                count = engine.FeatureCount
            }));
        }
        FeatureCollection returnFeatures = await _restHelper.PrepareFeatureCollection(queryFeatures, query, sRef, ui, null, geometryType, renderFields);

        if (json)
        {
            return await controller.JsonObject(watch.Apply<FeaturesDTO>(new FeaturesDTO(returnFeatures, FeaturesDTO.Meta.Tool.Identify, sortColumns: query.AutoSortFields)));
        }

        return await controller.ApiObject(new FeaturesDTO(returnFeatures, FeaturesDTO.Meta.Tool.Identify, sortColumns: query.AutoSortFields));
    }

    async public Task<IActionResult> PerformBufferAsync(ApiBaseController controller,
                                                        string serviceId,
                                                        string queryId,
                                                        string sourceServiceId,
                                                        string sourceQueryId,
                                                        string sourceOids,
                                                        double bufferDistance,
                                                        bool json,
                                                        CmsDocument.UserIdentification ui,
                                                        bool renderFields = true)
    {
        var httpRequest = controller.Request;
        var watch = new StopWatch(serviceId);

        var sourceQuery = await this.GetQuery(sourceServiceId, sourceQueryId, ui);
        if (sourceQuery == null)
        {
            throw new ArgumentException("Query " + queryId + " not found in service " + serviceId);
        }

        var sourceService = sourceQuery.Service;
        var sourceLayer = sourceService.Layers.Find(l => l.ID == sourceQuery.LayerId);

        var query = await this.GetQuery(serviceId, queryId, ui);
        if (query == null)
        {
            throw new ArgumentException("Query " + queryId + " not found in service " + serviceId);
        }

        var service = query.Service;
        var layer = service.Layers.Find(l => l.ID == query.LayerId);
        if (layer == null)
        {
            throw new Exception($"unknown layer: {query.LayerId}");
        }


        int srefId = 4326;
        if (service is IMapServiceSupportedCrs &&
            ((IMapServiceSupportedCrs)service).SupportedCrs != null &&
            ((IMapServiceSupportedCrs)service).SupportedCrs.Length > 0)
        {
            if (!((IMapServiceSupportedCrs)service).SupportedCrs.Contains(4326))
            {
                srefId = ((IMapServiceSupportedCrs)service).SupportedCrs[0];
            }
        }

        SpatialReference targetSref = E.Standard.Api.App.ApiGlobals.SRefStore.SpatialReferences.ById(srefId),
                         calcSref = null;

        int calcCrsId = int.Parse(controller.Request.FormOrQuery("calc_srs").OrTake("0"));
        if (calcCrsId != 0)
        {
            calcSref = ApiGlobals.SRefStore.SpatialReferences.ById(calcCrsId);
        }

        var bridge = _bridge.CreateInstance(ui);
        var sourceFeatures = await bridge.QueryLayerAsync(
            sourceServiceId,
            sourceQuery.LayerId,
            sourceLayer.IdFieldName + " in (" + sourceOids + ")",
            E.Standard.WebMapping.Core.Api.Bridge.QueryFields.Shape,
            calcSref);

        Polygon mergedBufferPolygon = null;

        using (var cts = _cancellationToken.CreateTimeoutCancellationToken())
        {
            try
            {
                List<Polygon> bufferPolygons = new List<Polygon>();

                foreach (var feature in sourceFeatures)
                {
                    if (feature.Shape != null)
                    {
                        if (bufferDistance < 0 && !(feature.Shape is Polygon))
                        {
                            throw new Exception($"Negative buffer distance {bufferDistance} only allowed with polygon source features");
                        }

                        //
                        // Bei negativen bufferDistance zuerst mergen und dann erst buffern
                        // => sonst gibt es lauter keine einzelflächen pro zb Grundstück
                        //
                        var bufferPolygon = feature.Shape?.CalcBuffer(Math.Max(0, bufferDistance), cts);
                        if (bufferPolygon != null)
                        {
                            bufferPolygons.Add(bufferPolygon);
                        }
                    }
                }

                mergedBufferPolygon = SpatialAlgorithms.FastMergePolygon(bufferPolygons, cts);

                if (bufferDistance < 0)
                {
                    mergedBufferPolygon = mergedBufferPolygon?.CalcBuffer(bufferDistance, cts);
                }

                if (mergedBufferPolygon == null || mergedBufferPolygon.Circumference == 0.0)
                {
                    throw new Exception("No source buffer polygon detected");
                }
            }
            catch (CancellationException)
            {
                throw new Exception("Die Pufferoperation ist zu komplex und kann in der vorgegeben Zeit nicht durchführt werden...");
            }
        }

        #region VisFilters

        string visFilterClause = LayerVisFilterClause(httpRequest, service, layer, ui);

        #endregion

        var bufferFilter = new E.Standard.WebMapping.Core.Api.Bridge.ApiSpatialFilter();
        bufferFilter.QueryShape = mergedBufferPolygon;
        bufferFilter.FilterSpatialReference = calcSref;
        bufferFilter.FeatureSpatialReference = targetSref;

        var engine = new QueryEngine();
        FeatureCollection queryFeatures = await engine.PerformAsync(_requestContext, query, bufferFilter, appendFilterClause: visFilterClause) ? engine.Features : new FeatureCollection();

        #region Try Order Features (Original first)

        try
        {
            if (sourceServiceId.Equals(serviceId) && sourceQueryId.Equals(queryId))
            {
                var ids = new List<long>(sourceOids.Split(',').Select(id => long.Parse(id)));

                Envelope sourceEnvelope = null;
                foreach (var feature in queryFeatures.Where(f => ids.Contains(f.Oid)))
                {
                    //feature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("_buffersource", "true"));

                    if (feature.Shape != null)
                    {
                        if (sourceEnvelope == null)
                        {
                            sourceEnvelope = new Envelope(feature.Shape.ShapeEnvelope);
                        }
                        else
                        {
                            sourceEnvelope.Union(feature.Shape.ShapeEnvelope);
                        }
                    }
                }

                if (sourceEnvelope != null)
                {
                    queryFeatures.OrderByPointDistance(sourceEnvelope.CenterPoint);
                }

                foreach (var feature in queryFeatures)
                {
                    if (!ids.Contains(feature.Oid))
                    {
                        ids.Add(feature.Oid);
                    }
                }

                queryFeatures.OrderByIds(ids);
            }
        }
        catch { }

        #endregion

        FeatureCollection returnFeatures = await _restHelper.PrepareFeatureCollection(queryFeatures, query, targetSref, ui, null, QueryGeometryType.Simple, renderFields);

        if (json)
        {
            string bufferId = null;

            if (calcSref != null)
            {
                using (GeometricTransformerPro transformer = new GeometricTransformerPro(calcSref, ApiGlobals.SRefStore.SpatialReferences.ById(4326)))
                {
                    transformer.Transform(mergedBufferPolygon);
                }

                var bufferJsonString = mergedBufferPolygon.ToGeoJson(new E.Standard.WebMapping.Core.Attribute[]{
                    new E.Standard.WebMapping.Core.Attribute("stroke","#000"),
                    new E.Standard.WebMapping.Core.Attribute("stroke-opacity",".5"),
                    new E.Standard.WebMapping.Core.Attribute("fill","#aaa"),
                    new E.Standard.WebMapping.Core.Attribute("fill-opacity",".2"),
                });

                bufferId = $"buffer_{Guid.NewGuid().ToString("N").ToLower()}";
                await System.IO.File.WriteAllTextAsync($"{_urlHelper.OutputPath()}/{bufferId}.json", bufferJsonString);
            }

            return await controller.JsonObject(watch.Apply<FeaturesDTO>(new FeaturesDTO(returnFeatures, FeaturesDTO.Meta.Tool.Buffer, select: true, customSelection: bufferId, sortColumns: query.AutoSortFields)));
        }

        return await controller.ApiObject(new FeaturesDTO(returnFeatures, FeaturesDTO.Meta.Tool.Buffer, sortColumns: query.AutoSortFields));
    }

    async public Task<IActionResult> PerformQueryDomains(ApiBaseController controller, string serviceId, string queryId, CmsDocument.UserIdentification ui)
    {
        var service = await _cache.GetOriginalService(serviceId, ui, _urlHelper);
        if (service == null)
        {
            throw new Exception("Unknown service");
        }

        var query = await this.GetQuery(serviceId, queryId, ui);
        if (query == null)
        {
            throw new Exception("Unknown query");
        }

        var layer = service.Layers.FindByLayerId(query.LayerId);
        if (layer == null)
        {
            throw new Exception("Unknwon layer");
        }

        List<DomainCodedValuesDTO> domainCodesValues = new List<DomainCodedValuesDTO>();
        if (layer is ILayerFieldDomains)
        {
            foreach (var field in layer.Fields)
            {
                var codedValues = ((ILayerFieldDomains)layer).CodedValues(field.Name);
                if (codedValues == null || codedValues.Count() == 0)
                {
                    continue;
                }

                List<DomainCodedValuesDTO.CodedValue> domains = new List<DomainCodedValuesDTO.CodedValue>();
                foreach (var keyValue in codedValues)
                {
                    domains.Add(new DomainCodedValuesDTO.CodedValue()
                    {
                        Code = keyValue.Key,
                        Value = keyValue.Value
                    });
                }
                domainCodesValues.Add(new DomainCodedValuesDTO()
                {
                    Fieldname = field.Name,
                    CodedValues = domains
                });
            }
        }

        return await controller.JsonObject(domainCodesValues);
    }

    async public Task<IActionResult> PerformAutocompleteQuery(ApiBaseController controller, string serviceId, string queryId, System.Collections.Specialized.NameValueCollection form, CmsDocument.UserIdentification ui)
    {
        try
        {
            var query = await this.GetQuery(serviceId, queryId, ui);
            if (query == null)
            {
                throw new Exception("unknown query");
            }

            var item = query.GetItem(form["_item"]);
            if (item == null)
            {
                throw new Exception("unknown item");
            }

            if (item.autocomplete == false || string.IsNullOrEmpty(item.Lookup_ConnectionString))
            {
                throw new Exception("no autocomplete item");
            }

            #region Parse Sql Clause

            string sql = item.Lookup_SqlClause ?? String.Empty;
            var sqlContainsCurrentItem =
                sql.Contains($"[{item.id}]") || sql.Contains($"{{{{{item.id}}}}}");

            foreach (var searchItem in query.items)
            {
                string val = form[searchItem.id] != null ? form[searchItem.id] : String.Empty;

                // Check for SQL Injektion
                try
                {
                    val = SqlInjection.ParsePro(val, ignoreCharacters: searchItem.SqlInjectionWhiteList);
                }
                catch (InputValidationException)
                {
                    throw new InputValidationException("Die Eingabe für " + searchItem.name + " enthält ungültige Zeichen:" + val);
                }

                sql = sql
                    .Replace($"[{searchItem.id}]", val)  // old placeholder syntax
                    .Replace($"{{{{{searchItem.id}}}}}", val);
            }

            sql = CmsHlp.ReplaceFilterKeys(_httpRequestContext.OriginalUrlParameters, ui, sql, startingBracket: "{{", endingBracket: "}}");
            sql = sql.ParseStatementPreCompilerDirectives(form, StatementType.Sql).ToSingleLineStatement();

            #endregion

            #region Service (Distinct) Query

            if (item.Lookup_ConnectionString == "#")
            {
                var service = query.Service;
                var layer = service.Layers.FindByLayerId(query.LayerId);

                if (layer is ILayerDistinctQuery && item.Fields.Length == 1)
                {
                    var values = await ((ILayerDistinctQuery)layer)
                                .QueryDistinctValues(_requestContext, item.Fields[0], sql, item.Fields[0],
                                                     featureLimit: 250);

                    if (!sqlContainsCurrentItem)
                    {
                        values = values.Where(v => v.StartsWith(form[item.id], StringComparison.OrdinalIgnoreCase));
                    }

                    return await controller.JsonObject(values.ToArray());
                }
                else
                {
                    throw new Exception("Can't distinct");
                }
            }

            #endregion

            #region Database Query 

            if (String.IsNullOrEmpty(sql))
            {
                throw new Exception("No Sql Statement defined");
            }

            using (var conn = new E.Standard.DbConnector.DBConnection())
            {
                conn.OleDbConnectionMDB = item.Lookup_ConnectionString;

                System.Data.DataTable tab = conn.Select(sql);
                List<string> ret = new List<string>();

                int counter = 0, max = 250;

                if (tab != null)
                {
                    foreach (System.Data.DataRow row in tab.Rows)
                    {
                        if (row[0] == null)
                        {
                            continue;
                        }

                        if (counter > max)
                        {
                            ret.Add("...");
                            break;
                        }

                        ret.Add(row[0].ToString());

                        counter++;
                    }
                }
                else if (!String.IsNullOrEmpty(conn.errorMessage))
                {
                    try
                    {
                        _requestContext.GetRequiredService<IExceptionLogger>()
                            .LogException(ui, "webgis", serviceId, "autocomplete", new Exception(conn.errorMessage));
                    }
                    catch { }
                }

                if (controller.Request.Query["_autocomplete_item_style"] == "label")
                {
                    return await controller.JsonObject(ret.Select(m => new { label = m }).ToArray());
                }
                return await controller.JsonObject(ret.ToArray());
            }

            #endregion

            //return controller.JsonViewSuccess(true);
        }
        catch (InputValidationException siv)
        {
            return await controller.JsonObject(new string[] { siv.Message }.Select(m => new { label = m }).ToArray());
        }
    }


    #endregion

    #region Helper

    private string LayerVisFilterClause(HttpRequest httpRequest, IMapService service, ILayer layer, CmsDocument.UserIdentification ui)
    {
        var visFilters = httpRequest.VisFilterDefinitionsFromParameters();
        string visFilterClause = String.Empty;

        var serviceFilters = _cache.GetAllVisFilters(service.Url, ui);

        if (visFilters != null || serviceFilters.HasLockedFilters)
        {
            if (serviceFilters != null && serviceFilters.filters != null)
            {
                if (visFilters != null)
                {
                    #region Normale Filter

                    foreach (var filter in visFilters)
                    {
                        if (!String.IsNullOrWhiteSpace(filter.ServiceId) && filter.ServiceId != service.Url)
                        {
                            continue;
                        }

                        var serviceFilter = serviceFilters.filters.Where(f => f.Id == filter.Id && f.FilterType != VisFilterType.locked).FirstOrDefault();
                        if (serviceFilter != null && serviceFilter.LayerNamesString != null && serviceFilter.LayerNamesString.Split(';').Contains(layer.Name))
                        {
                            string filterClause = serviceFilter.Filter;
                            foreach (var arg in filter.Arguments)
                            {
                                filterClause = filterClause.Replace("[" + arg.Name + "]", arg.Value);
                            }

                            if (String.IsNullOrWhiteSpace(filterClause))
                            {
                                continue;
                            }

                            //if (visFilterClause.Length > 0)
                            //{
                            //    visFilterClause.Append(" AND ");
                            //}

                            //visFilterClause.Append(filterClause);

                            visFilterClause = visFilterClause.AppendWhereClause(filterClause);
                        }
                    }

                    #endregion
                }

                #region Locked Vis Filters

                string lockedVisFilterClause = String.Empty;

                foreach (var lockedFilter in serviceFilters.LockedFilters.Where(f => f.LayerNamesString.Split(';').Contains(layer.Name)))
                {
                    lockedVisFilterClause = lockedVisFilterClause.AppendWhereClause(CmsHlp.ReplaceFilterKeys(_httpRequestContext.OriginalUrlParameters, ui, lockedFilter.Filter));
                }

                if (!String.IsNullOrEmpty(lockedVisFilterClause))
                {
                    visFilterClause = visFilterClause.AppendWhereClause(lockedVisFilterClause);
                }

                #endregion
            }
        }

        return visFilterClause.ToString();
    }

    async private Task<QueryDTO> GetQuery(string serviceId, string queryId, CmsDocument.UserIdentification ui)
    {
        if (_mapServiceInitializer.IsCustomService(serviceId))
        {
            var customService = await _mapServiceInitializer.GetCustomServiceByUrlAsync(serviceId, _mapServiceInitializer.Map(_requestContext, ui), ui, _httpContextAccessor?.HttpContext?.Request?.FormCollection());
            if (customService is IDynamicService && ((IDynamicService)customService).CreateQueriesDynamic != ServiceDynamicQueries.Manually)
            {
                return ((IDynamicService)customService).GetDynamicQuery(queryId);
            }
        }

        var query = await _cache.GetQuery(serviceId, queryId, ui, urlHelper: _urlHelper);

        return query;
    }

    #endregion
}
