using E.Standard.CMS.Core;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CmsSchema.Extensions;
using Newtonsoft.Json;
using System;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class SelectServiceVisFilter : UserControl, IUITypeEditor
{
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public SelectServiceVisFilter(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        this.AddControl(list);
    }

    #region UI

    ListBox list = new ListBox("list") { Label = "Filter", MultiSelect = false, SelectAndCommit = true };

    #endregion

    public IUIControl GetUIControl(ITypeEditorContext context)
    {
        CMSManager cms = context.CmsManager;
        string relPath = context.RelativePath;

        object[] objects = cms.SchemaNodeInstances(_servicePack, relPath.TrimAndAppendSchemaNodePath(2, "visfilter"), true);

        SelectMapVisFiler.FillList(_servicePack, list, objects);

        return this;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public object ResultValue
    {
        get { return list.SelectedItems != null && list.SelectedItems.Length == 1 ? list.SelectedItems[0] : String.Empty; }
    }
}
