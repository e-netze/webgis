//using E.Standard.Api.App.Services.Cms;
//using E.Standard.CMS.Core;
//using E.Standard.WebGIS.CMS;
//using E.Standard.WebMapping.Core;
//using E.Standard.WebMapping.Core.Abstraction;
//using E.Standard.WebMapping.Core.Collections;
//using E.Standard.WebMapping.Core.Filters;
//using System;
//using System.Threading.Tasks;

//namespace E.Standard.Api.App;

//public class ApiCollectionService : CollectionService
//{
//    private readonly CmsDocumentsService _cmsDocuments;
//    private readonly string _cmsName;
//    private readonly string _cmsNodeXPath;

//    public ApiCollectionService(CmsDocumentsService cmsDocuments, string cmsName, string cmsNodeXPath)
//        : base()
//    {
//        _cmsDocuments = cmsDocuments;
//        _cmsName = cmsName;
//        _cmsNodeXPath = cmsNodeXPath;
//    }

//    public override ServiceResponseType ResponseType
//    {
//        get
//        {
//            return ServiceResponseType.Collection;
//        }
//    }

//    async public override Task<bool> InitAsync(IMap map, IRequestContext requestContext)
//    {
//        if (this.Layers.Count != 0)
//        {
//            return true;
//        }


//        #region Init Child Services

//        foreach (IMapService service in _services)
//        {
//            if (service.Layers == null || service.Layers.Count == 0)
//            {
//                if (!await service.InitAsync(map, requestContext))
//                {
//                    return false;
//                }
//            }
//        }

//        #endregion

//        #region TOC

//        CmsDocument cms = _cmsDocuments.GetCmsDocument(_cmsName) ?? _cmsDocuments.GetCustomCmsDocument(_cmsName);
//        CmsNode cmsNode = cms.SelectSingleNode(null, _cmsNodeXPath);
//        CmsHlp cmsHlp = new CmsHlp(cms, null);
//        CmsNode gdiProperties = cmsHlp.ServiceGdiPropertiesNode(cmsNode);

//        string tocName = gdiProperties?.LoadString("tocname");
//        CmsNode tocNode = cms.SelectSingleNode(null, $"{cmsNode.NodeXPath}/tocs/{tocName}");

//        if (tocName != null)
//        {
//            foreach (CmsNode tocElement in cms.SelectNodes(null, $"{tocNode.NodeXPath}/.//*"))
//            {
//                if (tocElement is CmsLink)
//                {
//                    CmsLink tocElementLink = (CmsLink)tocElement;

//                    if (tocElementLink.Target == null)
//                    {
//                        continue;
//                    }

//                    var childServiceNode = tocElementLink.Target.ParentNode.ParentNode;
//                    string serviceUrl = String.IsNullOrEmpty(_cmsName) ? childServiceNode.Url : childServiceNode.Url + "@" + _cmsName;
//                    IMapService service = _services.FindByUrl(serviceUrl);

//                    if (service != null)
//                    {
//                        ILayer childLayer = service.Layers.FindByLayerId(tocElementLink.Target.Id);

//                        if (childLayer != null)
//                        {
//                            this.Layers.Add(new CollectionLayer(tocElement.AliasName, serviceUrl + ':' + childLayer.ID, this, childLayer)
//                            {
//                                Visible = tocElement.Visible,
//                            });
//                            childLayer.Visible = tocElement.Visible;
//                        }
//                    }
//                }
//            }
//        }

//        #endregion

//        return true;
//    }

//    protected override CollectionService ClassClone()
//    {
//        return new ApiCollectionService(_cmsDocuments, _cmsName, _cmsNodeXPath);
//    }

//    override public IMapService Clone(IMap parent)
//    {
//        ApiCollectionService clone = (ApiCollectionService)base.Clone(parent);

//        foreach (ILayer layer in this.Layers)
//        {
//            clone.Layers.Add(layer.Clone(clone));
//        }

//        return clone;
//    }

//    #region Classes

//    public class CollectionLayer : Layer
//    {
//        public CollectionLayer(string name, string id, IMapService service, ILayer childLayer)
//            : base(name, id, service, queryable: true)
//        {
//            this.ChildLayer = ChildLayer;
//        }

//        public ILayer ChildLayer { get; set; }

//        override public ILayer Clone(IMapService parent)
//        {
//            return new CollectionLayer(this.Name, this.ID, this._service, this.ChildLayer);
//        }

//        async public override Task<bool> GetFeaturesAsync(QueryFilter query, FeatureCollection result, IRequestContext requestContext)
//        {
//            return await ChildLayer.GetFeaturesAsync(query, result, requestContext);
//        }
//    }

//    #endregion
//}