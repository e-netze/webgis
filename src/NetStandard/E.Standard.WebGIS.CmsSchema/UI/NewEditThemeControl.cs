using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CmsSchema.Extensions;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class NewEditThemeControl : NameUrlUserConrol, IInitParameter, ISubmit
{
    private string _relPath = String.Empty;
    private CMSManager _cms = null;
    private ISchemaNode _node = null;

    private GroupBox _gbEditTheme = new GroupBox() { Label = "Thema bearbeiten" };
    private NameUrlControl _nameUrlControl = new NameUrlControl("nameUrlControl");
    private ComboBox _cmbEditTheme = new ComboBox("cmbEditTheme") { Label = "Edit Thema" };
    private ComboBox _cmbImportFields = new ComboBox("cmbImportFields") { Label = "Felder" };
    private Input _txtSrs = new Input("txtSrs") { Label = "Räumliches Bezugssystem (EPSG-Code)" };

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public NewEditThemeControl(CmsItemTransistantInjectionServicePack servicePack,
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

            _txtSrs.Value = Helper.TrimPathRight(schemaNode.RelativePath, 1).ServiceSrs(_cms, _servicePack).Result.ToString();
        }

        _gbEditTheme.AddControl(_cmbEditTheme);
        _gbEditTheme.AddControl(_txtSrs);
        _gbEditTheme.AddControl(_cmbImportFields);
        this.AddControl(_gbEditTheme);
        this.AddControl(_nameUrlControl);

        #region Events

        _cmbEditTheme.OnClick += CmbEditTheme_OnClick;

        #endregion

        FillCombos();
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
        if (_node is EditingTheme)
        {
            ((EditingTheme)_node).EditingThemeId = _cmbEditTheme.SelectedItem?.ToString().Split(":").First();
            ((EditingTheme)_node).Srs = int.Parse(_txtSrs.Value);
            ((EditingTheme)_node).AutoImportEditFields = (ImportEditFields)int.Parse(_cmbImportFields.SelectedItem?.ToString() ?? "0");
        }
    }

    #endregion

    #region Event Handlers

    private void CmbEditTheme_OnClick(object sender, EventArgs e)
    {
        if (_cmbEditTheme.Value != null)
        {
            _nameUrlControl.SetName(_cmbEditTheme.Value.Split(":").Last(), true);
        }
    }

    #endregion

    private void FillCombos()
    {
        if (_cms != null && !String.IsNullOrEmpty(_relPath))
        {
            _cmbImportFields.Options.Add(new ComboBox.Option(((int)ImportEditFields.None).ToString(), "Nichts importieren"));
            _cmbImportFields.Options.Add(new ComboBox.Option(((int)ImportEditFields.Fields).ToString(), "einzelne Felder importieren (Kategorie Allgemein)"));

            #region Themen aus Dienst auslesen

            object[] objects = null;
            if (_node is EditingTheme)
            {
                objects = _cms.SchemaNodeInstances(_servicePack, Helper.TrimPathRight(_relPath, 2) + "/Themes", true);
            }

            if (objects != null)
            {
                _cmbEditTheme.Options.Add(new ComboBox.Option("", "--- Select Edit Theme ---"));

                foreach (object obj in objects.Where(o => o is ServiceLayer).OrderBy(l => ((ServiceLayer)l).Name))
                {
                    _cmbEditTheme.Options.Add(new ComboBox.Option(
                        $"{((ServiceLayer)obj).Id}:{((ServiceLayer)obj).Name.Split(@"\").Last()}",
                        ((ServiceLayer)obj).Name));
                }
            }
            #endregion
        }
    }
}
