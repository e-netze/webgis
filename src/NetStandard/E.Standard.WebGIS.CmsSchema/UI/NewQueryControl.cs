using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CmsSchema.Extensions;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace E.Standard.WebGIS.CmsSchema.UI;


public class NewQueryControl : NameUrlUserConrol, IInitParameter, ISubmit
{
    private string _relPath = String.Empty;
    private CMSManager _cms = null;
    private ISchemaNode _node = null;

    private GroupBox _gbQuery = new GroupBox() { Label = "Abfrage" };
    private NameUrlControl _nameUrlControl = new NameUrlControl("nameUrlControl");
    private ComboBox _cmbQueryTheme = new ComboBox("cmbQueryTheme") { Label = "Abfragethema" };
    private ComboBox _cmbImportTable = new ComboBox("cmbImportTable") { Label = "Tabelle" };

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public NewQueryControl(CmsItemTransistantInjectionServicePack servicePack,
                           ISchemaNode schemaNode)
    {
        _servicePack = servicePack;

        if (schemaNode != null)
        {
            _node = schemaNode;
            _cms = schemaNode.CmsManager;
            _relPath = schemaNode.RelativePath;
            if (!_relPath.EndsWith("/") && !_relPath.EndsWith(@"\"))
            {
                _relPath += "/";
            }
        }

        _gbQuery.AddControl(_cmbQueryTheme);
        _gbQuery.AddControl(_cmbImportTable);
        this.AddControl(_gbQuery);
        this.AddControl(_nameUrlControl);

        _cmbImportTable.Options.Add(new ComboBox.Option(((int)ImportQueryTable.None).ToString(), "Nichts importieren"));
        _cmbImportTable.Options.Add(new ComboBox.Option(((int)ImportQueryTable.Dynamic).ToString(), "Felder Dynamisch (*) importiern"));
        if (schemaNode.CanImportFieldNames())
        {
            _cmbImportTable.Options.Add(new ComboBox.Option(((int)ImportQueryTable.Fields).ToString(), "einzelne Felder importieren"));
        }

        FillCombo();
    }

    public override NameUrlControl NameUrlControlInstance => _nameUrlControl;

    #region IInitParameter

    public object InitParameter
    {
        set
        {
            _nameUrlControl.InitParameter = value;
        }
    }

    #endregion

    #region ISubmit

    public void Submit(NameValueCollection secrets)
    {
        if (_node is Query)
        {
            ((Query)_node).QueryThemeId = _cmbQueryTheme.SelectedItem?.ToString();
            ((Query)_node).AutoImportAllFields = (ImportQueryTable)int.Parse(_cmbImportTable.SelectedItem?.ToString() ?? "0");
        }
    }

    #endregion

    private void FillCombo()
    {
        if (_cms != null || !String.IsNullOrEmpty(_relPath))
        {
            #region Themen aus Dienst auslesen

            object[] objects = null;
            if (_node is Query)
            {
                objects = _cms.SchemaNodeInstances(_servicePack, Helper.TrimPathRight(_relPath, 2) + "/Themes", true);
            }

            if (objects != null)
            {
                foreach (object obj in objects.Where(o => o is ServiceLayer).OrderBy(l => ((ServiceLayer)l).Name))
                {
                    _cmbQueryTheme.Options.Add(new ComboBox.Option(((ServiceLayer)obj).Id, ((ServiceLayer)obj).Name));
                }
            }

            #endregion
        }
    }
}
