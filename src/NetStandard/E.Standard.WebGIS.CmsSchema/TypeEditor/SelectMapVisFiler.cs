using E.Standard.CMS.Core;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CmsSchema.Extensions;
using Newtonsoft.Json;
using System;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class SelectMapVisFiler : UserControl, IUITypeEditor
{
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public SelectMapVisFiler(CmsItemTransistantInjectionServicePack servicePack)
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

        object[] objects = cms.SchemaNodeInstances(_servicePack, relPath.TrimAndAppendSchemaNodePath(3, "visfilter"), true);

        FillList(_servicePack, list, objects);

        return this;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public object ResultValue
    {
        get { return list.SelectedItems != null && list.SelectedItems.Length == 1 ? list.SelectedItems[0] : String.Empty; }
    }

    #region Helper

    static internal void FillList(CmsItemTransistantInjectionServicePack servicePack,
                                  ListBox list, object[] filters)
    {
        foreach (object visFilterObj in filters)
        {
            if (visFilterObj is VisFilterLink)
            {
                VisFilterLink visFilterLink = (VisFilterLink)visFilterObj;
                if (visFilterLink.CmsManager == null)
                {
                    continue;
                }

                VisFilter visFilter = visFilterLink.CmsManager.SchemaNodeInstance(servicePack, visFilterLink.LinkUri, true) as VisFilter;
                if (visFilter == null)
                {
                    continue;
                }

                list.Options.Add(new ListBox.Option(visFilterLink.Url.ToLower(), visFilter.DisplayName + " (" + visFilterLink.Url.ToLower() + ")"));
            }
            else if (visFilterObj is VisFilterGroup)
            {
                VisFilterGroup visFilterGroup = (VisFilterGroup)visFilterObj;

                list.Options.Add(new ListBox.Option(visFilterGroup.Url.ToLower(), visFilterGroup.DisplayName + " (" + visFilterGroup.Url.ToLower() + ")"));
            }
            else if (visFilterObj is VisFilter)
            {
                VisFilter visFilter = (VisFilter)visFilterObj;

                list.Options.Add(new ListBox.Option(visFilter.Url.ToLower(), visFilter.DisplayName + " (" + visFilter.Url.ToLower() + ")"));
            }
        }
    }

    #endregion
}
