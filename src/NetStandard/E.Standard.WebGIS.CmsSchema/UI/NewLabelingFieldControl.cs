using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CmsSchema.TypeEditor;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class NewLabelingFieldControl : NameUrlUserConrol, IInitParameter, ISubmit
{
    private string _relPath = String.Empty;
    private CMSManager _cms = null;
    private ISchemaNode _node = null;

    private GroupBox _gbField = new GroupBox() { Label = "Beschriftungsfeld" };
    private NameUrlControl _nameUrlControl = new NameUrlControl("nameUrlControl") { UrlIsVisible = false };
    private ComboBox _cmbField = new ComboBox("cmField") { Label = "Beschriftungsfeld" };

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public NewLabelingFieldControl(CmsItemTransistantInjectionServicePack servicePack,
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

        _gbField.AddControl(_cmbField);
        this.AddControl(_gbField);
        this.AddControl(_nameUrlControl);

        FillCombo().Wait();
    }

    public override NameUrlControl NameUrlControlInstance => _nameUrlControl;

    async private Task FillCombo()
    {
        if (_cms != null || !String.IsNullOrEmpty(_relPath))
        {
            // Use Typeeditor to query service for fields
            var typeEditorContext = new TypeEditorContext(_cms, _relPath, _node, "Fields");
            var themeFieldsEditor = new ThemeFieldsEditor(_servicePack);
            await themeFieldsEditor.PerformQuery(typeEditorContext);

            var fields = themeFieldsEditor.GetFields();

            foreach (var field in fields.OrderBy(f => f.ToLower()))
            {
                _cmbField.Options.Add(new ComboBox.Option(field));
            }
        }
    }

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
        if (_node is LabellingField)
        {
            ((LabellingField)_node).FieldName = _cmbField.SelectedItem?.ToString();
        }
    }

    #endregion
}
