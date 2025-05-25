using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Sorting;
using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Models;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Geometry.Extensions;
using E.Standard.WebMapping.GeoServices.Tiling;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.Rest;

public class RestHelperService
{
    private readonly UrlHelperService _urlHelper;
    private readonly UploadFilesService _upload;
    private readonly CacheService _cache;
    private readonly IRequestContext _requestContext;
    private readonly IHttpContextAccessor _contextAccessor;

    public RestHelperService(UrlHelperService urlHelper,
                             UploadFilesService upload,
                             CacheService cache,
                             IRequestContext requestContext,
                             IHttpContextAccessor httpContextAccessor)
    {
        _urlHelper = urlHelper;
        _upload = upload;
        _cache = cache;
        _requestContext = requestContext;
        _contextAccessor = httpContextAccessor;
    }

    #region Services

    public ServiceInfoDTO CreateServiceInfo(
                ApiBaseController controller, 
                IMapService service, 
                CmsDocument.UserIdentification ui)
    {
        string id = service.Url;

        ServiceInfoDTO info = null;
        ServiceType serviceType = ServiceDTO.GetServiceType(service);

        if (service is TileService)
        {
            #region Tile Service

            TileService tileService = (TileService)service;
            TileGrid tileGrid = tileService.TileGrid;
            if (tileGrid == null)
            {
                throw new Exception("Service is not a valid tileservice");
            }

            Envelope extent = tileGrid.Extent;

            string tileUrl = String.Empty;
            string[] domains = new string[0];

            if (tileGrid.Orientation == E.Standard.WebGIS.CMS.TileGridOrientation.LowerLeft)
            {
                tileUrl = $"{_urlHelper.AppRootUrl(HttpSchema.Default)}/rest/services/{service.Url}/tile?x={{x}}&y={{y}}&z={{z}}";
            }
            else
            {
                if (controller.ClientJavascriptVersionOrLater(new System.Version(3, 0, 0)))
                {
                    var tileUrlResult = tileService.ImageUrlPro(_requestContext, null);
                    tileUrl = tileUrlResult.tileUrl;
                    domains = tileUrlResult.domains ?? new string[0];
                }
                else
                {
                    tileUrl = tileService.ImageUrl(_requestContext, null);
                }

                tileUrl = tileUrl.Replace("[LEVEL]", "{z}").Replace("[COL]", "{x}").Replace("[ROW]", "{y}");
                tileUrl = tileUrl.Replace("[LEVEL_PAD2]", "{z_}").Replace("[COL_HEX_PAD8]", "{x_}").Replace("[ROW_HEX_PAD8]", "{y_}");
            }

            if (tileGrid.TileSizeX != tileGrid.TileSizeY)
            {
                throw new Exception("TileSizeX and TileSizeY must be ident for API Services: " + service.Name);
            }

            info = new ServiceInfoDTO()
            {
                name = service.Name,
                id = service.Url,
                type = serviceType.ToString().ToLower(),
                opacity = service.InitialOpacity,
                opacity_factor = service.OpacityFactor,
                properties = new ServiceInfoDTO.TileProperties()
                {
                    extent = new double[] { extent.MinX, extent.MinY, extent.MaxX, extent.MaxY },
                    origin = new double[] { tileGrid.Origin.X, tileGrid.Origin.Y },
                    tilesize = tileGrid.TileSizeX,
                    resolutions = tileGrid.GridResolutions,
                    tileurl = tileUrl,
                    domains = domains,
                    basemap_type = tileService.BasemapType.ToString().ToLower(),
                    hide_beyond_maxlevel = tileService.HideBeyondMaxLevel
                }
            };

            #endregion
        }
        else if (service is GeneralVectorTileService vtcService)
        {
            info = new ServiceInfoDTO()
            {
                name = vtcService.Name,
                id = vtcService.Url,
                type = serviceType.ToString().ToLower(),
                opacity = vtcService.InitialOpacity,
                properties = new ServiceInfoDTO.VectorTileProperties()
                {
                    basemap_type = vtcService.BasemapType.ToString().ToLower(),
                    vtc_styles_url = vtcService.Server,
                    fallback = vtcService.FallbackService,
                    preview_url = vtcService.PreviewImageUrl
                }
            };
        }
        else if (service is IStaticOverlayService)
        {
            #region StaticOverlayService

            var overlayService = (IStaticOverlayService)service;

            info = new ServiceInfoDTO()
            {
                name = service.Name,
                id = service.Url,
                type = serviceType.ToString().ToLower(),
                opacity = service.InitialOpacity,
                opacity_factor = service.OpacityFactor,

                OverlayUrl = overlayService.OverlayImageUrl,
                WidthHeightRatio = overlayService.WidthHeightRatio,
                AffinePoints = new[] {
                    ServiceInfoDTO.LngLat.Create(overlayService.TopLeftLngLat),
                    ServiceInfoDTO.LngLat.Create(overlayService.TopRightLngLat),
                    ServiceInfoDTO.LngLat.Create(overlayService.BottomLeftLngLat)
                }
            };

            #endregion
        }
        else
        {
            #region General Service

            info = new ServiceInfoDTO()
            {
                name = service.Name,
                id = service.Url,
                type = serviceType.ToString().ToLower(),
                opacity = service.InitialOpacity,
                opacity_factor = service.OpacityFactor,
            };

            if (service.IsBaseMap)
            {
                info.properties = new ServiceInfoDTO.ServiceInfoProperties()
                {
                    basemap_type = service.BasemapType.ToString().ToLower(),
                    preview_url = service.BasemapPreviewImage
                };
            }

            #endregion
        }

        if (service is IMapServiceSupportedCrs)
        {
            info.supportedCrs = ((IMapServiceSupportedCrs)service).SupportedCrs;
        }
        info.isbasemap = service.IsBaseMap;
        info.show_in_toc = service.ShowInToc;
        info.HasVisibleLockedLayers = false;

        List<ServiceInfoDTO.LayerInfo> layers = new List<ServiceInfoDTO.LayerInfo>();
        var serviceLayerProperties = _cache.GetServiceLayerProperties(id);

        var unauthorizedLayerIds = _cache.GetUnauthorizedLayerIds(service, ui);
        string cmsTocName = _cache.ServiceCmsTocName(id);

        info.ImageServiceType = service.CustomImageServiceType()?.ToString().ToLower();
        var service2 = service as IMapService2;

        foreach (var layer in service.Layers)
        {
            string name = layer.Name, tocName = layer.Name;
            bool? hiddenInToc = null;
            bool layerVisibility = layer.Visible;

            // nicht sichtbar für den User?
            if (unauthorizedLayerIds.Contains(layer.ID) || !service2.CmsThemeIncludedForStrictUse(layer))
            {
                continue;
            }

            var layerProps = serviceLayerProperties?.Where(l => l.Id == layer.ID).FirstOrDefault(); //Cache.GetLayerProperties(id, layer.ID);

            if (layerProps != null)
            {
                if (layerProps.Locked == true)
                {
                    if (layerProps.Visible == true)
                    {
                        info.HasVisibleLockedLayers = true;
                    }
                    continue;
                }

                if (!String.IsNullOrWhiteSpace(layerProps.Name))
                {
                    tocName = layerProps.Name;
                }
                if (!String.IsNullOrWhiteSpace(layerProps.Aliasname))
                {
                    tocName = (tocName.Contains("\\") ? tocName.Substring(0, tocName.LastIndexOf("\\") + 1) : "") + layerProps.Aliasname;
                }
            }
            else
            {
                if (_cache.ServiceHasCustomCmsToc(id))
                {
                    // Wichtig: Falls ein TOC im CMS definiert wurde:
                    // ##############################################
                    // * Layer im TOC nicht anzeigen
                    // * Layer unsichtbar schalten
                    hiddenInToc = _cache.ServiceHasCustomCmsToc(id);
                    layerVisibility = false;
                }
            }

            var layerInfo = new ServiceInfoDTO.LayerInfo()
            {
                id = layer.ID,
                name = name,
                type = layer.Type.ToString().ToLower(),
                minscale = layer.MinScale,
                maxscale = layer.MaxScale,
                idfieldname = layer.IdFieldName,
                visible = layerProps != null ? layerProps.Visible : layerVisibility,
                legend = layerProps != null ? layerProps.ShowInLegend : true,
                tocname = tocName != name ? tocName : null,
                tochidden = hiddenInToc,
                undropdownable_groupname = layerProps != null && !String.IsNullOrWhiteSpace(layerProps.UnDropdownableParentName) ? layerProps.UnDropdownableParentName : null,
                description = !String.IsNullOrEmpty(layerProps?.Description) ? layerProps.Description : layer.Description,
                IsTocLayer = layerProps != null ? layerProps.IsTocLayer : (String.IsNullOrEmpty(cmsTocName) || cmsTocName == "default"),  // Wenn TOC parametriert wurde => IsTocLayer = true
                metadata = layerProps != null ? layerProps.Metadata : null,
                MetadataButtonStyle = layerProps != null ? layerProps.MetadataButtonStyle : MetadataButtonStyle.i_button,
                MetadataTarget = layerProps != null ? layerProps.MetadataTarget : BrowserWindowTarget2.tab,
                MetadataTitle = layerProps != null ? layerProps.MetadataTitle : null,
            };

            if (layerProps != null)
            {
                layerInfo.OgcId = layerProps.OgcId;
                layerInfo.OgcGroupId = layerProps.OgcGroupId;
                layerInfo.OgcGroupTitle = layerProps.OgcGroupTitle;
            }

            layers.Add(layerInfo);
        }

        info.layers = layers.ToArray();
        if (serviceLayerProperties != null && serviceLayerProperties.Count() > 0)
        {
            #region Reorder layers => damit TOC richtig dargestellt wird

            var layerInfos = new List<ServiceInfoDTO.LayerInfo>();

            foreach (var layerProperties in serviceLayerProperties)
            {
                var layerInfo = info.layers.Where(l => l.id == layerProperties.Id).FirstOrDefault();
                if (layerInfo != null)
                {
                    layerInfos.Add(layerInfo);
                }
            }

            // Rest Layer (nicht im TOC)
            foreach (var layerInfo in info.layers)
            {
                if (!layerInfos.Contains(layerInfo))
                {
                    layerInfos.Add(layerInfo);
                }
            }

            info.layers = layerInfos.ToArray();

            #endregion
        }

        //Cache.AppendTocProperties(info, ui);   // sollte schon bei Cache Init ausgelesen werden und in den LayerProperties stehen

        info.ContainerDisplayname = _cache.CmsItemDisplayName(id, ui);

        #region Presentations

        if (service.AllowPresentations())
        {
            if (service.UseDynamicPresentations())
            {
                var originalService = _cache.GetOriginalService(id, ui, _urlHelper).Result;
                info.presentations = (service as IDynamicService).DynamicPresentations(originalService, layers);
            }
            else
            {
                info.presentations = _cache.GetPresentations(service, _urlHelper.GetCustomGdiScheme(), ui).presentations;
                if (info.presentations != null && info.presentations.Length == 0)
                {
                    info.presentations = null;
                }
            }
        }

        #endregion

        #region Queries

        if (service.UseDynamicQueries())
        {
            info.queries = (service as IDynamicService).GetDynamicQueries();
        }
        else
        {
            info.queries = _cache.GetQueries(id, ui).queries;
            if (info.queries != null && info.queries.Length == 0)
            {
                info.queries = null;
            }
        }

        #endregion

        info.chainagethemes = _cache.GetServiceChainageThemes(id, ui)?.ToArray();
        if (info.chainagethemes != null && info.chainagethemes.Count() == 0)
        {
            info.chainagethemes = null;
        }

        info.editthemes = _cache.GetEditThemes(id, ui).editthemes?.Where(e => e.Visible).ToArray();
        if (info.editthemes != null && info.editthemes.Length == 0)
        {
            info.editthemes = null;
        }

        List<VisFilterDTO> filters = new List<VisFilterDTO>();
        filters.AddRange(_cache.GetVisFilters(id, ui, E.Standard.WebGIS.CMS.VisFilterType.visible)
                        .filters.Select(f => { f.Visible = true; return f; }));
        filters.AddRange(_cache.GetVisFilters(id, ui, E.Standard.WebGIS.CMS.VisFilterType.invisible)
                        .filters.Select(f => { f.Visible = false; return f; }));

        info.filters = filters.Count > 0 ? filters.ToArray() : null;

        info.LockedFilters = _cache.GetVisFilters(id, ui, E.Standard.WebGIS.CMS.VisFilterType.locked).filters;
        //info.InvisibleFilters = Cache.GetVisFilters(id, ui, WebGIS.CMS.VisFilterType.invisible).filters;

        info.Labeling = _cache.GetLabeling(id, ui).labelings;
        if (info.Labeling != null && info.Labeling.Length == 0)
        {
            info.Labeling = null;
        }

        info.Snapping = _cache.GetSnapSchemes(id, ui);
        if (info.Snapping != null && info.Snapping.Count() == 0)
        {
            info.Snapping = null;
        }

        if (service is IMapServiceLegend mapServiceLegend)
        {
            info.HasLegend = mapServiceLegend.ShowServiceLegendInMap;
        }

        if (service is IMapServiceMetadataInfo mapServiceMetadata)
        {
            info.MetadataLink = mapServiceMetadata.MetadataLink;
            info.copyright = mapServiceMetadata.CopyrightInfoId;
        }

        if (service is IMapServiceDescription mapServiceDecription)
        {
            info.CopyrightText = mapServiceDecription.CopyrightText;
            info.ServiceDescription = mapServiceDecription.ServiceDescription;

            if (info.CopyrightText == "...")
            {
                info.CopyrightText = String.Empty;                // "..." wo kommt das her? Default aus AGS?
            }

            if (info.ServiceDescription == "...")
            {
                info.ServiceDescription = String.Empty;      // sollte nicht übernommen werden.
            }
        }

        #region Extent

        if (service is IExportableOgcService && ((IExportableOgcService)service).OgcEnvelope != null)
        {
            var extent = new Envelope(((IExportableOgcService)service).OgcEnvelope);

            info.extent = new double[]{
                extent.MinX,
                extent.MinY,
                extent.MaxX,
                extent.MaxY
            };
        }

        #endregion

        #region Exceptions

        if (service is IMapServiceInitialException && !String.IsNullOrEmpty(((IMapServiceInitialException)service).InitialException?.ErrorMessage))
        {
            info.InitialException = ((IMapServiceInitialException)service).InitialException?.ErrorMessage;
        }

        #endregion

        return info;

    }

    #endregion

    #region FeatureCollections

    async public Task<FeatureCollection> PrepareFeatureCollection(
                FeatureCollection queryFeatures,
                QueryDTO query,
                SpatialReference sRef,
                CmsDocument.UserIdentification ui,
                ApiToolEventArguments.ApiToolEventClick clickEvent = null,
                QueryGeometryType geometryType = QueryGeometryType.Simple,
                bool renderFields = true,
                bool select = false,
                int targetSRefId = 4326,
                bool appendHoverShape = true)
    {
        FeatureCollection returnFeatures = new FeatureCollection();

        if (queryFeatures != null)
        {
            int srefId = sRef != null ? sRef.Id : 4326;
            var service = query?.Service;
            var layer = query != null ? service?.Layers?.FindById(query.LayerId) : null;

            Point clickPoint = null;
            if (clickEvent != null)
            {
                clickPoint = new Point(clickEvent.Longitude, clickEvent.Latitude);
            }

            if (renderFields && query != null)
            {
                await query.InitFieldRendering(_requestContext.Http);
            }

            #region Query/Table Fields

            var tableFields = query?.Fields;

            // "*" => alle möglichen Felder anzeigen. Macht sinn wenn Dienst keine Schema hat (WMS und GetFetureInfo mit application/json)
            if (tableFields != null
                && tableFields.Length == 1
                && tableFields[0] is TableFieldData tableFieldData
                && tableFieldData.FieldName == "*")
            {
                List<TableFieldDTO> allTableFields = new List<TableFieldDTO>();
                foreach (var queryFeature in queryFeatures)
                {
                    if (queryFeature.Attributes == null)
                    {
                        continue;
                    }

                    foreach (var attribute in queryFeature.Attributes)
                    {
                        if (allTableFields.Where(f => f.ColumnName == attribute.Alias).Count() == 0)
                        {
                            allTableFields.Add(new TableFieldData()
                            {
                                ColumnName = attribute.Alias,
                                FieldName = attribute.Name,
                                Visible = true,
                                RawHtml = ((TableFieldData)tableFields[0]).RawHtml
                            });
                        }
                    }
                }

                tableFields = allTableFields.ToArray();
            }

            #endregion

            using (GeometricTransformer transformer = new GeometricTransformer())
            {
                if (srefId != targetSRefId)
                {
                    transformer.FromSpatialReference(sRef.Proj4, !sRef.IsProjective);
                    var targetSRef = E.Standard.Api.App.ApiGlobals.SRefStore.SpatialReferences.ById(targetSRefId);
                    transformer.ToSpatialReference(targetSRef.Proj4, !targetSRef.IsProjective);
                }

                foreach (E.Standard.WebMapping.Core.Feature queryFeature in queryFeatures)
                {
                    E.Standard.WebMapping.Core.Feature returnFeature = new E.Standard.WebMapping.Core.Feature();

                    var shape = queryFeature.Shape;
                    if (shape != null && transformer.CanTransform)
                    {
                        transformer.Transform2D(shape);
                    }

                    if (geometryType == QueryGeometryType.Simple)
                    {
                        #region Shape 2 Point

                        if (shape != null)
                        {
                            returnFeature.ZoomEnvelope = shape.ShapeEnvelope;

                            if (appendHoverShape && targetSRefId == Epsg.WGS84
                                && shape.CountPoints() <= 1000)
                            {
                                returnFeature.HoverShape = shape;
                            }

                            Point[] points = null;
                            try
                            {
                                if (shape is Point)
                                {
                                    points = new Point[] { (Point)shape };
                                }
                                else
                                {
                                    points = SpatialAlgorithms.DeterminePointsOnShape(null, shape, 10, !sRef.IsProjective, clickPoint);
                                }
                            }
                            catch { /* TODO: Warnung ausgeben?! */ }

                            if (points != null && points.Length > 0)
                            {
                                returnFeature.Shape = SpatialAlgorithms.ClosestPointToHotspot(points, shape.ShapeEnvelope.CenterPoint);
                            }
                            else
                            {
                                returnFeature.Shape = null;
                            }
                        }

                        #endregion
                    }
                    else
                    {
                        returnFeature.Shape = shape;
                    }

                    if (service != null && query != null)
                    {
                        string featureOid;

                        if (queryFeature.Oid < 0 && String.IsNullOrEmpty(layer?.IdFieldName))
                        {
                            featureOid = Guid.NewGuid().ToString("N").ToLower();  // Eindeutige Id auch für Layer ohne ObjectId, damit die Ergebnisse dann im Viewer auch einzeln ausgewählt werden können.
                        }
                        else
                        {
                            featureOid = queryFeature.Oid.ToString();
                        }
                        returnFeature.GlobalOid = $"{service.Url}:{query.id}:{featureOid}";

                        if (query.draggable)
                        {
                            returnFeature.DragAttributes.Add(new E.Standard.WebMapping.Core.Attribute("_oid", queryFeature.Oid.ToString()));
                            returnFeature.DragAttributes.Add(new E.Standard.WebMapping.Core.Attribute("_global_oid", queryFeature.GlobalOid?.ToString()));
                        }
                        foreach (var field in tableFields)
                        {
                            if (renderFields)
                            {
                                string val = field.RenderField(queryFeature, _contextAccessor.HttpContext?.Request?.HeadersCollection());

                                returnFeature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute(
                                    field.ColumnName,
                                    val));

                                if (query.draggable && field is TableFieldData && returnFeature.Attributes[((TableFieldData)field).FieldName] == null)
                                {
                                    returnFeature.DragAttributes.Add(new E.Standard.WebMapping.Core.Attribute(((TableFieldData)field).FieldName, queryFeature[((TableFieldData)field).FieldName]));
                                }
                            }
                            else if (field is TableFieldData && returnFeature.Attributes[((TableFieldData)field).FieldName] == null)
                            {
                                string val = queryFeature[((TableFieldData)field).FieldName];
                                returnFeature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute(((TableFieldData)field).FieldName, val));
                            }
                        }

                        #region Fulltext (Vorschautext)

                        StringBuilder fulltext = new StringBuilder();

                        if (!String.IsNullOrEmpty(query.PreviewTextTemplate))
                        {
                            fulltext.Append(E.Standard.WebGIS.CMS.Globals.SolveExpression(queryFeature, query.PreviewTextTemplate));
                        }
                        else if (query.items != null)
                        {
                            foreach (var item in query.items.Where(i => !i.IgnoreInPreviewText))
                            {
                                if (item.Fields == null)
                                {
                                    continue;
                                }

                                foreach (string field in item.Fields)
                                {
                                    string val = queryFeature[field];
                                    if (!String.IsNullOrEmpty(val))
                                    {
                                        if (fulltext.Length > 0)
                                        {
                                            fulltext.Append("<br>");
                                        }

                                        fulltext.Append(val);
                                    }
                                }
                            }
                        }

                        #region Raw Html (from WMS GetFeatureInfo)

                        var featureFullTextAttribute = queryFeature?.Attributes?.Where(a => a.Name == "_fulltext").FirstOrDefault();
                        if (featureFullTextAttribute != null)
                        {
                            fulltext.Append(featureFullTextAttribute.Value);
                        }

                        #endregion

                        if (fulltext.Length == 0 && renderFields && tableFields?.FirstOrDefault() != null)
                        {
                            var firstField = tableFields.First();
                            fulltext.Append($"{firstField.ColumnName}: {firstField.RenderField(queryFeature, _contextAccessor.HttpContext?.Request?.HeadersCollection())}");
                        }

                        if (fulltext.Length > 0)
                        {
                            var fullTextAttribute = returnFeature?.Attributes?.Where(a => a.Name == "_fulltext").FirstOrDefault();

                            if (fullTextAttribute != null)
                            {
                                fullTextAttribute.Value = fulltext.ToString();
                            }
                            else
                            {
                                returnFeature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("_fulltext", fulltext.ToString()));
                            }
                        }

                        returnFeature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("_title", query.Name));
                        returnFeature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("_zoomscale", query.zoomscale.ToString()));
                        //returnFeature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("_hash", queryFeature.CalcHashCode()));

                        #endregion
                    }
                    else
                    {
                        returnFeature.GlobalOid = String.IsNullOrWhiteSpace(queryFeature.GlobalOid) ?
                            "unknown:unknown:" + queryFeature.Oid :
                            queryFeature.GlobalOid;

                        foreach (var attribute in queryFeature.Attributes)
                        {
                            returnFeature.Attributes.Add(attribute);
                        }
                    }

                    returnFeatures.Add(returnFeature);
                }

                #region 1:n Links

                returnFeatures.Append1toNLinks(queryFeatures, query, renderFields, _contextAccessor.HttpContext?.Request?.HeadersCollection());

                #endregion

                #region Fields

                if (tableFields != null)
                {
                    returnFeatures.TableFieldDefintions = tableFields.Select(f =>
                        new FeatureCollection.TableFieldDefintion()
                        {
                            Name = f.ColumnName,
                            Visible = f.Visible,
                            SortingAlgorithm = f is TableFieldData fieldData && !String.IsNullOrEmpty(fieldData.SortingAlgorithm)
                                                        ? ((TableFieldData)f).SortingAlgorithm
                                                        : null,
                            ImageSize = f is TableFieldImageDTO fieldImage && (fieldImage.ImageWidth > 0 || fieldImage.ImageHeight > 0)
                                                        ? (fieldImage.ImageWidth, fieldImage.ImageHeight)
                                                        : null,
                        });
                }

                #endregion

                #region Attachments

                returnFeatures.HasAttachments =
                    queryFeatures.Layer is IFeatureAttachmentProvider attachmentContainer
                    && attachmentContainer.HasAttachments
                    ? _cache.IsQueryShowAttachmentsAllowed(query.Service.Url, query.id, ui)
                    : false;

                #endregion

                #region Warnings/Information

                if (queryFeatures.Warnings != null)
                {
                    foreach (string warning in queryFeatures.Warnings.Where(w => !String.IsNullOrWhiteSpace(w)).Distinct())
                    {
                        returnFeatures.AddWarning(warning);
                    }
                }

                if (queryFeatures.Informations != null)
                {
                    foreach (string information in queryFeatures.Informations.Where(i => !String.IsNullOrWhiteSpace(i)).Distinct())
                    {
                        returnFeatures.AddInformation(information);
                    }
                }


                #endregion
            }

            if (clickEvent?.SketchInfo != null && clickEvent.SketchInfo.GeometryType == "circle" && clickEvent.SketchInfo.Center != null)
            {
                returnFeatures.Sort(new FeatureSortByDistance(new Point(clickEvent.SketchInfo.Center.Lng, clickEvent.SketchInfo.Center.Lat), targetSRefId == 4326, true));
            }
            if (clickPoint != null /* && returnFeatures.Where(m => m.Shape != null).Count() == returnFeatures.Count*/)
            {
                returnFeatures.Sort(new FeatureSortByDistance(clickPoint, targetSRefId == 4326, false));
            }
        }

        return returnFeatures;
    }


    public IEnumerable<string> PrepareFeatureGlobalsOids(IEnumerable<int> oids,
                                                         IEnumerable<QueryDTO> queries)
    {
        List<string> globalOids = new List<string>();

        foreach (var query in queries)
        {
            if (query?.Service == null)
            {
                continue;
            }

            foreach (var oid in oids)
            {
                globalOids.Add($"{query.Service.Url}:{query.id}:{oid}");
            }
        }

        return globalOids;
    }

    #endregion

    #region Helper



    #endregion
}
