using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using Newtonsoft.Json;
using System;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class TocNameEditor : UserControl, IUITypeEditor
{
    public TocNameEditor()
    {
        this.AddControl(list);
    }

    #region UI

    ListBox list = new ListBox("list") { Label = "Inhaltsverzeichnisse", MultiSelect = false, SelectAndCommit = true };

    #endregion

    public IUIControl GetUIControl(ITypeEditorContext context)
    {
        CMSManager cms = context.CmsManager;
        string relPath = context.RelativePath;

        string tocsPath = cms.ConnectionString + @"/" + ((Link)context.Instance).LinkUri + @"/tocs";
        var fi = DocumentFactory.DocumentInfo(cms.ConnectionString + @"/" + relPath + ".xml");
        if (fi.Exists && fi.Name.ToLower() == ".linktemplate.xml")
        {
            tocsPath = fi.Directory.FullName + @"/tocs";
        }

        ItemOrder itemOrder = new ItemOrder(tocsPath);
        foreach (string item in itemOrder.Items)
        {
            var di = DocumentFactory.PathInfo(tocsPath + @"/" + item);
            list.Options.Add(new ListBox.Option(di.Name));
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
