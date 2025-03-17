using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using Newtonsoft.Json;
using System;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class Proj4TypeEditor : UserControl, IUITypeEditor
{
    public Proj4TypeEditor()
    {
        this.AddControl(txtWebGISInstance);
        this.AddControl(txtFilter);
        this.AddControl(btnSearch);
        this.AddControl(lstEpsg);
    }

    #region UI

    Input txtWebGISInstance = new Input("txtWebGISInstance") { Label = "WebGIS Instanz" };
    Input txtFilter = new Input("txtFilter") { Label = "Filter" };
    Button btnSearch = new Button("btnSearch") { Label = "Suchen »" };
    ListBox lstEpsg = new ListBox("lstEpsg") { Label = "Projektionen", MultiSelect = false, SelectAndCommit = true };

    #endregion

    public IUIControl GetUIControl(ITypeEditorContext context)
    {
        btnSearch.OnClick += BtnSearch_OnClick;
        return this;
    }

    private void BtnSearch_OnClick(object sender, EventArgs e)
    {

    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public object ResultValue
    {
        get { return lstEpsg.SelectedItems != null && lstEpsg.SelectedItems.Length == 1 ? lstEpsg.SelectedItems[0] : String.Empty; }
    }
}
