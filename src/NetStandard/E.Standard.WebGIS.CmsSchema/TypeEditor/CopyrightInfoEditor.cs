using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using Newtonsoft.Json;
using System;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class CopyrightInfoEditor : UserControl, IUITypeEditor
{
    public CopyrightInfoEditor()
    {
        this.AddControl(list);
    }

    #region UI

    ListBox list = new ListBox("list") { Label = "Copyright Infos", MultiSelect = false, SelectAndCommit = true };

    #endregion

    public IUIControl GetUIControl(ITypeEditorContext context)
    {
        CMSManager cms = context.CmsManager;
        string relPath = context.RelativePath;

        ItemOrder itemOrder = new ItemOrder(cms.ConnectionString + @"/etc/copyright");
        foreach (string item in itemOrder.Items)
        {
            var fi = DocumentFactory.DocumentInfo(cms.ConnectionString + @"/etc/copyright/" + item);
            list.Options.Add(new ListBox.Option(fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length)));
        }

        return this;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public object ResultValue
    {
        get { return list.SelectedItems != null && list.SelectedItems.Length == 1 ? list.SelectedItems[0] : String.Empty; }
    }
}
