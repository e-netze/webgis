using E.Standard.CMS.Core;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CmsSchema.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class SelectLayersEditor : UserControl, IUITypeEditor
{
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public SelectLayersEditor(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        this.AddControl(_lstLayers);
    }

    #region UI

    protected ListBox _lstLayers = new ListBox("lstLayers") { Label = "Layer" };

    #endregion

    public IUIControl GetUIControl(ITypeEditorContext context)
    {
        CMSManager cms = context.CmsManager;
        string relPath = context.RelativePath;

        object[] objects;
        if (context.Instance is EditingFeatureTransfer)
        {
            objects = cms.SchemaNodeInstances(_servicePack, relPath.TrimAndAppendSchemaNodePath(4, "Themes"), true);
        }
        else
        {
            objects = cms.SchemaNodeInstances(_servicePack, relPath.TrimAndAppendSchemaNodePath(2, "Themes"), true);
        }

        List<string> checkedLayerNames = new List<string>();
        foreach (string layername in ((string)context.Value ?? "").Split(';'))
        {
            if (String.IsNullOrEmpty(layername))
            {
                continue;
            }

            checkedLayerNames.Add(layername);
        }

        Dictionary<ServiceLayer, bool> layers = new Dictionary<ServiceLayer, bool>();
        foreach (object obj in objects.Where(o => o is ServiceLayer).OrderBy(l => ((ServiceLayer)l).Name))
        {
            //if (obj is ServiceLayer)
            {
                ServiceLayer layer = (ServiceLayer)obj;
                if (String.IsNullOrEmpty(layer.Name))
                {
                    continue;
                }

                layers.Add(layer, false);
            }
        }

        foreach (string checkedLayerName in checkedLayerNames)
        {
            bool found = false;
            foreach (ServiceLayer layer in layers.Keys)
            {
                if (layer.Name == checkedLayerName)
                {
                    layers[layer] = true;
                    found = true;
                    break;
                }
            }

            if (!found) // AGS: Hier können immer noch Layer ohne den mxd-Gruppen stehen -> fuzzy suche
            {
                foreach (ServiceLayer layer in layers.Keys)
                {
                    if (ArcServerService.ShortLayerName(layer.Name) == ArcServerService.ShortLayerName(checkedLayerName))
                    {
                        layers[layer] = true;
                        break;
                    }
                }
            }
        }

        foreach (var layer in layers.Keys)
        {
            _lstLayers.Options.Add(new ListBox.Option(layer.Name)
            {
                Selected = layers[layer] == true ? true : null
            });
        }

        return this;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    virtual public object ResultValue
    {
        get
        {
            if (_lstLayers.SelectedItems == null || _lstLayers.SelectedItems.Length == 0)
            {
                return String.Empty;
            }

            return String.Join(";", _lstLayers.SelectedItems);
        }
    }
}
