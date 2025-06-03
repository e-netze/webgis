using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.Exceptions;
using E.Standard.Api.App.Extensions;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Extensions;
using E.Standard.Extensions.Compare;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Api.App.Services.Cache;

public class CmsCacheItem
{
    public Dictionary<string, AuthObject<IMapService>> _mapServices = new Dictionary<string, AuthObject<IMapService>>();
    public Dictionary<string, ExtentDTO> _extents = new Dictionary<string, ExtentDTO>();
    public Dictionary<string, AuthObject<PresentationDTO>[]> _presentations = new Dictionary<string, AuthObject<PresentationDTO>[]>();
    public Dictionary<string, AuthObject<QueryDTO>[]> _queries = new Dictionary<string, AuthObject<QueryDTO>[]>();
    public Dictionary<string, AuthObject<EditThemeDTO>[]> _editThemes = new Dictionary<string, AuthObject<EditThemeDTO>[]>();
    public Dictionary<string, AuthObject<VisFilterDTO>[]> _visFilters = new Dictionary<string, AuthObject<VisFilterDTO>[]>();
    public Dictionary<string, AuthObject<LabelingDTO>[]> _labeling = new Dictionary<string, AuthObject<LabelingDTO>[]>();
    public Dictionary<string, AuthObject<SnapSchemaDTO>[]> _snapSchemes = new Dictionary<string, AuthObject<SnapSchemaDTO>[]>();
    public Dictionary<string, string> _tocName = new Dictionary<string, string>();
    public Dictionary<string, AuthObject<PrintLayoutDTO>> _printLayouts = new Dictionary<string, AuthObject<PrintLayoutDTO>>();
    public Dictionary<string, List<AuthObject<PrintFormatDTO>>> _printFormats = new Dictionary<string, List<AuthObject<PrintFormatDTO>>>();
    public Dictionary<string, AuthObject<ISearchService>> _searchServices = new Dictionary<string, AuthObject<ISearchService>>();
    public Dictionary<string, object> _toolConfig = new Dictionary<string, object>();
    public List<AuthProperty<bool>> _authBoolProperties = new List<AuthProperty<bool>>();
    public List<AuthProperty<bool>> _authStringProperties = new List<AuthProperty<bool>>();
    public Dictionary<string, AuthObject<ChainageThemeDTO>[]> _chainageThemes = new Dictionary<string, AuthObject<ChainageThemeDTO>[]>();
    public List<CopyrightInfoDTO> _copyright = new List<CopyrightInfoDTO>();
    public Dictionary<string, string> _serviceContainers = new Dictionary<string, string>();

    public Dictionary<string, LayerPropertiesDTO[]> _layerProperties = new Dictionary<string, LayerPropertiesDTO[]>();
    public Dictionary<string, AuthProperty<bool>[]> _layerAuthVisibility = new Dictionary<string, AuthProperty<bool>[]>();

    public object _lockThis = new object();
    public DateTime _intitialTime = new DateTime();

    public bool _isInitialized = false;
    public bool _isCorrupt = false;

    public List<string> _warnings = new List<string>();

    private readonly CacheInstance _cacheInstance;

    public CmsCacheItem(CacheInstance cacheInstance)
    {
        _cacheInstance = cacheInstance;
    }

    public CacheInstance CacheInstance => _cacheInstance;

    public void Init(CacheService cacheService,
                     string cmsName,
                     CmsDocument cms,
                     bool isCustom)
    {
        this.IsCustom = isCustom;
        this.Name = cmsName;

        cmsName = cmsName.Split('$')[0]; // remove branch from name...

        lock (_lockThis)
        {
            if (_isInitialized)
            {
                return;
            }

            _isCorrupt = false;
            ErrorMessage = String.Empty;

            try
            {
                Map map = new Map("init");
                map.Environment.SetUserValue(webgisConst.AppConfigPath, ApiGlobals.AppConfigPath);
                map.Environment.SetUserValue(webgisConst.EtcPath, ApiGlobals.AppEtcPath);

                var cmsHlp = new CmsHlp(cms, map);

                using (var serviceProviderScope = cacheService.ServiceProvider.CreateScope())
                {
                    var mapServiceIntitializer = serviceProviderScope.ServiceProvider
                                                                     .GetRequiredService<MapServiceInitializerService>();

                    if (_mapServices.Count == 0 || _extents.Count == 0 || _isInitialized == false)
                    {
                        _intitialTime = DateTime.UtcNow;

                        Clear();
                        _isInitialized = true;

                        //string cmsGdiSchema = WebgisConfigSettings.AppSettings("cmsgdischema" + (String.IsNullOrWhiteSpace(cmsName) ? "" : "_" + cmsName), Viewer4.AppConfigFileTitle);
                        List<string> cmsGdiSchemes = new List<string>(new string[] { "" });
                        cmsGdiSchemes.AddRange(cmsHlp.GdiCustomSchemeNames());

                        #region Extents

                        var extentUrls = cmsHlp.ExtentUrls();

                        //Console.WriteLine(String.Join(",", extentUrls));

                        foreach (string extentUrl in extentUrls)
                        {
                            try
                            {
                                var extentNode = cmsHlp.ExtentNode(extentUrl);
                                var extent = new ExtentDTO()
                                {
                                    id = String.IsNullOrWhiteSpace(cmsName) ? extentUrl : extentUrl + "@" + cmsName,
                                    epsg = (int)extentNode.Load("projid", 0) > 0 ? (int?)extentNode.Load("projid", 0) : null,
                                    extent = new double[] { 
                                        //double.Parse(extentNode.LoadString("minx").Replace(",","."),webgisCMS.Core.Globals.Nhi),
                                        //double.Parse(extentNode.LoadString("miny").Replace(",","."),webgisCMS.Core.Globals.Nhi),
                                        //double.Parse(extentNode.LoadString("maxx").Replace(",","."),webgisCMS.Core.Globals.Nhi),
                                        //double.Parse(extentNode.LoadString("maxy").Replace(",","."),webgisCMS.Core.Globals.Nhi)
                                        (double)extentNode.Load("minx",0D),
                                        (double)extentNode.Load("miny",0D),
                                        (double)extentNode.Load("maxx",0D),
                                        (double)extentNode.Load("maxy",0D)
                                    }
                                };

                                //using (var transformer = new WebMapping.Core.Geometry.GeometricTransformerPro(ApiGlobals.MapApplication.SpatialReferences, (int)extent.epsg, 4326))
                                //{
                                //    var envelope = new WebMapping.Core.Geometry.Envelope(extent.extent[0], extent.extent[1], extent.extent[2], extent.extent[3]);
                                //    transformer.Transform(envelope);
                                //    extent.bounds = new double[]
                                //    {
                                //        envelope.MinX,
                                //        envelope.MinY,
                                //        envelope.MaxX,
                                //        envelope.MaxY
                                //    };
                                //}

                                if (extent.epsg > 0)
                                {
                                    E.Standard.WebMapping.Core.Geometry.SpatialReference sr = ApiGlobals.SRefStore.SpatialReferences.ById((int)extent.epsg);
                                    if (sr != null)
                                    {
                                        extent.p4 = sr.Proj4;
                                    }
                                }
                                else
                                {
                                    continue;
                                }

                                #region Resolutions/Origin

                                int resIndex = 0;
                                double? res = null;
                                List<double> resList = new List<double>();
                                while ((res = (double?)extentNode.Load("res" + resIndex, null)) != null)
                                {
                                    resList.Add((double)res);
                                    resIndex++;
                                }
                                extent.resolutions = resList.ToArray();
                                if (extent.resolutions == null || extent.resolutions.Length == 0)
                                {
                                    continue;
                                }

                                extent.origin = new double[]{
                                (double)extentNode.Load("originx",0D),
                                (double)extentNode.Load("originy",0D)
                            };

                                #endregion

                                _extents.Add(extent.id, extent);
                            }
                            catch { }
                        }

                        #endregion

                        #region MapServices

                        var serviceUrls = cmsHlp.MapServiceUrls();

                        foreach (string serviceUrl in serviceUrls)
                        {
                            var serviceNode = cmsHlp.ServiceNode(serviceUrl);
                            if (serviceNode == null)
                            {
                                continue;   // Not Supported Service Type (see ServiceNode Method);
                            }

                            _cacheInstance.Log(LogLevel.Information, "Initialize geo-service: {service}", serviceUrl);

                            CmsNode serviceGdiPropierties = cmsHlp.ServiceGdiPropertiesNode(serviceNode);
                            CmsLink cmsLink = CmsLink.Create(cms, null,
                                serviceGdiPropierties != null ? serviceGdiPropierties : serviceNode,   // zB GeoRSS haben keine GDI Properties
                                serviceNode);
                            string tocName = serviceGdiPropierties != null ? serviceGdiPropierties.LoadString("tocname") : "default";

                            IMapService service = mapServiceIntitializer.ServiceInstance(cms, cmsName, cmsLink, ApiGlobals.AppConfigPath);
                            if (service == null)
                            {
                                continue;
                            }
                            //service.Init(map);

                            #region Service Properties

                            #endregion

                            string serviceUrlKey = String.IsNullOrWhiteSpace(cmsName) ? service.Url : $"{service.Url}@{cmsName}";

                            #region Service Presentations

                            if (service.UseDynamicPresentations() == false)
                            {
                                foreach (string cmsGdiScheme in cmsGdiSchemes)
                                {
                                    List<AuthObject<PresentationDTO>> presentations = new List<AuthObject<PresentationDTO>>();

                                    var presentationNodes = cmsHlp.ServicePresentations(service, false);
                                    var cmsPresentations = cmsHlp.GetServiceGdiPresentations(cmsGdiScheme, serviceNode.NodeXPath);

                                    foreach (var presentationNode in presentationNodes)
                                    {
                                        /*
                                        List<string> layerIds = new List<string>();
                                        foreach (string layerName in String.IsNullOrEmpty(presentationNode.LoadString("layers")) ? new string[0] : presentationNode.LoadString("layers").Split(';'))
                                        {
                                            ILayer layer = service.Layers.FindByName(layerName);
                                            if (layer != null)
                                                layerIds.Add(layer.ID);
                                        }
                                         * */
                                        PresentationDTO presentation = new PresentationDTO()
                                        {
                                            id = presentationNode.Url,
                                            name = presentationNode.Name,
                                            layers = String.IsNullOrEmpty(presentationNode.LoadString("layers")) ? null : presentationNode.LoadString("layers").Split(';'),
                                            thumbnail = String.IsNullOrEmpty(presentationNode.LoadString("thumbnail")) ? null : presentationNode.LoadString("thumbnail"),
                                            description = String.IsNullOrEmpty(presentationNode.LoadString("description")) ? null : presentationNode.LoadString("description"),
                                            basemap = (bool)presentationNode.Load("usewithbasemap", false),
                                        };
                                        presentations.Add(new AuthObject<PresentationDTO>(presentation, CmsDocument.GetAuthNodeFast(cms, presentationNode.NodeXPath)));

                                        #region GDI Properties (old cms pattern)

                                        CmsArrayItems items = cmsHlp.CmsDocument.SelectArray(cmsHlp.UserIdentification, presentationNode.NodeXPath, "gdiproperties");
                                        List<PresentationDTO.GdiProperties> gdiProperties = new List<PresentationDTO.GdiProperties>();
                                        foreach (CmsArrayItem item in items)
                                        {
                                            PresentationCheckMode gdi_mode = (PresentationCheckMode)item.Load("gdi_checkmode", (int)PresentationCheckMode.Button);
                                            PresentationGroupStyle gdi_groupstyle = (PresentationGroupStyle)item.Load("gdi_groupstyle", (int)PresentationGroupStyle.Button);
                                            string gdi_groupname = item.LoadString("gdi_groupname");
                                            bool gdi_vis_with_service = (bool)item.Load("gdi_vis_with_service", false);
                                            string[] gdi_vis_with_one_of_services = null;
                                            if (!String.IsNullOrWhiteSpace(item.LoadString("gdi_vis_with_on_of_services")))
                                            {
                                                gdi_vis_with_one_of_services =
                                                item.LoadString("gdi_vis_with_on_of_services")?.Replace(" ", "").Replace(";", ",").Split(',')
                                                    .Select(s => String.IsNullOrWhiteSpace(cmsName) ? s.Trim() : s.Trim() + "@" + cmsName).ToArray();
                                            }
                                            string gdi_containerUrl = item.LoadString("gdi_containerurl");
                                            string gdi_containerName = Globals.GetContainerName(presentationNode.Cms, gdi_containerUrl);

                                            PresentationAffecting gdi_affecting = (PresentationAffecting)item.Load("gdi_affecting", (int)PresentationAffecting.Service);
                                            PresentationCheckMode gdi_style = (PresentationCheckMode)item.Load("gdi_checkmode", (int)PresentationCheckMode.Button);

                                            PresentationDTO.GdiProperties gdiProperty = new PresentationDTO.GdiProperties()
                                            {
                                                container = gdi_containerName,
                                                name = gdi_groupname,
                                                groupstyle = gdi_groupstyle.ToString().ToLower(),
                                                affecting = gdi_affecting.ToString().ToLower(),
                                                style = gdi_style.ToString().ToLower(),
                                                visible_with_service = gdi_vis_with_service,
                                                visible_with_one_of_services = gdi_vis_with_one_of_services
                                            };
                                            gdiProperties.Add(gdiProperty);
                                        }

                                        #endregion

                                        #region GDI Properties (new cms pattern)

                                        if (cmsPresentations != null)
                                        {
                                            foreach (var cmsPresentation in cmsPresentations)
                                            {
                                                if (cmsPresentation.NodeXPath.ToLower() != presentationNode.NodeXPath.ToLower())
                                                {
                                                    continue;
                                                }

                                                string[] gdi_vis_with_on_of_services = null;
                                                if (cmsPresentation.VisibleWithOnOfServices != null && cmsPresentation.VisibleWithOnOfServices.Length > 0)
                                                {
                                                    gdi_vis_with_on_of_services =
                                                        cmsPresentation.VisibleWithOnOfServices
                                                        .Select(s => String.IsNullOrWhiteSpace(cmsName) ? s.Trim() : s.Trim() + "@" + cmsName).ToArray();
                                                }

                                                PresentationDTO.GdiProperties gdiProperty = new PresentationDTO.GdiProperties()
                                                {
                                                    container = cmsPresentation.Container,
                                                    name = cmsPresentation.Group,
                                                    groupstyle = cmsPresentation.GroupStyle.ToString().ToLower(),
                                                    affecting = cmsPresentation.Affecting.ToString().ToLower(),
                                                    style = cmsPresentation.CheckMode.ToString().ToLower(),
                                                    visible = cmsPresentation.Visible,
                                                    client_visibility = cmsPresentation.ClientVisibility != ClientVisibility.Any ? cmsPresentation.ClientVisibility.ToString().ToLower() : null,
                                                    visible_with_service = cmsPresentation.VisibleWithService,
                                                    visible_with_one_of_services = gdi_vis_with_on_of_services,
                                                    container_order = cmsPresentation.ContainerIndex,
                                                    group_order = cmsPresentation.GroupIndex,
                                                    item_order = cmsPresentation.Index,
                                                    metadata = cmsPresentation.MetadataLink,
                                                    metadata_target = String.IsNullOrEmpty(cmsPresentation.MetadataLink) ? null : cmsPresentation.MetadataTarget.ToString().ToLower(),
                                                    metadata_title = String.IsNullOrEmpty(cmsPresentation.MetadataLink) ? null : cmsPresentation.MetadataTitle,
                                                    metadata_button_style = String.IsNullOrEmpty(cmsPresentation.MetadataLink) ? null : cmsPresentation.MetadataButtonStyle.ToString().ToLower(),
                                                    group_metadata = cmsPresentation.GroupMetadataLink,
                                                    group_metadata_target = String.IsNullOrEmpty(cmsPresentation.GroupMetadataLink) ? null : cmsPresentation.GroupMetadataTarget?.ToString().ToLower(),
                                                    group_metadata_title = String.IsNullOrEmpty(cmsPresentation.GroupMetadataLink) ? null : cmsPresentation.GroupMetadataTitle,
                                                    group_metadata_button_style = String.IsNullOrEmpty(cmsPresentation.GroupMetadataLink) ? null : cmsPresentation.GroupMetadataButtonStyle?.ToString().ToLower(),
                                                    ui_groupname = String.IsNullOrWhiteSpace(cmsPresentation.UIGroupName) ? null : cmsPresentation.UIGroupName,
                                                    //allow_as_dynamic_markers = cmsPresentation.AllowAsDynamicMarkers == true ? (bool?)true : null
                                                };
                                                gdiProperties.Add(gdiProperty);
                                            }
                                        }

                                        #endregion

                                        presentation.items = gdiProperties.ToArray();

                                        // Leere Darstellungsvarianten (zB "alles aus") markiere/merken
                                        // Diese werden immer dargestellt. Alle anderen sind abhängig von den Berechtigungen der verbunden Layer
                                        // und werden nicht angezeigt, wenn keine berechtigten Layer/Themen damit verbunden ist
                                        presentation.IsEmpty = presentation.layers == null || presentation.layers.Count() == 0;
                                    }

                                    if (presentations.Count > 0)
                                    {
                                        if (_presentations.ContainsKey(cacheService.GdiCustomSchemeKey(serviceUrlKey, cmsGdiScheme)))
                                        {
                                            throw new Exception("Key for presentations already exists: " + cacheService.GdiCustomSchemeKey(serviceUrlKey, cmsGdiScheme));
                                        }

                                        _presentations.Add(cacheService.GdiCustomSchemeKey(serviceUrlKey, cmsGdiScheme), presentations.ToArray());
                                    }
                                }
                            }

                            #endregion

                            #region Service Queries

                            if (service.UseDynamicQueries() == false)
                            {
                                List<AuthObject<QueryDTO>> queries = new List<AuthObject<QueryDTO>>();

                                CmsNodeCollection queryNodes = cmsHlp.GetServiceQueries(serviceNode);

                                foreach (CmsNode queryNode in queryNodes)
                                {
                                    CmsNode layerNode = cmsHlp.GetQueryLayerNode(queryNode);

                                    if (layerNode == null)
                                    {
                                        continue;
                                    }

                                    #region Associted Layers

                                    List<QueryDTO.AssociatedLayer> associatedLayers = new List<QueryDTO.AssociatedLayer>();

                                    CmsNodeCollection associatedLayerLinks = cmsHlp.GetAssociatedTocLayerLinks(queryNode);

                                    if (associatedLayerLinks.Count != 0)
                                    {
                                        foreach (CmsLink associatedLayerLink in associatedLayerLinks)
                                        {
                                            if (associatedLayerLink.Target == null)
                                            {
                                                continue;
                                            }

                                            string associatedServiceId = associatedLayerLink.Target.ParentNode.ParentNode.Url + (String.IsNullOrEmpty(cmsName) ? String.Empty : "@" + cmsName);
                                            if (associatedServiceId == serviceUrlKey)
                                            {
                                                associatedServiceId = null;
                                            }

                                            associatedLayers.Add(new QueryDTO.AssociatedLayer() { id = associatedLayerLink.Target.Id, serviceid = associatedServiceId });
                                        }
                                    }
                                    else
                                    {
                                        associatedLayers.Add(new QueryDTO.AssociatedLayer() { id = layerNode.Id });
                                    }

                                    #endregion

                                    QueryDTO query = new QueryDTO()
                                    {
                                        id = queryNode.Url,
                                        name = queryNode.Name,
                                        AllowEmptyQueries = (bool)queryNode.Load("allowemptysearch", true),
                                        LayerId = layerNode.Id,
                                        associatedlayers = associatedLayers.ToArray(),
                                        geojuhu = (bool)queryNode.Load("gdi_geojuhu", false),
                                        geojuhuschema = queryNode.LoadString("gdi_geojuhuschema"),
                                        zoomscale = Convert.ToInt32(queryNode.Load("minzoomscale", -1)),
                                        Filter = queryNode.LoadString("gdi_filter"),
                                        draggable = (bool)queryNode.Load("draggable", false),
                                        Distinct = (bool)queryNode.Load("distinct", false),
                                        Union = (bool)queryNode.Load("union", false),
                                        ApplyZoomLimits = (bool)queryNode.Load("applyzoomlimits", false),
                                        MaxFeatures = (int)queryNode.Load("maxfeatures", 0),
                                        QueryDef = new QueryDefinitions.QueryDefinition(cmsHlp, queryNode)
                                        {
                                            Id = $"{serviceUrlKey}:{queryNode.Url}"
                                        },
                                        PreviewTextTemplate = queryNode.LoadString("preview_text_template")
                                    };

                                    #region Search Items

                                    List<AuthObject<QueryDTO.Item>> items = new List<AuthObject<QueryDTO.Item>>();
                                    foreach (CmsNode itemNode in cmsHlp.GetQuerySearchItems(queryNode))
                                    {
                                        var lookupConnectionString = itemNode.LoadString("lookup_connectionstring");
                                        var lookupSqlClause = itemNode.LoadString("lookup_sqlclause");
                                        IEnumerable<string> autocomplete_depends_on = null;

                                        if (!String.IsNullOrEmpty(lookupSqlClause))
                                        {
                                            var sqlParamters = new UniqueList();
                                            sqlParamters.AddRange(Globals.KeyParameters(lookupSqlClause, startingBracket: "[", endingBracket: "]"));
                                            sqlParamters.AddRange(Globals.KeyParameters(lookupSqlClause, startingBracket: "{{", endingBracket: "}}"));

                                            sqlParamters.Remove(itemNode.Url);
                                            if (sqlParamters.Count() > 0)
                                            {
                                                autocomplete_depends_on = sqlParamters.ToArray();
                                            }
                                        }

                                        items.Add(new AuthObject<QueryDTO.Item>(
                                            new QueryDTO.Item()
                                            {
                                                id = itemNode.Url,
                                                name = itemNode.Name,
                                                visible = (bool)itemNode.Load("visible", true),
                                                required = (bool)itemNode.Load("required", false),
                                                autocomplete = (bool)itemNode.Load("uselookup", false),
                                                autocomplete_minlength = (int)itemNode.Load("mininputlength", 2),
                                                autocomplete_depends_on = autocomplete_depends_on,
                                                examples = itemNode.LoadString("examples"),
                                                Fields = itemNode.LoadString("fields").Split(';'),
                                                UseUpper = (bool)itemNode.Load("useupper", false),
                                                QueryMethod = (QueryMethod)itemNode.Load("method", QueryMethod.Exact),
                                                Regex = itemNode.LoadString("regex"),
                                                FormatExpression = itemNode.LoadString("format_expression"),
                                                Lookup_ConnectionString = lookupConnectionString,
                                                Lookup_SqlClause = lookupSqlClause,
                                                SqlInjectionWhiteList = itemNode.LoadString("sqlinjectionwhitelist"),
                                                IgnoreInPreviewText = (bool)itemNode.Load("ignore_in_preview_text", false)
                                            }, CmsDocument.GetAuthNodeFast(cms, itemNode.NodeXPath)));
                                    }
                                    query.AuthItems = items.ToArray();

                                    #endregion

                                    #region Table Fields

                                    List<AuthObject<TableFieldDTO>> fields = new List<AuthObject<TableFieldDTO>>();
                                    foreach (CmsNode columnNode in cmsHlp.TableColumns(queryNode))
                                    {
                                        ColumnType colType = (ColumnType)columnNode.Load("columntype", (int)ColumnType.Field);
                                        TableFieldDTO field = null;

                                        if (colType == ColumnType.Field ||
                                            colType == ColumnType.EmailAddress ||
                                            colType == ColumnType.PhoneNumber)
                                        {
                                            field = new TableFieldData();

                                            ((TableFieldData)field).FieldName = (string)columnNode.Load("fieldname", String.Empty);
                                            ((TableFieldData)field).SimpleDomains = (string)columnNode.Load("simpledomains", String.Empty);
                                            ((TableFieldData)field).RawHtml = (bool)columnNode.Load("rawhtml", false);
                                            ((TableFieldData)field).ColType = colType;
                                            ((TableFieldData)field).SortingAlgorithm = (string)columnNode.Load("sorting_alg", String.Empty);
                                            ((TableFieldData)field).AutoSort = (FieldAutoSortMethod)columnNode.Load("auto_sort", (int)FieldAutoSortMethod.None);
                                        }
                                        else if (colType == ColumnType.DateTime)
                                        {
                                            field = new TableFieldDateTime();

                                            ((TableFieldDateTime)field).FieldName = (string)columnNode.Load("fieldname", String.Empty);
                                            ((TableFieldDateTime)field).DisplayType = (DateFieldDisplayType)columnNode.Load("displaytype", (int)DateFieldDisplayType.Normal);
                                            ((TableFieldDateTime)field).FormatString = (string)columnNode.Load("date_formatstring", String.Empty);
                                        }
                                        else if (colType == ColumnType.MultiField)
                                        {
                                            field = new TableFieldDataMulti();
                                            ((TableFieldDataMulti)field).FieldNames = Helper.StringToArray((string)columnNode.Load("fieldnames", String.Empty));
                                        }
                                        else if (colType == ColumnType.Hotlink)
                                        {
                                            field = new TableFieldHotlinkDTO();

                                            ((TableFieldHotlinkDTO)field).HotlinkUrl = (string)columnNode.Load("hotlinkurl", String.Empty);
                                            ((TableFieldHotlinkDTO)field).HotlinkName = (string)columnNode.Load("hotlinkname", String.Empty);
                                            ((TableFieldHotlinkDTO)field).One2N = (bool)columnNode.Load("one2n", false);
                                            ((TableFieldHotlinkDTO)field).One2NSeperator = (char)columnNode.Load("one2nseperator", ';');
                                            ((TableFieldHotlinkDTO)field).Target = (BrowserWindowTarget)columnNode.Load("target", (int)BrowserWindowTarget._blank);
                                            ((TableFieldHotlinkDTO)field).ImageExpression = (string)columnNode.Load("imgexpression", String.Empty);
                                            ((TableFieldHotlinkDTO)field).ImageHeight = (int)columnNode.Load("imgheight", 0);
                                            ((TableFieldHotlinkDTO)field).ImageWidth = (int)columnNode.Load("imgwidth", 0);
                                        }
                                        else if (colType == ColumnType.Expression)
                                        {
                                            field = new TableFieldExpressionDTO();

                                            ((TableFieldExpressionDTO)field).Expression = (string)columnNode.Load("expression", String.Empty);
                                            ((TableFieldExpressionDTO)field).ColDataType = (ColumnDataType)columnNode.Load("coldatatype", (int)ColumnDataType.String);
                                        }
                                        else if (colType == ColumnType.ImageExpression)
                                        {
                                            field = new TableFieldImageDTO();

                                            ((TableFieldImageDTO)field).ImageExpression = (string)columnNode.Load("imgexpression", String.Empty);
                                            ((TableFieldImageDTO)field).ImageWidth = (int)columnNode.Load("iwidth", 0);
                                            ((TableFieldImageDTO)field).ImageHeight = (int)columnNode.Load("iheight", 0);
                                        }

                                        if (field != null)
                                        {
                                            field.ColumnName = !String.IsNullOrEmpty(columnNode.AliasName) ? columnNode.AliasName : columnNode.Name;
                                            field.Visible = (bool)columnNode.Load("visible", true);

                                            fields.Add(new AuthObject<TableFieldDTO>(field, CmsDocument.GetAuthNodeFast(cms, columnNode.NodeXPath)));
                                        }
                                    }
                                    query.AuthFields = fields.ToArray();

                                    #endregion

                                    #region Export Table Formats

                                    List<AuthObject<QueryDTO.TableExportFormat>> exportFormats = new List<AuthObject<QueryDTO.TableExportFormat>>();
                                    foreach (CmsNode exportFormatNode in cmsHlp.GetExportTableFormats(queryNode))
                                    {
                                        var exportFormat = new QueryDTO.TableExportFormat()
                                        {
                                            Id = exportFormatNode.Url,
                                            Name = exportFormatNode.Name,
                                            FileExtension = exportFormatNode.Load("fileext", "txt")?.ToString(),
                                            FormatString = exportFormatNode.LoadString("formatstring")
                                        };
                                        exportFormats.Add(new AuthObject<QueryDTO.TableExportFormat>(exportFormat, CmsDocument.GetAuthNodeFast(cms, exportFormatNode.NodeXPath)));
                                    }
                                    query.AuthTableExportFormats = exportFormats.ToArray();

                                    #endregion

                                    #region Feature Transfers

                                    var featureTransfers = new List<AuthObject<QueryDTO.FeatureTransfer>>();

                                    foreach (CmsNode cmsQueryFeatureTransferNode in cmsHlp.GetCmsQueryFeatureTransfers(queryNode))
                                    {
                                        var targetLinks = cmsHlp.GetCmsQueryFeatureTransferTargets(cmsQueryFeatureTransferNode);
                                        var targets = new List<AuthObject<IQueryFeatureTransferTargetBridge>>();
                                        var setterNodes = cmsHlp.GetCmsQueryFeatureTransferFieldSetters(cmsQueryFeatureTransferNode);
                                        var setters = new List<AuthObject<IFieldSetter>>();

                                        foreach (CmsLink targetLink in targetLinks.Links)  // Target EditThemes
                                        {
                                            var targetServiceNode = cmsHlp.ServiceNodeFromXPath(targetLink.Target?.NodeXPath);

                                            if (targetServiceNode == null)
                                            {
                                                continue;
                                            }

                                            targets.Add(new AuthObject<IQueryFeatureTransferTargetBridge>(new QueryDTO.FeatureTransfer.Target()
                                            {
                                                ServiceId = String.IsNullOrEmpty(cmsName) ? targetServiceNode.Url : $"{targetServiceNode.Url}@{cmsName}",
                                                EditThemeId = targetLink.Target.LoadString("themeid"),
                                                PipelineSuppressAutovalues = (bool)targetLink.Load("suppress_autovalues", false),
                                                PipelineSuppressValidation = (bool)targetLink.Load("suppress_validation", false)
                                            },
                                                CmsDocument.GetAuthNodeFast(cms, targetLink.NodeXPath)
                                            ));
                                        }

                                        foreach (CmsNode setterNode in setterNodes)
                                        {
                                            setters.Add(new AuthObject<IFieldSetter>(new QueryDTO.FeatureTransfer.FieldSetter()
                                            {
                                                Field = setterNode.LoadString("field"),
                                                ValueExpression = setterNode.LoadString("value_expression"),
                                                IsDefaultValue = (bool)setterNode.Load("is_defaultvalue", false),
                                                IsRequired = (bool)setterNode.Load("is_required", false)
                                            },
                                            CmsDocument.GetAuthNodeFast(cms, setterNode.NodeXPath)));
                                        }

                                        var featureTransfer = new QueryDTO.FeatureTransfer()
                                        {
                                            Id = cmsQueryFeatureTransferNode.Url,
                                            Name = cmsQueryFeatureTransferNode.Name,
                                            AuthTargets = targets,
                                            AuthFieldSetters = setters,
                                            CopyAttributes = (bool)cmsQueryFeatureTransferNode.Load("copy_attributes", false),
                                            Method = (FeatureTransferMethod)cmsQueryFeatureTransferNode.Load("method", (int)FeatureTransferMethod.Copy)
                                        };

                                        if (featureTransfer.AuthTargets.Count() > 0)
                                        {
                                            featureTransfers.Add(new AuthObject<QueryDTO.FeatureTransfer>(featureTransfer, CmsDocument.GetAuthNodeFast(cms, cmsQueryFeatureTransferNode.NodeXPath)));
                                        }
                                    }

                                    if (featureTransfers.Count > 0)
                                    {
                                        query.AuthFeatureTransfers = featureTransfers.ToArray();
                                    }

                                    #endregion

                                    #region Attachments

                                    if ((bool)queryNode.Load("show_attachments", false) == true)
                                    {
                                        CmsDocument.AuthNode showAttachementsAuthNode = CmsDocument.GetPropertyAuthNodeFast(cms, queryNode.NodeXPath, "show_attachments");

                                        _authBoolProperties.Add(new AuthProperty<bool>(
                                            $"{serviceUrlKey}::{query.id}::show_attachments",
                                            true,
                                            false,
                                            showAttachementsAuthNode));
                                    }

                                    #endregion

                                    queries.Add(new AuthObject<QueryDTO>(query, CmsDocument.GetAuthNodeFast(cms, queryNode.NodeXPath)));
                                }

                                if (queries.Count > 0)
                                {
                                    if (_queries.ContainsKey(serviceUrlKey))
                                    {
                                        throw new Exception("Key for queries already exists: " + serviceUrlKey);
                                    }

                                    _queries.Add(serviceUrlKey, queries.ToArray());
                                }
                            }

                            #endregion

                            #region Editable Layers (old method... from TOC Gdi Properties)

                            List<AuthObject<EditThemeDTO>> editThemes = new List<AuthObject<EditThemeDTO>>();

                            foreach (var tocElementNode in cmsHlp.GetTocElements(serviceNode, tocName))
                            {
                                var tocElementNodeGdiProperties = cmsHlp.GetTocElementGdiProperties(tocElementNode);

                                foreach (var tocElementNodeGdiProperty in tocElementNodeGdiProperties)
                                {
                                    if (tocElementNode.Target != null)
                                    {
                                        EditThemeDTO editTheme = new EditThemeDTO()
                                        {
                                            Name = tocElementNodeGdiProperty.LoadString("gdi_aliasname"),
                                            ThemeId = tocElementNodeGdiProperty.LoadString("gdi_editthemeid"),
                                            LayerId = tocElementNode.Target.Id,
                                            Visible = (bool)tocElementNodeGdiProperty.Load("gdi_visible", true),
                                            IsEditServiceTheme = (bool)tocElementNodeGdiProperty.Load("gdi_editservice", false)
                                        };
                                        if (String.IsNullOrWhiteSpace(editTheme.Name))
                                        {
                                            editTheme.Name = !String.IsNullOrWhiteSpace(tocElementNode.AliasName) ?
                                                tocElementNode.AliasName : tocElementNode.Name;
                                        }
                                        var xmlNode = editTheme.EditThemeXmlNode(ApiGlobals.AppEtcPath);
                                        if (xmlNode == null)
                                        {
                                            _warnings.Add($"{serviceUrlKey} - Edittheme mask '{editTheme.ThemeId}' not found  in XML files");
                                            continue;
                                        }
                                        editTheme.DbRights = xmlNode.Attributes["dbrights"] != null ? xmlNode.Attributes["dbrights"].Value.ToEditingRights() : EditingRights.Unknown;

                                        editThemes.Add(new AuthObject<EditThemeDTO>(editTheme, CmsDocument.GetAuthNodeFast(cms, tocElementNode.NodeXPath + "@gdiproperties_" + tocElementNodeGdiProperty.ItemGuid)));
                                    }
                                }
                            }

                            if (editThemes.Count > 0)
                            {
                                if (_editThemes.ContainsKey(serviceUrlKey))
                                {
                                    throw new Exception("Key for editthemes already exists: " + serviceUrlKey);
                                }

                                _editThemes.Add(serviceUrlKey, editThemes.ToArray());
                            }

                            #endregion

                            #region Edititable Layers (new CMS param Editing)

                            editThemes = new List<AuthObject<EditThemeDTO>>();
                            var gdiEditingThemes = cmsHlp.CmsEditingThemes(serviceNode);
                            foreach (var cmsEditingTheme in gdiEditingThemes)
                            {
                                var editThemeSrs = (int)cmsEditingTheme.Load("srs", -1);
                                if (editThemeSrs <= 0)
                                {
                                    continue;  // Srs must be definied!!!
                                }

                                var editTheme = new EditThemeDTO()
                                {
                                    Name = cmsEditingTheme.Name,
                                    ThemeId = (string)cmsEditingTheme.Load("themeid", String.Empty),
                                    Visible = (bool)cmsEditingTheme.Load("visible", true),
                                    IsEditServiceTheme = (bool)cmsEditingTheme.Load("editservice", false),
                                    Srs = editThemeSrs,
                                    CanGenerateMaskXml = true,
                                    Tags = ((string)cmsEditingTheme.Load("tags", String.Empty)).Split(",").Select(x => x.Trim()).Where(x => !String.IsNullOrEmpty(x)).ToArray(),
                                };

                                #region Rights

                                editTheme.DbRights |= (bool)cmsEditingTheme.Load("allow_insert", true) ? EditingRights.Insert : EditingRights.Unknown;
                                editTheme.DbRights |= (bool)cmsEditingTheme.Load("allow_update", true) ? EditingRights.Update : EditingRights.Unknown;
                                editTheme.DbRights |= (bool)cmsEditingTheme.Load("allow_delete", true) ? EditingRights.Delete : EditingRights.Unknown;
                                editTheme.DbRights |= (bool)cmsEditingTheme.Load("allow_editgeometry", true) ? EditingRights.Geometry : EditingRights.Unknown;
                                editTheme.DbRights |= (bool)cmsEditingTheme.Load("allow_massattributation", false) ? EditingRights.MassAttributeable : EditingRights.Unknown;
                                editTheme.DbRights |= (bool)cmsEditingTheme.Load("allow_multipartgeometries", true) ? EditingRights.MultipartGeometries : EditingRights.Unknown;

                                #endregion

                                #region Insert Actions

                                List<EditThemeDTO.InsertAction> insertActions = new List<EditThemeDTO.InsertAction>();

                                if ((bool)cmsEditingTheme.Load("show_save_button", true))
                                {
                                    insertActions.Add(new EditThemeDTO.InsertAction() { IsDefault = true, Action = EditingInsertAction.Save });
                                }
                                if ((bool)cmsEditingTheme.Load("show_save_and_select_button", true))
                                {
                                    insertActions.Add(new EditThemeDTO.InsertAction() { IsDefault = true, Action = EditingInsertAction.SaveAndSelect });
                                }

                                for (int iaIndex = 1; iaIndex <= 5; iaIndex++)
                                {
                                    var insertAction = (EditingInsertAction)(int)cmsEditingTheme.Load($"insert_action{iaIndex}", (int)EditingInsertAction.None);
                                    var insertActionText = (string)cmsEditingTheme.Load($"insert_action_text{iaIndex}", String.Empty);

                                    if (insertAction != EditingInsertAction.None && !String.IsNullOrEmpty(insertActionText))
                                    {
                                        insertActions.Add(new EditThemeDTO.InsertAction() { IsDefault = false, Action = insertAction, ButtonText = insertActionText });
                                    }
                                }

                                editTheme.InsertActions = insertActions.ToArray();

                                #endregion

                                #region Insert/Update Actions

                                editTheme.AutoExplodeMultipartFeatures = (bool)cmsEditingTheme.Load("auto_explode_multipart_features", false);

                                #endregion

                                #region Fields

                                var editFields = new List<AuthObject<EditThemeDTO.EditField>>();
                                foreach (var cmsEditingFieldNode in cmsHlp.GetCmsEditingThemeFieldNodes(cmsEditingTheme))
                                {
                                    editFields.Add(new AuthObject<EditThemeDTO.EditField>(
                                        new EditThemeDTO.EditField(cmsEditingFieldNode, cmsName),
                                        CmsDocument.GetAuthNodeFast(cms, cmsEditingFieldNode.NodeXPath)));
                                }
                                editTheme.EditFields = editFields;

                                #endregion

                                #region Categories

                                var editCategories = new List<AuthObject<EditThemeDTO.EditCategory>>();
                                foreach (var cmsEditingCategoryNode in cmsHlp.GetCmsEditingCategoryNodes(cmsEditingTheme))
                                {
                                    editCategories.Add(new AuthObject<EditThemeDTO.EditCategory>(
                                        new EditThemeDTO.EditCategory(cmsEditingCategoryNode),
                                        CmsDocument.GetAuthNodeFast(cms, cmsEditingCategoryNode.NodeXPath)));
                                }
                                editTheme.EditCategories = editCategories;

                                #endregion

                                #region Snapping

                                var snappingSchemes = new List<EditThemeDTO.SnappingScheme>();

                                foreach (CmsLink cmsEditingSnappingLink in cmsHlp.GetCmsEditingSnappingSchemeLinks(cmsEditingTheme))
                                {
                                    var snappingSchemeNode = cmsEditingSnappingLink.Target;
                                    var snappingServiceNode = snappingSchemeNode?.ParentNode?.ParentNode;

                                    if (snappingSchemeNode != null && snappingServiceNode != null)
                                    {
                                        List<string> types = new List<string>();
                                        if ((bool)cmsEditingSnappingLink.Load("nodes", true))
                                        {
                                            types.Add("nodes");
                                        }

                                        if ((bool)cmsEditingSnappingLink.Load("edges", true))
                                        {
                                            types.Add("edges");
                                        }

                                        if ((bool)cmsEditingSnappingLink.Load("endpoints", true))
                                        {
                                            types.Add("endpoints");
                                        }

                                        List<EditThemeDTO.SnappingScheme.FixToDefintion> fixToList = new List<EditThemeDTO.SnappingScheme.FixToDefintion>();
                                        foreach (string fixTo in ((string)cmsEditingSnappingLink.Load("fix_to", String.Empty)).Split('|'))
                                        {
                                            if (!String.IsNullOrEmpty(fixTo))
                                            {
                                                fixToList.Add(new EditThemeDTO.SnappingScheme.FixToDefintion(fixTo));
                                            }
                                        }


                                        string snappingServiceId = String.IsNullOrWhiteSpace(cmsName) ? snappingServiceNode.Url : snappingServiceNode.Url + "@" + cmsName;
                                        snappingSchemes.Add(new EditThemeDTO.SnappingScheme()
                                        {
                                            Id = snappingSchemeNode.Url,
                                            ServiceId = snappingServiceId,
                                            Types = types.ToArray(),
                                            FixTo = fixToList.Count > 0 ? fixToList.ToArray() : null
                                        });
                                    }
                                }
                                if (snappingSchemes.Count > 0)
                                {
                                    editTheme.Snapping = snappingSchemes.ToArray();
                                }

                                #endregion

                                #region Mask Validation

                                var maskValidations = new List<AuthObject<EditThemeDTO.MaskValidation>>();
                                foreach (CmsNode cmsEditingMaskValidationNode in cmsHlp.GetCmsEditingMaskValidationNodes(cmsEditingTheme))
                                {
                                    var maskValidation = new EditThemeDTO.MaskValidation()
                                    {
                                        FieldName = cmsEditingMaskValidationNode.LoadString("fieldname"),
                                        Operator = (MaskValidationOperators)cmsEditingMaskValidationNode.Load("operator", (int)MaskValidationOperators.Ident),
                                        Validator = cmsEditingMaskValidationNode.LoadString("validator"),
                                        Message = cmsEditingMaskValidationNode.LoadString("message")
                                    };

                                    if (maskValidation.IsValid())
                                    {
                                        maskValidations.Add(new AuthObject<EditThemeDTO.MaskValidation>(maskValidation
                                            , CmsDocument.GetAuthNodeFast(cms, cmsEditingMaskValidationNode.NodeXPath)));
                                    }
                                }
                                if (maskValidations.Count > 0)
                                {
                                    editTheme.MaskValidations = maskValidations.ToArray();
                                }

                                #endregion

                                var editingLayerNode = cmsHlp.GetCmsEditingThemeLayerNode(cmsEditingTheme);

                                if (editingLayerNode != null)
                                {
                                    editTheme.LayerId = editingLayerNode.Id;

                                    editThemes.Add(new AuthObject<EditThemeDTO>(editTheme, CmsDocument.GetAuthNodeFast(cms, cmsEditingTheme.NodeXPath)));
                                }
                                else
                                {
                                    _warnings.Add($"{serviceUrlKey} - Edit theme: {cmsEditingTheme.Name} => no editable layer found...");
                                }
                            }

                            if (editThemes.Count > 0)
                            {
                                if (_editThemes.ContainsKey(serviceUrlKey))
                                {
                                    var list = new List<AuthObject<EditThemeDTO>>(_editThemes[serviceUrlKey]);
                                    list.AddRange(editThemes);
                                    _editThemes[serviceUrlKey] = list.ToArray();
                                }
                                else
                                {
                                    _editThemes.Add(serviceUrlKey, editThemes.ToArray());
                                }
                            }

                            #endregion

                            #region LayerProperties

                            var layerProperies = new List<LayerPropertiesDTO>();
                            var layerAuthProperties = new List<AuthProperty<bool>>();

                            #region Read CMS Toc (old Method)

                            _tocName[serviceUrlKey] = tocName;

                            if (!String.IsNullOrWhiteSpace(tocName) && tocName != "default")
                            {
                                foreach (var tocElement in cmsHlp.GetTocElements(serviceNode, tocName))
                                {
                                    if (tocElement?.Target == null)
                                    {
                                        continue;
                                    }

                                    var layerProps = new LayerPropertiesDTO()
                                    {
                                        Id = tocElement.Target.Id,
                                        Aliasname = tocElement.LoadString("aliasname"),

                                        Metadata = tocElement.LoadString("metadata"),
                                        MetadataFormat = tocElement.LoadString("metadataformat"),

                                        Visible = (bool)tocElement.Load("visible", true),
                                        Locked = (bool)tocElement.Load("locked", false),
                                        OgcId = tocElement.LoadString("ogcid"),
                                        Abstract = tocElement.LoadString("abstract"),
                                        ShowInLegend = (bool)tocElement.Load("legend", true),
                                        LegendAliasname = tocElement.LoadString("legendaliasname"),
                                        IsTocLayer = true
                                    };

                                    CmsNode parentTocElement = cmsHlp.GetTocParentNode(serviceNode, tocName, tocElement);
                                    string fullName = String.IsNullOrWhiteSpace(layerProps.Aliasname) ?
                                        (tocElement.Target.Name.Contains("\\") ? tocElement.Target.Name.Substring(tocElement.Target.Name.LastIndexOf("\\") + 1) : tocElement.Target.Name) :
                                         layerProps.Aliasname;

                                    string parentName = String.Empty;
                                    while (parentTocElement != null)
                                    {
                                        parentName = !String.IsNullOrWhiteSpace(parentName) ? parentTocElement.Name + "\\" + parentName : parentTocElement.Name;
                                        if ((bool)parentTocElement.Load("dropdownable", true) == false)
                                        {
                                            layerProps.OgcGroupId = !String.IsNullOrWhiteSpace(parentTocElement.LoadString("ogcid")) ? parentTocElement.LoadString("ogcid") : parentTocElement.Name;
                                            layerProps.OgcGroupTitle = parentTocElement.Name;
                                        }
                                        if ((bool)parentTocElement.Load("visible", true) == false)
                                        {
                                            layerProps.Visible = false;
                                        }

                                        if ((bool)parentTocElement.Load("dropdownable", true) == false)
                                        {
                                            layerProps.UnDropdownableParentName = cmsHlp.GetTocFullName(serviceNode, tocName, parentTocElement);
                                        }

                                        parentTocElement = cmsHlp.GetTocParentNode(serviceNode, tocName, parentTocElement);
                                    }

                                    layerProps.Name = (String.IsNullOrWhiteSpace(parentName) ? "" : parentName + "\\") + fullName;

                                    layerProperies.Add(layerProps);

                                    var layerAuthNode = CmsDocument.GetAuthNodeFast(cms, tocElement.NodeXPath);
                                    var layerVisibilityAutPropertyId = tocElement.Target.Id.LayerVisibilityAuthPropertyId();
                                    if (layerAuthNode.HasDeniedMembers() && !layerAuthNode.InList(layerAuthProperties.Where(l => l.Property == layerVisibilityAutPropertyId).Select(p => p.AuthNode)))
                                    {
                                        layerAuthProperties.Add(new AuthProperty<bool>(layerVisibilityAutPropertyId, true, false, layerAuthNode));
                                    }
                                }
                            }

                            #endregion

                            foreach (var layerPropertyNode in cmsHlp.GetLayerProperties(serviceNode))
                            {
                                if (layerPropertyNode.Target != null)
                                {
                                    var layerProps = layerProperies.Where(l => l.Id == layerPropertyNode.Target.Id).FirstOrDefault() ?? new LayerPropertiesDTO();

                                    layerProps.Id = layerPropertyNode.Target.Id;
                                    layerProps.Name = null;
                                    layerProps.Aliasname = layerPropertyNode.LoadString("aliasname");

                                    layerProps.Metadata = layerPropertyNode.LoadString("metadata");
                                    layerProps.MetadataFormat = layerPropertyNode.LoadString("metadataformat");
                                    layerProps.MetadataTarget = (BrowserWindowTarget2)layerPropertyNode.Load("metadata_target", (int)BrowserWindowTarget2.tab);
                                    layerProps.MetadataTitle = layerPropertyNode.LoadString("metadata_title");
                                    layerProps.MetadataButtonStyle = (MetadataButtonStyle)layerPropertyNode.Load("metadata_button_style", (int)MetadataButtonStyle.i_button);

                                    layerProps.Visible = (bool)layerPropertyNode.Load("visible", true);
                                    layerProps.Locked = (bool)layerPropertyNode.Load("locked", false);
                                    layerProps.OgcId = layerPropertyNode.LoadString("ogcid");
                                    layerProps.Abstract = layerPropertyNode.LoadString("abstract");
                                    layerProps.ShowInLegend = (bool)layerPropertyNode.Load("legend", true);
                                    layerProps.LegendAliasname = layerPropertyNode.LoadString("legendaliasname");
                                    layerProps.Description = layerPropertyNode.LoadString("description");
                                    layerProps.IsTocLayer = cmsHlp.GetTocElement(serviceNode, tocName, layerPropertyNode.Target.Id) != null;

                                    if (!layerProperies.Contains(layerProps))
                                    {
                                        layerProperies.Add(layerProps);
                                    }

                                    var layerAuthNode = CmsDocument.GetAuthNodeFast(cms, layerPropertyNode.NodeXPath);
                                    var layerVisibilityAutPropertyId = layerPropertyNode.Target.Id.LayerVisibilityAuthPropertyId();
                                    if (layerAuthNode.HasDeniedMembers() && !layerAuthNode.InList(layerAuthProperties.Where(l => l.Property == layerVisibilityAutPropertyId).Select(p => p.AuthNode)))
                                    {
                                        layerAuthProperties.Add(new AuthProperty<bool>(layerVisibilityAutPropertyId, true, false, layerAuthNode));
                                    }
                                }
                            }

                            if (layerProperies.Count > 0)
                            {
                                if (_layerProperties.ContainsKey(serviceUrlKey))
                                {
                                    throw new Exception("Key for layerProperties already exists: " + serviceUrlKey);
                                }

                                _layerProperties.Add(serviceUrlKey, layerProperies.ToArray());
                            }

                            #region Theme Authorizations

                            foreach (var themeNode in cms.SelectNodes(null, $"{serviceNode.NodeXPath}/themes/*"))
                            {
                                var layerAuthNode = CmsDocument.GetAuthNodeFast(cms, themeNode.NodeXPath);
                                var layerVisibilityAutPropertyId = themeNode.Id.LayerVisibilityAuthPropertyId();
                                if (layerAuthNode.HasDeniedMembers() && !layerAuthNode.InList(layerAuthProperties.Where(l => l.Property == layerVisibilityAutPropertyId).Select(p => p.AuthNode)))
                                {
                                    layerAuthProperties.Add(new AuthProperty<bool>(layerVisibilityAutPropertyId, true, false, layerAuthNode));
                                }
                            }

                            #endregion

                            if (layerAuthProperties.Count > 0)
                            {
                                if (_layerAuthVisibility.ContainsKey(serviceUrlKey))
                                {
                                    throw new Exception("Key for layerAuthVisibility already exists: " + serviceUrlKey);
                                }

                                _layerAuthVisibility.Add(serviceUrlKey, layerAuthProperties.ToArray());
                            }

                            if (service is IMapService2 &&
                                (DynamicDehavior)cmsLink.Target.Load("dynamic_behavior", (int)DynamicDehavior.AutoAppendNewLayers) == DynamicDehavior.UseStrict)
                            {
                                ((IMapService2)service).ServiceThemes = cmsHlp.ServiceThemes(serviceNode)?.ToArray();
                            }

                            #endregion

                            #region Vis Filters

                            List<AuthObject<VisFilterDTO>> visFilters = new List<AuthObject<VisFilterDTO>>();

                            foreach (var visFilterNode in cmsHlp.GetServiceVisFilters(serviceNode))
                            {
                                var visFilter = new VisFilterDTO()
                                {
                                    Id = visFilterNode.Url, //$"{ serviceUrlKey }~{ visFilterNode.Url }",
                                    Name = visFilterNode.Name,
                                    FilterType = (VisFilterType)visFilterNode.Load("type", (int)VisFilterType.visible),
                                    LayerNamesString = visFilterNode.LoadString("layers"),
                                    Filter = visFilterNode.LoadString("filter"),
                                    SetLayersVisible = (bool)visFilterNode.Load("setlayervis", false),
                                    LookupLayerNameString = visFilterNode.LoadString("lookup_layer")
                                };

                                int lookupCount = (int)visFilterNode.Load("lookupCount", 0);
                                for (int l = 0; l < lookupCount; l++)
                                {
                                    visFilter.AddLookup(new VisFilterDTO.LookupDef()
                                    {
                                        Type = (Lookuptype)visFilterNode.Load("type" + l, (int)Lookuptype.ComboBox),
                                        Parameter = visFilterNode.LoadString("key" + l),
                                        ConnectionString = visFilterNode.LoadString("lookup_connectionstring" + l),
                                        SqlClause = visFilterNode.LoadString("lookup_sqlclause" + l)
                                    });
                                }

                                visFilters.Add(new AuthObject<VisFilterDTO>(visFilter, CmsDocument.GetAuthNodeFast(cms, visFilterNode.NodeXPath)));
                            }

                            if (visFilters.Count > 0)
                            {
                                if (_visFilters.ContainsKey(serviceUrlKey))
                                {
                                    throw new Exception("Key for visfilters already exists: " + serviceUrlKey);
                                }

                                _visFilters.Add(serviceUrlKey, visFilters.ToArray());
                            }

                            #endregion

                            #region Chainage Themes

                            List<AuthObject<ChainageThemeDTO>> chainageThemes = new List<AuthObject<ChainageThemeDTO>>();
                            foreach (var chainageNode in cmsHlp.ServiceChaingageThemes(serviceNode.NodeXPath))
                            {
                                var lineThemeNode = cms.SelectSingleNode(null, chainageNode.NodeXPath + "/chainagelinetheme/*") as CmsLink;
                                var pointThemeNode = cms.SelectSingleNode(null, chainageNode.NodeXPath + "/chainagepointtheme/*") as CmsLink;
                                var apiServiceUrl = chainageNode.LoadString("serviceurl");

                                if (lineThemeNode?.Target != null /*&& pointThemeNode?.Target != null*/
                                    || !String.IsNullOrEmpty(apiServiceUrl))
                                {
                                    var chainageTheme = new ChainageThemeDTO()
                                    {
                                        Id = String.IsNullOrWhiteSpace(cmsName) ? chainageNode.Url : chainageNode.Url + "@" + cmsName, //chainageNode.Url,
                                        Name = chainageNode.Name,
                                        ServiceId = serviceUrlKey,
                                        PointLayerId = pointThemeNode?.Target.Id,
                                        LineLayerId = lineThemeNode?.Target.Id,
                                        Expression = chainageNode.LoadString("expression"),
                                        PointLineRelation = chainageNode.LoadString("pointlinerelation"),
                                        PointStatField = chainageNode.LoadString("pointstatfield"),
                                        ApiServiceUrl = apiServiceUrl,
                                        Unit = ((LengthUnit)chainageNode.Load("unit", (int)LengthUnit.m)).ToString(),
                                        CalcSrefId = (int)chainageNode.Load("calcsrefid", 0)
                                    };
                                    chainageThemes.Add(new AuthObject<ChainageThemeDTO>(chainageTheme, CmsDocument.GetAuthNodeFast(cms, chainageNode.NodeXPath)));
                                }
                            }
                            if (chainageThemes.Count > 0)
                            {
                                if (_chainageThemes.ContainsKey(serviceUrlKey))
                                {
                                    throw new Exception("Key for chainage already exists: " + serviceUrlKey);
                                }

                                _chainageThemes.Add(serviceUrlKey, chainageThemes.ToArray());
                            }

                            #endregion

                            #region Labeling

                            List<AuthObject<LabelingDTO>> labelingThemes = new List<AuthObject<LabelingDTO>>();
                            foreach (var labelingNode in cmsHlp.ServiceLabeling(serviceNode.NodeXPath))
                            {
                                var themeLink = cms.SelectSingleNode(null, labelingNode.NodeXPath + "/labellingtheme/*") as CmsLink;
                                var fieldNodes = cms.SelectNodes(null, labelingNode.NodeXPath + "/labellingfields/*");

                                if (themeLink == null || themeLink.Target == null || fieldNodes == null || fieldNodes.Count == 0)
                                {
                                    continue;
                                }

                                var labeling = new LabelingDTO()
                                {
                                    Id = labelingNode.Url,
                                    Name = labelingNode.Name,
                                    LayerId = themeLink.Target.Id,
                                    Fields = fieldNodes.Select(f => new LabelingDTO.Field()
                                    {
                                        Name = f.LoadString("fieldname"),
                                        Alias = f.Name
                                    }).ToArray()
                                };

                                labelingThemes.Add(new AuthObject<LabelingDTO>(labeling, CmsDocument.GetAuthNodeFast(cms, labelingNode.NodeXPath)));
                            }

                            if (labelingThemes.Count > 0)
                            {
                                if (_labeling.ContainsKey(serviceUrlKey))
                                {
                                    throw new Exception("Key for labeling already exists: " + serviceUrlKey);
                                }

                                _labeling.Add(serviceUrlKey, labelingThemes.ToArray());
                            }

                            #endregion

                            #region SnapSchemes

                            List<AuthObject<SnapSchemaDTO>> snapSchemes = new List<AuthObject<SnapSchemaDTO>>();
                            foreach (var snapSchemaNode in cmsHlp.ServiceSnapSchemes(serviceNode.NodeXPath))
                            {
                                var snapSchema = new SnapSchemaDTO()
                                {
                                    Id = snapSchemaNode.Url,
                                    Name = snapSchemaNode.Name,
                                    MinScale = (int)snapSchemaNode.Load("minscale", 3000),
                                    LayerIds = cms.SelectNodes(null, snapSchemaNode.NodeXPath + "/*").Select(l => l is CmsLink && ((CmsLink)l).Target != null ? ((CmsLink)l).Target.Id : null)
                                    .Where(id => id != null).ToArray()
                                };
                                snapSchemes.Add(new AuthObject<SnapSchemaDTO>(snapSchema, CmsDocument.GetAuthNodeFast(cms, snapSchemaNode.NodeXPath)));
                            }

                            if (snapSchemes.Count > 0)
                            {
                                if (_snapSchemes.ContainsKey(serviceUrlKey))
                                {
                                    throw new Exception("Key for snapping already exists: " + serviceUrlKey);
                                }

                                _snapSchemes.Add(serviceUrlKey, snapSchemes.ToArray());
                            }

                            #endregion

                            #region Service (Ogc) Extent

                            if (serviceGdiPropierties != null)
                            {
                                string ogcExtentUrl = serviceGdiPropierties.LoadString("serviceextenturl");
                                if (!String.IsNullOrWhiteSpace(ogcExtentUrl) && service is IExportableOgcService)
                                {
                                    //var extent = Cache.GetExtent(String.IsNullOrWhiteSpace(cmsName) ? ogcExtentUrl : ogcExtentUrl + "@" + cmsName);
                                    var extentUrl = String.IsNullOrWhiteSpace(cmsName) ? ogcExtentUrl : ogcExtentUrl + "@" + cmsName;
                                    if (this._extents != null && this._extents.ContainsKey(extentUrl))
                                    {
                                        var extent = this._extents[extentUrl];
                                        if (extent != null && extent.epsg != null)
                                        {
                                            using (var transformer = new E.Standard.WebMapping.Core.Geometry.GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, (int)extent.epsg, 4326))
                                            {
                                                var envelope = new E.Standard.WebMapping.Core.Geometry.Envelope(extent.extent[0], extent.extent[1], extent.extent[2], extent.extent[3]);
                                                transformer.Transform(envelope);
                                                envelope.SrsId = 4326;
                                                ((IExportableOgcService)service).OgcEnvelope = envelope;
                                            }
                                        }
                                    }
                                }
                            }

                            #endregion

                            service.Url = serviceUrlKey;

                            if (_mapServices.ContainsKey(serviceUrlKey))
                            {
                                throw new Exception("Key for services already exists: " + serviceUrlKey);
                            }

                            _mapServices.Add(serviceUrlKey, new AuthObject<IMapService>(service, CmsDocument.GetAuthNodeFast(cms, serviceNode.NodeXPath)));

                            //if (_serviceNodes.ContainsKey(serviceUrlKey))
                            //{
                            //    throw new Exception("Key for serviceNodes already exists: " + serviceUrlKey);
                            //}

                            //_serviceNodes.Add(serviceUrlKey, serviceNode);

                            if (service is IExportableOgcService)
                            {
                                CmsDocument.AuthNode wmsExportAuthNode = CmsDocument.GetPropertyAuthNodeFast(cms, serviceGdiPropierties.ParentNode.NodeXPath + "/.linktemplate", "exportwms");
                                if (wmsExportAuthNode != null)
                                {
                                    _authBoolProperties.Add(new AuthProperty<bool>(serviceUrlKey + "::exportwms", ((IExportableOgcService)service).ExportWms, false, wmsExportAuthNode));
                                }
                            }
                        }

                        #endregion

                        #region Search Services

                        foreach (CmsNode searchServiceNode in cmsHlp.SearchServiceNodes())
                        {
                            string serviceUrlKey = String.IsNullOrWhiteSpace(cmsName) ? searchServiceNode.Url : searchServiceNode.Url + "@" + cmsName;

                            SearchServiceTarget target = (SearchServiceTarget)searchServiceNode.Load("target", (int)SearchServiceTarget.Solr);
                            ISearchService searchService = null;
                            switch (target)
                            {
                                case SearchServiceTarget.Solr:
                                    searchService = new WebMapping.GeoServices.SearchService.SolrSearchService(searchServiceNode);
                                    searchService.Id = serviceUrlKey;
                                    break;
                                case SearchServiceTarget.ElasticSearch_5:
                                    searchService = new WebMapping.GeoServices.SearchService.ElasticSearch5Service(searchServiceNode);
                                    searchService.Id = serviceUrlKey;
                                    break;
                                case SearchServiceTarget.ElasticSearch_7:
                                    searchService = new WebMapping.GeoServices.SearchService.ElasticSearch7Service(searchServiceNode);
                                    searchService.Id = serviceUrlKey;
                                    break;
                                case SearchServiceTarget.SolrMeta:
                                    searchService = new WebMapping.GeoServices.SearchService.SolrMetaSearchService(map, searchServiceNode);
                                    searchService.Id = serviceUrlKey;
                                    break;
                                case SearchServiceTarget.PassThrough:
                                    searchService = new WebMapping.GeoServices.SearchService.PassThroughSearchService(searchServiceNode);
                                    searchService.Id = serviceUrlKey;
                                    break;
                                case SearchServiceTarget.LuceneServerNET:
                                case SearchServiceTarget.LuceneServerNET_Phonetic:
                                    searchService = new WebMapping.GeoServices.LuceneServer.SearchService(searchServiceNode,
                                        target == SearchServiceTarget.LuceneServerNET_Phonetic);
                                    searchService.Id = serviceUrlKey;
                                    break;
                            }

                            if (searchService != null)
                            {
                                searchService.CopyrightId = searchServiceNode.LoadString("copyright");
                                if (!String.IsNullOrEmpty(searchService.CopyrightId) && !String.IsNullOrEmpty(cmsName))
                                {
                                    searchService.CopyrightId = $"{searchService.CopyrightId}@{cmsName}";
                                }

                                _searchServices.Add(serviceUrlKey, new AuthObject<ISearchService>(searchService, CmsDocument.GetAuthNodeFast(cms, searchServiceNode.NodeXPath)));
                            }
                        }

                        #endregion

                        #region GDI 

                        #region Tools Config

                        try
                        {
                            E.Standard.WebGIS.Tools.PluginManager pman = new E.Standard.WebGIS.Tools.PluginManager(ApiGlobals.AppPluginsPath);

                            foreach (var buttonType in pman.ApiButtonTypes)
                            {
                                foreach (var attribute in buttonType.GetCustomAttributes(true))
                                {
                                    if (attribute is ToolCmsConfigParameterAttribute)
                                    {
                                        object val = cmsHlp.GdiToolValue(((ToolCmsConfigParameterAttribute)attribute).CmsParameterName, ((ToolCmsConfigParameterAttribute)attribute).ValueType, true);
                                        if (val == null)
                                        {
                                            continue;
                                        }

                                        _toolConfig.Add(String.IsNullOrWhiteSpace(cmsName) ? ((ToolCmsConfigParameterAttribute)attribute).CmsParameterName : ((ToolCmsConfigParameterAttribute)attribute).CmsParameterName + "@" + cmsName, val);
                                    }
                                }
                            }
                        }
                        catch (Exception /*ex*/)
                        {

                        }

                        #endregion

                        #region Print 

                        foreach (string cmsGdiScheme in cmsGdiSchemes)
                        {
                            foreach (var layoutNode in cmsHlp.GetGdiPrintLayouts(cmsGdiScheme))
                            {
                                PrintLayoutDTO layout = new PrintLayoutDTO();

                                layout.Id = String.IsNullOrWhiteSpace(cmsName) ? layoutNode.Url : layoutNode.Url + "@" + cmsName;
                                layout.Name = layoutNode.Name;
                                layout.LayoutFile = layoutNode.LoadString("layoutfile");
                                layout.LayoutParameters = layoutNode.LoadString("parameters");

                                if (!_printLayouts.ContainsKey(cacheService.GdiCustomSchemeKey(layout.Id, cmsGdiScheme)))
                                {
                                    _printLayouts.Add(cacheService.GdiCustomSchemeKey(layout.Id, cmsGdiScheme), new AuthObject<PrintLayoutDTO>(layout, CmsDocument.GetAuthNodeFast(cms, layoutNode.NodeXPath)));
                                }
                            }

                            var printFormatsNode = cmsHlp.GEtGdiPrintFormats(cmsGdiScheme);
                            if (printFormatsNode != null)
                            {
                                if (!_printFormats.ContainsKey(cacheService.GdiCustomSchemeKey(String.Empty, cmsGdiScheme)))
                                {
                                    _printFormats.Add(cacheService.GdiCustomSchemeKey(String.Empty, cmsGdiScheme), new List<AuthObject<PrintFormatDTO>>());
                                }

                                foreach (E.Standard.WebMapping.GeoServices.Print.PageSize pageSize in Enum.GetValues(typeof(E.Standard.WebMapping.GeoServices.Print.PageSize)))
                                {
                                    if (pageSize.ToString().Contains("_"))
                                    {
                                        if ((bool)printFormatsNode.Load(pageSize.ToString().ToLower().Replace("_", ""), false) == true)
                                        {
                                            _printFormats[cacheService.GdiCustomSchemeKey(String.Empty, cmsGdiScheme)].Add(new AuthObject<PrintFormatDTO>(new PrintFormatDTO()
                                            {
                                                Size = (PageSize)pageSize,
                                                Orientation = PageOrientation.Fixed
                                            }, CmsDocument.GetAuthNodeFast(cms, printFormatsNode.NodeXPath)));
                                        }
                                    }
                                    else
                                    {
                                        if ((bool)printFormatsNode.Load(pageSize.ToString().ToLower() + "port", false) == true)
                                        {
                                            _printFormats[cacheService.GdiCustomSchemeKey(String.Empty, cmsGdiScheme)].Add(new AuthObject<PrintFormatDTO>(new PrintFormatDTO()
                                            {
                                                Size = (PageSize)pageSize,
                                                Orientation = (PageOrientation)E.Standard.WebMapping.GeoServices.Print.PageOrientation.Portrait
                                            }, CmsDocument.GetAuthNodeFast(cms, printFormatsNode.NodeXPath)));
                                        }
                                        if ((bool)printFormatsNode.Load(pageSize.ToString().ToLower() + "land", false) == true)
                                        {
                                            _printFormats[cacheService.GdiCustomSchemeKey(String.Empty, cmsGdiScheme)].Add(new AuthObject<PrintFormatDTO>(new PrintFormatDTO()
                                            {
                                                Size = (PageSize)pageSize,
                                                Orientation = (PageOrientation)E.Standard.WebMapping.GeoServices.Print.PageOrientation.Landscape
                                            }, CmsDocument.GetAuthNodeFast(cms, printFormatsNode.NodeXPath)));
                                        }
                                    }
                                }
                            }
                        }

                        #endregion

                        #region Copyright

                        foreach (var copyrightNode in cms.SelectNodes(null, "etc/copyright/*"))
                        {
                            _copyright.Add(new CopyrightInfoDTO()
                            {
                                Id = String.IsNullOrWhiteSpace(cmsName) ? copyrightNode.Url : copyrightNode.Url + "@" + cmsName,
                                Copyright = copyrightNode.LoadString("copyright"),
                                CopyrightLink = copyrightNode.LoadString("copyrightlink"),
                                CopyrighLinkText = copyrightNode.LoadString("copyrightlinktext"),
                                Advice = copyrightNode.LoadString("advice"),
                                Logo = copyrightNode.LoadString("logo"),
                                LogoSize = new[] { (int)copyrightNode.Load("logowidth", 0), (int)copyrightNode.Load("logoheight", 0) }
                            });
                        }

                        #endregion

                        #region Add Service Containers

                        foreach (string cmsGdiScheme in cmsGdiSchemes)
                        {
                            foreach (CmsLink serviceLink in cmsHlp.GetGdiAddableServices(cmsGdiScheme))
                            {
                                if (serviceLink.Target == null)
                                {
                                    continue;
                                }

                                var containerNode = serviceLink.ParentNode;

                                string serviceUrlKey = String.IsNullOrWhiteSpace(cmsName) ? serviceLink.Target.Url : serviceLink.Target.Url + "@" + cmsName;
                                if (!_serviceContainers.ContainsKey(cacheService.GdiCustomSchemeKey(serviceUrlKey, cmsGdiScheme)))
                                {
                                    _serviceContainers.Add(cacheService.GdiCustomSchemeKey(serviceUrlKey, cmsGdiScheme), containerNode.Name);
                                }
                            }
                        }

                        #endregion

                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                _isCorrupt = true;
#if DEBUG
                this.ErrorMessage = ex.Message + $"{System.Environment.NewLine} {ex.StackTrace}";
#else
                this.ErrorMessage = ex.Message + (ex is NullReferenceException ? $"{System.Environment.NewLine} {ex.StackTrace}" : String.Empty);
#endif
            }
        }
    }

    public string ErrorMessage { get; set; }

    public string Name { get; private set; }

    private string _displayName = null;
    public string DisplayName
    {
        get
        {
            var displayName = _displayName ?? this.Name;

            if (String.IsNullOrWhiteSpace(displayName))
            {
                return "CMS";
            }

            return displayName;
        }
        set
        {
            _displayName = String.IsNullOrWhiteSpace(value) ? null : value;
        }
    }

    public bool IsCustom { get; private set; }

    public void Clear()
    {
        _mapServices.Clear();
        //_serviceNodes.Clear();
        _extents.Clear();
        _presentations.Clear();
        _queries.Clear();
        _searchServices.ClearAndDispose();
        _editThemes.Clear();
        _visFilters.Clear();
        _labeling.Clear();
        _snapSchemes.Clear();
        _toolConfig.Clear();
        _authBoolProperties.Clear();
        _authStringProperties.Clear();
        _printLayouts.Clear();
        _printFormats.Clear();
        _chainageThemes.Clear();
        _copyright.Clear();
        _serviceContainers.Clear();
        _layerProperties.Clear();
        _layerAuthVisibility.Clear();
        _tocName.Clear();

        if (_isInitialized)
        {
            // Do not log. If calling task ist finished maybe the Console Handle ist invalid.
            //($"Cache item {this.Name} cleared").LogLine();
        }

        _isInitialized = false;
    }

    public bool IsEmpty()
    {
        return _mapServices.Count() == 0 && _extents.Count() == 0;
    }

    public bool IsCorrupt => _isCorrupt;
}
