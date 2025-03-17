using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Extensions;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E.Standard.WebGIS.CMS;

public class CmsHlp
{
    private CmsDocument _cms = null;
    private IMap _map = null;
    private CmsDocument.UserIdentification _ui = null;

    public CmsHlp(CmsDocument cms, IMap map)
    {
        _cms = cms;
        _map = map;
        if (_map != null)
        {
            _ui = _map.Environment.UserValue(webgisConst.UserIdentification, null) as CmsDocument.UserIdentification;
        }
    }

    public CmsHlp(CmsDocument cms, CmsDocument.UserIdentification ui, IMap map)
    {
        _cms = cms;
        _ui = ui;
        _map = map;
    }

    public CmsDocument CmsDocument
    {
        get { return _cms; }
    }

    public CmsDocument.UserIdentification UserIdentification
    {
        get { return _ui; }
        set { _ui = value; }
    }

    #region General

    public ILayer GetLayer(CmsNode theme)
    {
        if (_map == null || theme == null)
        {
            return null;
        }

        string guid = ServiceIdFromThemeNode(theme);

        return _map.LayerById(guid + ":" + theme.Id);
    }

    public CmsNode GetQueryLayerNode(CmsNode query)
    {
        if (query == null)
        {
            return null;
        }

        CmsLink queryThemeLink = _cms.SelectSingleNode(_ui, query.NodeXPath + "/querytheme/*") as CmsLink;
        if (queryThemeLink == null)
        {
            return null;
        }

        return queryThemeLink.Target;
    }

    public string GetQueryLayerGlobalId(CmsNode query)
    {
        try
        {
            if (query == null)
            {
                return String.Empty;
            }

            CmsLink queryThemeLink = _cms.SelectSingleNode(_ui, query.NodeXPath + "/querytheme/*") as CmsLink;
            if (queryThemeLink == null || queryThemeLink.Target == null)
            {
                return String.Empty;
            }

            return query.ParentNode.ParentNode.LoadString("guid") + ":" + queryThemeLink.Target.Id;
        }
        catch
        {
            return String.Empty;
        }
    }

    public CmsNodeCollection GetAssociatedTocLayerLinks(CmsNode queryNode)
    {
        CmsNodeCollection ret = _cms.SelectNodes(_ui, queryNode.NodeXPath + "/querytoctheme/*");
        return ret;
    }

    public string ServiceIdFromThemeNode(CmsNode theme)
    {
        if (theme == null)
        {
            return String.Empty;
        }

        CmsNode service = theme.ParentNode.ParentNode;
        string guid = ((string)service.Load("guid", String.Empty)).ToLower();

        return guid;
    }


    public CmsNodeCollection GetLayerQueryLinks()
    {
        if (_cms == null || _map == null)
        {
            return new CmsNodeCollection();
        }

        return _cms.SelectNodes(_ui, "maps/" + _map.Name + "/queries/*/*");
    }

    public CmsLink GetTocElement(CmsNode serviceNode, string tocname, string layerId)
    {
        string xPath = serviceNode.NodeXPath + "/tocs/" + tocname;
        return GetTocElement(xPath, layerId);
    }

    private CmsLink GetTocElement(string xPath, string layerId)
    {
        foreach (CmsNode node in _cms.SelectNodes(_ui, xPath + "/*"))
        {
            if (node == null)
            {
                continue;
            }

            if (!(node is CmsLink))
            {
                CmsLink ret = GetTocElement(node.NodeXPath, layerId);
                if (ret != null)
                {
                    return ret;
                }

                continue;
            }

            CmsLink tocElement = (CmsLink)node;
            if (tocElement.Target == null)
            {
                continue;
            }

            if (tocElement.Target.Id == layerId)
            {
                return tocElement;
            }
        }
        return null;
    }

    public CmsLink[] GetTocElements(CmsNode serviceNode, string tocName)
    {
        List<CmsLink> tocElements = new List<CmsLink>();
        string xPath = serviceNode.NodeXPath + "/tocs/" + tocName;
        GetTocElements(xPath, tocElements);
        return tocElements.ToArray();
    }

    private void GetTocElements(string xPath, List<CmsLink> tocElements)
    {
        foreach (CmsNode node in _cms.SelectNodes(_ui, xPath + "/*"))
        {
            if (node == null)
            {
                continue;
            }

            if (node is CmsLink)
            {
                tocElements.Add((CmsLink)node);
            }
            else
            {
                GetTocElements(node.NodeXPath, tocElements);
            }
        }
    }

    public CmsNode GetTocParentNode(CmsNode serviceNode, string tocname, CmsNode tocElementNode)
    {
        if (tocElementNode == null)
        {
            return null;
        }

        string xPath = (serviceNode.NodeXPath + "/tocs/" + tocname).ToLower();

        var parentNode = tocElementNode.ParentNode;
        if (!parentNode.NodeXPath.ToLower().StartsWith(xPath))
        {
            return null;
        }

        if (parentNode.NodeXPath.Length <= xPath.Length + 1)
        {
            return null;
        }

        return parentNode;
    }

    public string GetTocFullName(CmsNode serviceNode, string tocName, CmsNode tocElementNode)
    {
        if (tocElementNode == null)
        {
            return String.Empty;
        }

        string name = tocElementNode.Name;

        while ((tocElementNode = GetTocParentNode(serviceNode, tocName, tocElementNode)) != null)
        {
            name = tocElementNode.Name + "\\" + name;
        }

        return name;
    }

    public CmsNodeCollection SelectNodes(string xPath)
    {
        if (_cms == null)
        {
            return new CmsNodeCollection();
        }

        return _cms.SelectNodes(_ui, xPath);
    }

    public CmsNodeCollection GetServiceQueries(CmsNode serviceNode)
    {
        if (serviceNode == null)
        {
            return new CmsNodeCollection();
        }

        return _cms.SelectNodes(_ui, serviceNode.NodeXPath + "/queries/*");
    }

    public CmsNodeCollection GetQuerySearchItems(CmsNode queryNode)
    {
        if (queryNode == null)
        {
            return new CmsNodeCollection();
        }

        return _cms.SelectNodes(_ui, queryNode.NodeXPath + "/searchitems/*");
    }

    #endregion

    #region Presentations

    public CmsLink[] Presentations
    {
        get
        {
            List<CmsLink> links = new List<CmsLink>();
            foreach (CmsNode n in _cms.SelectNodes(_ui, "maps/" + _map.Name + "/presentations/*"))
            {
                if (n is CmsLink)
                {
                    links.Add((CmsLink)n);
                }
            }
            foreach (CmsLink l in _cms.SelectNodes(_ui, "maps/" + _map.Name + "/presentations/*/*"))
            {
                links.Add(l);
            }
            return links.ToArray();
        }
    }

    public CmsNode[] CmsServicePresentations(IMapService service)
    {
        List<CmsNode> ret = new List<CmsNode>();
        CmsNode serviceNode = this.ServiceNode(service.Url);
        if (serviceNode != null)
        {
            foreach (CmsNode n in _cms.SelectNodes(_ui, serviceNode.NodeXPath + "/presentations/*"))
            {
                ret.Add(n);
            }
        }
        return ret.ToArray();
    }

    public CmsNodeCollection ServicePresentations(IMapService service, bool mapMode)
    {
        CmsNodeCollection ret = new CmsNodeCollection();
        if (mapMode)
        {
            foreach (CmsLink preslink in Presentations)
            {
                if (preslink.Target == null)
                {
                    continue;
                }

                if ((string)preslink.Target.ParentNode.ParentNode.Load("guid", String.Empty) == service.ID)
                {
                    ret.Add(preslink);
                }
            }
        }
        else
        {
            foreach (CmsNode presNode in CmsServicePresentations(service))
            {
                ret.Add(presNode);
            }
        }
        return ret;
    }

    private class CheckalbePresentationGroup
    {
        public string Url;
        public CmsLink[] Links;

        public CheckalbePresentationGroup(string url, CmsLink[] links)
        {
            Url = url;
            Links = links;
        }
    }

    #region GDI Presentations

    public GdiPresentation[] GetServiceGdiPresentations(string gdiSchema, string serviceNodePath)
    {
        serviceNodePath = serviceNodePath.ToLower();

        List<GdiPresentation> servicePresenations = new List<GdiPresentation>();

        int containerIndex = 1;
        string path = "gdi/presentations/*";
        if (!String.IsNullOrWhiteSpace(gdiSchema))
        {
            path = "gdi/gdi-custom/" + gdiSchema + "/presentations/*";
        }

        foreach (CmsNode containerNode in _cms.SelectNodes(_ui, path))
        {
            int groupIndex = 0;
            foreach (CmsNode groupNode in _cms.SelectNodes(_ui, containerNode.NodeXPath + "/*"))
            {
                groupIndex++;

                if (groupNode is CmsLink)
                {

                    CmsLink presentationLink = (CmsLink)groupNode;
                    if (presentationLink.Target == null || !presentationLink.Target.NodeXPath.ToLower().StartsWith(serviceNodePath))
                    {
                        continue;
                    }

                    GdiPresentation pres = GdiPresenationFromCmsNode(presentationLink, null, containerNode);
                    pres.ContainerIndex = containerIndex;
                    pres.GroupIndex = pres.Index = groupIndex;
                    pres.Visible = groupNode.Visible;
                    pres.ClientVisibility = (ClientVisibility)groupNode.Load("client_visibility", (int)ClientVisibility.Any);

                    servicePresenations.Add(pres);
                }
                else
                {
                    int presentationIndex = 0;
                    foreach (CmsNode presentationNode in _cms.SelectNodes(_ui, groupNode.NodeXPath + "/*"))
                    {
                        presentationIndex++;

                        if (!(presentationNode is CmsLink))
                        {
                            continue;
                        }

                        CmsLink presentationLink = (CmsLink)presentationNode;
                        if (presentationLink.Target == null || !presentationLink.Target.NodeXPath.ToLower().StartsWith(serviceNodePath))
                        {
                            continue;
                        }

                        GdiPresentation pres = GdiPresenationFromCmsNode(presentationLink, groupNode, containerNode);
                        pres.ContainerIndex = containerIndex;
                        pres.GroupIndex = groupIndex;
                        pres.Index = presentationIndex;
                        pres.Visible = presentationNode.Visible;
                        pres.ClientVisibility = (ClientVisibility)presentationNode.Load("client_visibility", (int)ClientVisibility.Any);
                        pres.UIGroupName = presentationLink.LoadString("ui_group");

                        servicePresenations.Add(pres);
                    }
                }
            }

            containerIndex++;
        }

        return servicePresenations.ToArray();
    }

    private GdiPresentation GdiPresenationFromCmsNode(CmsLink link, CmsNode groupNode, CmsNode containerNode)
    {
        string[] gdi_vis_with_on_of_services = null;
        if (!String.IsNullOrWhiteSpace(link.LoadString("vis_with_on_of_services")))
        {
            gdi_vis_with_on_of_services =
            link.LoadString("vis_with_on_of_services")?.Replace(" ", "").Replace(";", ",").Split(',');
        }

        string groupMetadata = groupNode != null ? groupNode.LoadString("metadata") : String.Empty;
        GdiPresentation presentation = new GdiPresentation()
        {
            Name = link.Target.Name,
            Url = link.Target.Url,
            ServiceUrl = link.Target.ParentNode.ParentNode.Url,
            Layers = (string)link.Target.Load("layers", String.Empty),
            NodeXPath = link.Target.NodeXPath,
            CheckMode = (PresentationLinkCheckMode)link.Load("checkmode", (int)PresentationLinkCheckMode.Button),
            Affecting = (PresentationAffecting)link.Load("affecting", (int)PresentationAffecting.Service),
            VisibleWithService = (bool)link.Load("vis_with_service", true),
            VisibleWithOnOfServices = gdi_vis_with_on_of_services,
            Container = containerNode != null ? containerNode.Name : "Allgemein",
            ContainerUrl = containerNode != null ? containerNode.Url : "_general",
            MetadataLink = link.LoadString("metadata"),
            MetadataTarget = (BrowserWindowTarget2)link.Load("metadata_target", (int)BrowserWindowTarget2.tab),
            MetadataTitle = link.LoadString("metadata_title"),
            MetadataButtonStyle = (MetadataButtonStyle)link.Load("metadata_button_style", (int)MetadataButtonStyle.i_button),
            IsContainerDefault = (bool)link.Load("containerdefault", false),
            Group = groupNode != null ? groupNode.Name : String.Empty,
            GroupStyle = groupNode != null ? (PresentationGroupStyle)groupNode.Load("checkmode", (int)PresentationGroupStyle.Button) : PresentationGroupStyle.Dropdown,
            GroupMetadataLink = groupMetadata,
            GroupMetadataTarget = String.IsNullOrEmpty(groupMetadata) ? null : (BrowserWindowTarget2)groupNode.Load("metadata_target", (int)BrowserWindowTarget2.tab),
            GroupMetadataTitle = String.IsNullOrEmpty(groupMetadata) ? null : groupNode.LoadString("metadata_title"),
            GroupMetadataButtonStyle = String.IsNullOrEmpty(groupMetadata) ? null : (MetadataButtonStyle)groupNode.Load("metadata_button_style", (int)MetadataButtonStyle.i_button),
            //AllowAsDynamicMarkers = (bool)link.Load("showdynamicmarkers", false)  
        };
        return presentation;
    }

    #region SubClasses

    public class GdiPresentation
    {
        public GdiPresentation()
        {
            this.Visible = true;
        }
        public string Name { get; set; }
        public string Url { get; set; }
        public string ServiceUrl { get; set; }
        public string Layers { get; set; }
        public string NodeXPath { get; set; }
        public PresentationLinkCheckMode CheckMode { get; set; }
        public PresentationAffecting Affecting { get; set; }
        public ClientVisibility ClientVisibility { get; set; }
        public bool VisibleWithService { get; set; }
        public string[] VisibleWithOnOfServices { get; set; }
        public string Container { get; set; }
        public string ContainerUrl { get; set; }
        public bool IsContainerDefault { get; set; }
        public string Group { get; set; }
        public PresentationGroupStyle GroupStyle { get; set; }

        public string MetadataLink { get; set; }
        public BrowserWindowTarget2 MetadataTarget { get; set; }
        public string MetadataTitle { get; set; }
        public MetadataButtonStyle MetadataButtonStyle { get; set; }

        //public bool AllowAsDynamicMarkers { get; set; }

        public int ContainerIndex { get; set; }
        public int GroupIndex { get; set; }
        public int Index { get; set; }

        public string GroupMetadataLink { get; set; }
        public BrowserWindowTarget2? GroupMetadataTarget { get; set; }
        public string GroupMetadataTitle { get; set; }
        public MetadataButtonStyle? GroupMetadataButtonStyle { get; set; }

        public string UIGroupName { get; set; }

        public bool Visible { get; set; }
    }

    #endregion

    #endregion

    #endregion

    #region GDI Print

    public CmsNodeCollection GetGdiPrintLayouts(string gdiSchema)
    {
        string path = "gdi/print/layouts/*";
        if (!String.IsNullOrWhiteSpace(gdiSchema))
        {
            path = "gdi/gdi-custom/" + gdiSchema + "/print/layouts/*";
        }

        return _cms.SelectNodes(_ui, path);
    }

    public CmsNode GEtGdiPrintFormats(string gdiSchema)
    {
        string path = "gdi/print/formats";
        if (!String.IsNullOrWhiteSpace(gdiSchema))
        {
            path = "gdi/gdi-custom/" + gdiSchema + "/print/formats";
        }

        return _cms.SelectSingleNode(_ui, path);
    }

    #endregion

    #region GDI Add Service Categories

    public CmsNodeCollection GetGdiAddableServices(string gdiSchema)
    {
        string path = "gdi/addservices/*/*";
        if (!String.IsNullOrWhiteSpace(gdiSchema))
        {
            path = "gdi/gdi-custom/" + gdiSchema + "/addservices/*/*";
        }

        return _cms.SelectNodes(_ui, path);
    }

    #endregion

    #region Query

    public CmsNodeCollection TableColumns(CmsNode queryNode)
    {
        if (_cms == null || queryNode == null)
        {
            new CmsNodeCollection();
        }

        return _cms.SelectNodes(_ui, queryNode.NodeXPath + "/tablecolumns/*");
    }

    public CmsNodeCollection GetExportTableFormats(CmsNode queryNode)
    {
        if (_cms == null || queryNode == null)
        {
            new CmsNodeCollection();
        }

        return _cms.SelectNodes(_ui, queryNode.NodeXPath + "/exportformats/*");
    }

    public CmsNodeCollection GetCmsQueryFeatureTransfers(CmsNode queryNode)
    {
        return _cms.SelectNodes(_ui, $"{queryNode.NodeXPath}/featuretransfers/*");
    }

    public CmsNodeCollection GetCmsQueryFeatureTransferTargets(CmsNode featureTransferNode)
    {
        return _cms.SelectNodes(_ui, $"{featureTransferNode.NodeXPath}/editthemes/*");
    }

    public CmsNodeCollection GetCmsQueryFeatureTransferFieldSetters(CmsNode featureTransferNode)
    {
        return _cms.SelectNodes(_ui, $"{featureTransferNode.NodeXPath}/fieldsetters/*");
    }

    #endregion

    #region Themes

    public ServiceTheme[] GetServiceThemes(string serviceUrl)
    {
        CmsNode serviceNode = ServiceNode(serviceUrl);

        if (serviceNode == null)
        {
            throw new Exception("Unknwon Service");
        }

        return ServiceThemes(_cms, serviceNode, _ui);
    }

    static public ServiceTheme[] ServiceThemes(CmsDocument cms, CmsNode serviceNode, CmsDocument.UserIdentification ui = null)
    {
        List<ServiceTheme> serviceThemes = new List<ServiceTheme>();
        if (cms != null && serviceNode != null)
        {
            foreach (var cmsNode in cms.SelectNodes(ui, serviceNode.NodeXPath + "/themes/*"))
            {
                serviceThemes.Add(new ServiceTheme()
                {
                    Id = cmsNode.Id,
                    Name = cmsNode.Name
                });
            }
        }

        return serviceThemes.ToArray();
    }

    public ServiceTheme[] ServiceThemes(CmsNode serviceNode)
    {
        return CmsHlp.ServiceThemes(_cms, serviceNode, null);
    }

    #endregion

    #region Editing

    private CmsLink GdiEditThemeLink(CmsNode parent, ILayer layer)
    {
        foreach (CmsNode node in _cms.SelectNodes(_ui, parent.NodeXPath + "/*"))
        {
            if (node is CmsLink)
            {
                CmsArrayItems items = _cms.SelectArray(_ui, node.NodeXPath, "gdiproperties");
                if (items.Count > 0 && GetLayer(((CmsLink)node).Target) == layer)
                {
                    return (CmsLink)node;
                }
            }
            else
            {
                CmsLink ret = GdiEditThemeLink(node, layer);
                if (ret != null)
                {
                    return ret;
                }
            }
        }
        return null;
    }

    public CmsArrayItems GetTocElementGdiProperties(CmsNode tocElement)
    {
        CmsArrayItems items = _cms.SelectArray(_ui, tocElement.NodeXPath, "gdiproperties");
        return items;
    }

    public CmsNodeCollection CmsEditingThemes(CmsNode serviceNode)
    {
        CmsNodeCollection gdiEditingThemes = new CmsNodeCollection();

        String path = serviceNode.NodeXPath + "/editing/*";

        foreach (CmsNode gdiEditingTheme in _cms.SelectNodes(_ui, path))
        {
            gdiEditingThemes.Add(gdiEditingTheme);
        }

        return gdiEditingThemes;
    }

    public CmsNode GetCmsEditingThemeLayerNode(CmsNode editingTheme)
    {
        if (editingTheme == null)
        {
            return null;
        }

        CmsLink editingThemeLink = _cms.SelectSingleNode(_ui, editingTheme.NodeXPath + "/editingtheme/*") as CmsLink;
        if (editingThemeLink == null)
        {
            return null;
        }

        return editingThemeLink.Target;
    }

    public CmsNodeCollection GetCmsEditingThemeFieldNodes(CmsNode editingTheme)
    {
        CmsNodeCollection fieldNodes = new CmsNodeCollection();

        foreach (var categoryNode in _cms.SelectNodes(_ui, editingTheme.NodeXPath + "/editingfields/*"))
        {
            fieldNodes.AddRange(_cms.SelectNodes(_ui, categoryNode.NodeXPath + "/*"));
        }

        return fieldNodes;
    }

    public CmsNodeCollection GetCmsEditingCategoryNodes(CmsNode editingTheme)
    {
        return _cms.SelectNodes(_ui, editingTheme.NodeXPath + "/editingfields/*");
    }

    public CmsNodeCollection GetCmsEditingSnappingSchemeLinks(CmsNode editingTheme)
    {
        return _cms.SelectNodes(_ui, editingTheme.NodeXPath + "/snapping/*");
    }

    public CmsNodeCollection GetCmsEditingMaskValidationNodes(CmsNode editingTheme)
    {
        return _cms.SelectNodes(_ui, editingTheme.NodeXPath + "/validations/*");
    }

    #endregion

    #region VisFilter

    public static string ReplaceFilterKeys(RequestParameters requestParameters, CmsDocument.UserIdentification ui, string filter, string startingBracket = "[", string endingBracket = "]")
    {
        return ReplaceFilterKeys(null, requestParameters, ui, filter, startingBracket: startingBracket, endingBracket: endingBracket);
    }

    public static string ReplaceFilterKeys(IMap map,
                                           RequestParameters requestParameters,
                                           CmsDocument.UserIdentification ui,
                                           string filter,
                                           string startingBracket = "[", string endingBracket = "]")
    {
        requestParameters = requestParameters ?? new RequestParameters();

        string[] keys = Globals.KeyParameters(filter, startingBracket: startingBracket, endingBracket: endingBracket);
        if (keys != null)
        {
            foreach (string key in keys)
            {
                if (key.ToLower().StartsWith("url-parameter:") && requestParameters != null)
                {
                    try
                    {
                        string urlParameter = key.Substring(14, key.Length - 14);
                        string val = requestParameters[urlParameter];
                        if (val != null)
                        {
                            filter = filter.Replace(startingBracket + key + endingBracket, val);
                        }
                    }
                    catch { }
                }

                if (map != null)
                {
                    if (key.ToLower() == "sessionid")
                    {
                        filter = filter.Replace(startingBracket + key + endingBracket, map.Environment.UserString(webgisConst.SessionId));
                    }
                    if (key.ToLower() == "username")
                    {
                        filter = filter.Replace(startingBracket + key + endingBracket, (map.Environment.UserString(webgisConst.UserName)).RemoveUserIdentificationNamespace());
                    }
                    if (key.ToLower() == "username_short")
                    {
                        filter = filter.Replace(startingBracket + key + endingBracket, (map.Environment.UserString(webgisConst.UserName)).RemoveUserIdentificationNamespace()
                                                                                                                                         .RemoveUserIdentificationDomain());
                    }
                    if (key.ToLower() == "username_full")
                    {
                        filter = filter.Replace(startingBracket + key + endingBracket, map.Environment.UserString(webgisConst.UserName));
                    }
                    if (key.ToLower() == "username_domain")
                    {
                        filter = filter.Replace(startingBracket + key + endingBracket, map.Environment.UserString(webgisConst.UserName).UsernameDomain());
                    }
                }
                else if (ui != null)
                {
                    if (key.ToLower() == "username")
                    {
                        filter = filter.Replace(startingBracket + key + endingBracket, ui.Username.RemoveUserIdentificationNamespace());
                    }
                    if (key.ToLower() == "username_short")
                    {
                        filter = filter.Replace(startingBracket + key + endingBracket, ui.Username.RemoveUserIdentificationNamespace()
                                                                                                  .RemoveUserIdentificationDomain());
                    }
                    if (key.ToLower() == "username_full")
                    {
                        filter = filter.Replace(startingBracket + key + endingBracket, ui.Username);
                    }
                    if (key.ToLower() == "username_domain")
                    {
                        filter = filter.Replace(startingBracket + key + endingBracket, ui.Username.UsernameDomain());
                    }
                }

                if (key.ToLower().StartsWith("role-parameter:"))
                {
                    //    [role-parameter:parameter_name,filter(,logical_operator)]
                    // zB [role-parameter:gemnr,GEM_NR like '{0}%'(,OR)]
                    try
                    {
                        string[] roleParameterDef = key.Substring(15, key.Length - 15).Split(',');
                        string roleParamName = roleParameterDef[0].Trim();
                        string roleParamFilter = roleParameterDef.Length > 1 ? roleParameterDef[1].Trim() : "{0}";
                        string logicalOp = (roleParameterDef.Length > 2 ? " " + roleParameterDef[2].Trim() + " " : " OR ");
                        StringBuilder sb = new StringBuilder();
                        if (ui != null && ui.UserrolesParameters != null)
                        {
                            int paramIndex = -1;
                            if (int.TryParse(roleParamName, out paramIndex))
                            {
                                if (paramIndex > 0 && paramIndex < ui.UserrolesParameters.Length)
                                {
                                    sb.Append(String.Format(roleParamFilter, ui.UserrolesParameters[paramIndex]));
                                }
                            }
                            else
                            {
                                foreach (string userroleParam in ui.UserrolesParameters)
                                {
                                    if (userroleParam.StartsWith(roleParamName + "="))
                                    {
                                        string val = userroleParam.Substring(roleParamName.Length + 1, userroleParam.Length - roleParamName.Length - 1);
                                        if (sb.Length > 0)
                                        {
                                            sb.Append(logicalOp);
                                        }

                                        sb.Append(String.Format(roleParamFilter, val));
                                    }
                                }
                            }
                        }
                        filter = filter.Replace(startingBracket + key + endingBracket, sb.ToString());
                    }
                    catch { }
                }
                if (key.ToLower().StartsWith("role-param-name:") && ui != null && ui.UserrolesParameters != null)
                {
                    // role-param:paremeter_name
                    //zB:[role-param-name:GEM_NR]
                    string paramName = key.Substring(16, key.Length - 16);
                    StringBuilder sb = new StringBuilder();
                    foreach (string userroleParam in ui.UserrolesParameters)
                    {
                        if (userroleParam.StartsWith(paramName + "="))
                        {
                            string val = userroleParam.Substring(paramName.Length + 1, userroleParam.Length - paramName.Length - 1);
                            if (sb.Length > 0)
                            {
                                sb.Append(";");
                            }

                            sb.Append(val);
                        }
                    }
                    filter = filter.Replace(startingBracket + key + endingBracket, sb.ToString());
                }
            }
        }
        return filter;
    }

    public CmsNodeCollection GetServiceVisFilters(CmsNode serviceNode)
    {
        if (serviceNode == null)
        {
            return new CmsNodeCollection();
        }

        return _cms.SelectNodes(_ui, serviceNode.NodeXPath + "/visfilter/*");
    }

    #endregion

    #region Chainage

    public CmsNodeCollection ServiceChaingageThemes(string serviceUrl)
    {
        return _cms.SelectNodes(_ui, serviceUrl + "/chainage/*");
    }

    public CmsNodeCollection ServiceLabeling(string serviceUrl)
    {
        return _cms.SelectNodes(_ui, serviceUrl + "/labelling/*");
    }

    public CmsNodeCollection ServiceSnapSchemes(string serviceUrl)
    {
        return _cms.SelectNodes(_ui, serviceUrl + "/snapschemas/*");
    }

    #endregion

    #region GDI

    public CmsNode ServiceNodeFromXPath(string xPath)
    {
        if (String.IsNullOrEmpty(xPath))
        {
            return null;
        }

        foreach (string serviceRootPath in new string[]
        {
            "services/arcgisserver/mapserver/",
            "services/arcgisserver/tileservice/",
            "services/arcgisserver/imageserverservice/",
            "services/miscellaneous/generaltilecache/",
            "services/miscellaneous/generalvectortilecache/",
            "services/miscellaneous/servicecollection/",
            "services/ims/",
            "services/georss/",
            "services/ogc/wms/",
            "services/ogc/wmts/",
            "services/ogc/wmsc/",
            "services/ogc/wfs/",
            "services/mapservicecollections/"
        })
        {
            if (xPath.StartsWith(serviceRootPath))
            {
                int pos = xPath.IndexOf("/", serviceRootPath.Length + 1);
                string serviceUrl = pos > 0 ? xPath.Substring(serviceRootPath.Length, pos - serviceRootPath.Length) : xPath.Substring(serviceRootPath.Length);

                return _cms.SelectSingleNode(_ui, $"{serviceRootPath}/*", "url", serviceUrl);
            }
        }

        return null;
    }

    public CmsNode ServiceNode(string serviceUrl)
    {
        if (_cms == null)
        {
            return null;
        }

        CmsNode node = _cms.SelectSingleNode(_ui, "services/arcgisserver/mapserver/*", "url", serviceUrl);
        if (node != null)
        {
            return node;
        }

        #region obsolete

        node = _cms.SelectSingleNode(_ui, "services/ims/*", "url", serviceUrl);
        if (node != null)
        {
            return node;
        }

        #endregion

        node = _cms.SelectSingleNode(_ui, "services/georss/*", "url", serviceUrl);
        if (node != null)
        {
            return node;
        }

        node = _cms.SelectSingleNode(_ui, "services/ogc/wms/*", "url", serviceUrl);
        if (node != null)
        {
            return node;
        }

        node = _cms.SelectSingleNode(_ui, "services/ogc/wmts/*", "url", serviceUrl);
        if (node != null)
        {
            return node;
        }

        node = _cms.SelectSingleNode(_ui, "services/ogc/wmsc/*", "url", serviceUrl);
        if (node != null)
        {
            return node;
        }

        node = _cms.SelectSingleNode(_ui, "services/ogc/wfs/*", "url", serviceUrl);
        if (node != null)
        {
            return node;
        }

        node = _cms.SelectSingleNode(_ui, "services/arcgisserver/tileservice/*", "url", serviceUrl);
        if (node != null)
        {
            return node;
        }

        node = _cms.SelectSingleNode(_ui, "services/arcgisserver/imageserverservice/*", "url", serviceUrl);
        if (node != null)
        {
            return node;
        }

        node = _cms.SelectSingleNode(_ui, "services/arcgisserver/wmtsservice/*", "url", serviceUrl);
        if (node != null)
        {
            return node;
        }

        node = _cms.SelectSingleNode(_ui, "services/miscellaneous/generaltilecache/*", "url", serviceUrl);
        if (node != null)
        {
            return node;
        }

        node = _cms.SelectSingleNode(_ui, "services/miscellaneous/generalvectortilecache/*", "url", serviceUrl);
        if (node != null)
        {
            return node;
        }

        #region obsolete

        node = _cms.SelectSingleNode(_ui, "services/miscellaneous/servicecollection/*", "url", serviceUrl);
        if (node != null)
        {
            return node;
        }

        #endregion

        node = _cms.SelectSingleNode(_ui, "services/mapservicecollections/*", "url", serviceUrl);
        if (node != null)
        {
            return node;
        }

        return null;
    }
    public CmsNode ServiceGdiPropertiesNode(CmsNode serviceNode)
    {
        if (_cms == null || serviceNode == null)
        {
            return null;
        }

        return _cms.SelectSingleNode(_ui, serviceNode.NodeXPath + "/gdiproperties");
    }
    public List<string> MapServiceUrls()
    {
        List<string> urls = new List<string>();

        if (_cms == null)
        {
            return urls;
        }

        foreach (CmsNode node in _cms.SelectNodes(_ui, "services/arcgisserver/mapserver/*"))
        {
            urls.Add(node.Url);
        }

        foreach (CmsNode node in _cms.SelectNodes(_ui, "services/arcgisserver/imageserverservice/*"))
        {
            urls.Add(node.Url);
        }

        foreach (CmsNode node in _cms.SelectNodes(_ui, "services/arcgisserver/wmtsservice/*"))
        {
            urls.Add(node.Url);
        }

        foreach (CmsNode node in _cms.SelectNodes(_ui, "services/arcgisserver/tileservice/*"))
        {
            urls.Add(node.Url);
        }

        foreach (CmsNode node in _cms.SelectNodes(_ui, "services/ims/*"))
        {
            urls.Add(node.Url);
        }

        #region obsolete

        foreach (CmsNode node in _cms.SelectNodes(_ui, "services/georss/*"))
        {
            urls.Add(node.Url);
        }

        #endregion

        foreach (CmsNode node in _cms.SelectNodes(_ui, "services/ogc/wms/*"))
        {
            urls.Add(node.Url);
        }

        foreach (CmsNode node in _cms.SelectNodes(_ui, "services/ogc/wfs/*"))
        {
            urls.Add(node.Url);
        }

        foreach (CmsNode node in _cms.SelectNodes(_ui, "services/ogc/wmts/*"))
        {
            urls.Add(node.Url);
        }

        foreach (CmsNode node in _cms.SelectNodes(_ui, "services/miscellaneous/generaltilecache/*"))
        {
            urls.Add(node.Url);
        }

        foreach (CmsNode node in _cms.SelectNodes(_ui, "services/miscellaneous/generalvectortilecache/*"))
        {
            urls.Add(node.Url);
        }

        #region obsolete

        foreach (CmsNode node in _cms.SelectNodes(_ui, "services/miscellaneous/servicecollection/*"))
        {
            urls.Add(node.Url);
        }

        #endregion

        foreach (CmsNode node in _cms.SelectNodes(_ui, "services/mapservicecollections/*"))
        {
            urls.Add(node.Url);
        }

        return urls;
    }

    public CmsNodeCollection MapServiceCollectionNodes()
    {
        return _cms?.SelectNodes(_ui, "gdi/mapservicecollections/*") ?? new CmsNodeCollection();
    }

    public CmsNodeCollection MapServiceCollectionServiceNodes(CmsNode collectionNode)
    {
        return _cms?.SelectNodes(_ui, $"{collectionNode.NodeXPath}/services/*") ?? new CmsNodeCollection();
    }

    public object GdiToolValue(string parameterName, Type type = null, bool nullIfDefault = false)
    {
        if (_cms == null || _map == null)
        {
            return null;
        }

        CmsNode node = _cms.SelectSingleNode(_ui, "gdi/tools");
        if (node == null)
        {
            return null;
        }

        object defaultVal = null;
        if (type == null)
        {
            type = typeof(String);
        }

        if (!type.Equals(typeof(string)))
        {
            defaultVal = Activator.CreateInstance(type);
        }

        object val = node.Load(parameterName, defaultVal);

        if (nullIfDefault == true)
        {
            if (val != null && val.Equals(defaultVal))
            {
                return null;
            }

            if (val is String && String.IsNullOrEmpty((string)val))
            {
                return null;
            }
        }

        return val;
    }

    public string[] GdiCustomSchemeNames()
    {
        return _cms.SelectNodes(_ui, "gdi/gdi-custom/*").Select(n => n.Url).ToArray();
    }

    #endregion

    #region SearchService

    public CmsNodeCollection SearchServiceNodes()
    {
        if (_cms == null)
        {
            return new CmsNodeCollection();
        }

        return _cms.SelectNodes(_ui, "services/miscellaneous/searchservices/*");
    }

    public CmsNode SearchServiceNode(string url)
    {
        if (_cms == null)
        {
            return null;
        }

        return _cms.SelectSingleNode(_ui, "services/miscellaneous/searchservices/*", "url", url);
    }

    #endregion

    #region Extents

    public List<string> ExtentUrls()
    {
        List<string> urls = new List<string>();

        if (_cms == null)
        {
            return urls;
        }

        foreach (CmsNode node in _cms.SelectNodes(_ui, "extents/*"))
        {
            urls.Add(node.Url);
        }

        return urls;
    }

    public CmsNode ExtentNode(string extentUrl)
    {
        CmsNode node = _cms.SelectSingleNode(_ui, "extents/*", "url", extentUrl);
        return node;
    }

    #endregion

    #region Layer Properties

    public CmsLink[] GetLayerProperties(CmsNode serviceNode)
    {
        return _cms.SelectNodes(_ui, serviceNode.NodeXPath + "/themesproperties/*")
                    .Where(n => n is CmsLink)
                    .Select(n => (CmsLink)n)
                    .ToArray();
    }

    #endregion
}
