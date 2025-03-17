using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using System;
using System.Collections.Specialized;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class NewLabelingControl : NameUrlUserConrol, IInitParameter, ISubmit
{
    private string _relPath = String.Empty;
    private CMSManager _cms = null;
    private ISchemaNode _node = null;

    private GroupBox _gbQuery = new GroupBox() { Label = "Themen Beschriften" };
    private NameUrlControl _nameUrlControl = new NameUrlControl("nameUrlControl") { UrlIsVisible = false };
    private ComboBox _cmbLabellingTheme = new ComboBox("cmbLabellingTheme") { Label = "Beschriftungsthema" };

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public NewLabelingControl(CmsItemTransistantInjectionServicePack servicePack,
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

        _gbQuery.AddControl(_cmbLabellingTheme);
        this.AddControl(_gbQuery);
        this.AddControl(_nameUrlControl);

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
        if (_node is Labelling)
        {
            ((Labelling)_node).LabellingThemeId = _cmbLabellingTheme.SelectedItem?.ToString();
        }
    }

    #endregion

    private void FillCombo()
    {
        if (_cms != null || !String.IsNullOrEmpty(_relPath))
        {
            #region Felder aus Dienst auslesen

            object[] objects = null;
            if (_node is Labelling)
            {
                objects = _cms.SchemaNodeInstances(_servicePack, Helper.TrimPathRight(_relPath, 2) + "/Themes", true);
            }

            if (objects != null)
            {
                foreach (object obj in objects)
                {
                    if (obj is ServiceLayer)
                    {
                        _cmbLabellingTheme.Options.Add(new ComboBox.Option(((ServiceLayer)obj).Id, ((ServiceLayer)obj).Name));
                    }
                }
            }
            #endregion
        }
    }
}
