//using E.Standard.CMS.Core;
//using E.Standard.CMS.UI.Controls;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace E.Standard.WebGIS.CmsSchema.TypeEditor
//{
//    public class SelectLayersJsonEditor : UserControl, IUITypeEditor
//    {
//        private readonly CmsItemTransistantInjectionServicePack _servicePack;

//        public SelectLayersJsonEditor(CmsItemTransistantInjectionServicePack servicePack)
//        {
//            _servicePack = servicePack;

//            this.AddControl(_lstLayers);
//        }

//        #region UI

//        protected ListBox _lstLayers = new ListBox("lstLayers") { Label = "Layer" };

//        #endregion

//        public IUIControl GetUIControl(ITypeEditorContext context)
//        {
//            CMSManager cms = context.CmsManager;
//            string relPath = context.RelativePath;

//            object[] objects;
//            if (context.Instance is EditingFeatureTransfer)
//            {
//                objects = cms.SchemaNodeInstances(_servicePack, Helper.TrimPathRight(relPath, 4) + "/Themes", true);
//            } 
//            else
//            {
//                objects = cms.SchemaNodeInstances(_servicePack, Helper.TrimPathRight(relPath, 2) + "/Themes", true);
//            }

//            List<string> checkedLayerNames = new List<string>();
//            foreach (string layername in ((string)context.Value ?? "").Split(';'))
//            {
//                if (String.IsNullOrEmpty(layername))
//                {
//                    continue;
//                }

//                checkedLayerNames.Add(layername);
//            }

//            Dictionary<ServiceLayer, bool> layers = new Dictionary<ServiceLayer, bool>();
//            foreach (object obj in objects.Where(o => o is ServiceLayer).OrderBy(l => ((ServiceLayer)l).Name))
//            {
//                //if (obj is ServiceLayer)
//                {
//                    ServiceLayer layer = (ServiceLayer)obj;
//                    if (String.IsNullOrEmpty(layer.Name))
//                    {
//                        continue;
//                    }

//                    layers.Add(layer, false);
//                }
//            }

//            foreach (string checkedLayerName in checkedLayerNames)
//            {
//                bool found = false;
//                foreach (ServiceLayer layer in layers.Keys)
//                {
//                    if (layer.Name == checkedLayerName)
//                    {
//                        layers[layer] = true;
//                        found = true;
//                        break;
//                    }
//                }

//                if (!found) // AGS: Hier können immer noch Layer ohne den mxd-Gruppen stehen -> fuzzy suche
//                {
//                    foreach (ServiceLayer layer in layers.Keys)
//                    {
//                        if (ArcServerService.ShortLayerName(layer.Name) == ArcServerService.ShortLayerName(checkedLayerName))
//                        {
//                            layers[layer] = true;
//                            break;
//                        }
//                    }
//                }
//            }

//            foreach (var layer in layers.Keys)
//            {
//                _lstLayers.Options.Add(new ListBox.Option(layer.Name)
//                {
//                    Selected = layers[layer] == true ? (bool?)true : null
//                });
//            }

//            return this;
//        }

//        [JsonIgnore]
//        [System.Text.Json.Serialization.JsonIgnore]
//        public object ResultValue
//        {
//            get
//            {
//                return JSerializer.Serialize(_lstLayers.SelectedOptions ?? Array.Empty<ListBox.Option>());
//            }
//        }
//    }
//}
