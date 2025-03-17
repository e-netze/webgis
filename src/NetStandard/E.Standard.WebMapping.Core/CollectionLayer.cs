//using E.Standard.ThreadSafe;
//using E.Standard.Web.Abstractions;
//using E.Standard.WebMapping.Core.Abstraction;
//using E.Standard.WebMapping.Core.Collections;
//using E.Standard.WebMapping.Core.Filters;
//using E.Standard.WebMapping.Core.Geometry;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace E.Standard.WebMapping.Core
//{
//    public class CollectionLayer : Layer, IEnumerable<ILayer>
//    {
//        private readonly ThreadSafeList<ILayer> _layers = new ThreadSafeList<ILayer>();
//        private readonly IMapSession _session;
//        private const string _layerGlobalIdFieldName = "_layerGlobalId";

//        //public CollectionLayer(IMapSession session)
//        //    : base("CollectionLayer", String.Empty, null, true)
//        //{
//        //    _session = session;

//        //    this.Fields.Add(new Field(this.IdFieldNameSetter = "CollectionLayerObjectId", FieldType.ID));
//        //    this.Fields.Add(new Field(this.ShapeFieldNameSetter = "CollectionlayerShape", FieldType.Shape));
//        //}

//        public void Add(ILayer layer)
//        {
//            if (layer != null)
//            {
//                _layers.Add(layer);

//                if (layer.Fields != null)
//                {
//                    foreach (IField field in layer.Fields)
//                    {
//                        if (this.Fields.FindField(field.Name) == null)
//                        {
//                            this.Fields.Add(field);
//                        }
//                    }
//                }
//            }
//        }
//        public int Count
//        {
//            get { return _layers.Count; }
//        }
//        public ILayer this[int index]
//        {
//            get
//            {
//                if (index < 0 || index >= _layers.Count)
//                {
//                    return null;
//                }

//                return _layers[index];
//            }
//        }

//        override public ILayer Clone(IMapService parent)
//        {
//            //CollectionLayer clone = new CollectionLayer(_session);

//            //foreach (ILayer layer in _layers)
//            //{
//            //    clone.Add(layer);
//            //}

//            //return clone;

//            return null;
//        }

//        async public override Task<bool> GetFeaturesAsync(QueryFilter filter, FeatureCollection result, IRequestContext requestContext)
//        {
//            List<string> affectedServices = null;

//            if (filter is SpatialFilter &&
//                ((SpatialFilter)filter).QueryShape != null &&
//                _session.SpatialConstraintService != null && _session.SpatialConstraintService.IsValid)
//            {
//                foreach (ILayer layer in _layers)
//                {
//                    IMapService service = _session.Map.Services.FindByLayer(layer);
//                    if (service == null)
//                    {
//                        continue;
//                    }

//                    if (service.CheckSpatialConstraints == true)
//                    {
//                        Shape shape = ((SpatialFilter)filter).QueryShape;
//                        // TODO: Muss Shape projeziert werden?
//                        // Methode Affected erwartet SHAPE in Map SpatialReference System!!

//                        affectedServices = await _session.SpatialConstraintService.AffectedAsync(shape, requestContext);
//                        break;
//                    }
//                }
//            }
//            foreach (ILayer layer in _layers)
//            {
//                try
//                {
//                    IMapService service = _session.Map.Services.FindByLayer(layer);
//                    if (service == null)
//                    {
//                        continue;
//                    }

//                    if (service.CheckSpatialConstraints == true &&
//                       affectedServices != null &&
//                       affectedServices.Contains(service.Url) == false)
//                    {
//                        continue;
//                    }

//                    QueryFilter cFilter = filter.Clone();
//                    Core.Filters.QueryFilter.SetFeatureFilterCoordsys(_session, cFilter, layer);
//                    Core.Filters.QueryFilter.ReduceSubFields(cFilter, layer, true);

//                    FeatureCollection features = new FeatureCollection();
//                    await layer.GetFeaturesAsync(cFilter, features, requestContext);

//                    foreach (Feature feature in features)
//                    {
//                        feature.Attributes.Add(new Attribute(this.IdFieldName, result.Count.ToString()));
//                        feature.Attributes.Add(new Attribute(_layerGlobalIdFieldName, layer.GlobalID));

//                        result.Add(feature);
//                    }
//                    if (features.Count == 0 && !String.IsNullOrEmpty(features.ResultText))
//                    {
//                        result.ResultText += features.ResultText;
//                    }
//                }
//                catch { } // Kann bei Geoland fehler verursachen, wenn zB bei den Burgenländern "Operation not supported" ist. Betrieb macht ja die SynerGis ;-)
//            }
//            result.Layer = this;
//            result.Query = filter;

//            return true;
//        }

//        public static string GlobalLayerIdFieldName
//        {
//            get { return _layerGlobalIdFieldName; }
//        }

//        #region IEnumerable<ILayer> Member

//        public IEnumerator<ILayer> GetEnumerator()
//        {
//            return _layers.GetEnumerator();
//        }

//        #endregion

//        #region IEnumerable Member

//        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
//        {
//            return _layers.GetEnumerator();
//        }

//        #endregion
//    }
//}
