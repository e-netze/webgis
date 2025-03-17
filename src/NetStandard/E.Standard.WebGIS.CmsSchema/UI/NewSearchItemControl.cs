using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.TypeEditor;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class NewSearchItemControl : NameUrlUserConrol, IInitParameter, ISubmit
{
    private string _relPath = String.Empty;
    private CMSManager _cms = null;
    private ISchemaNode _node = null;

    private GroupBox _gbField = new GroupBox() { Label = "Feld" };
    private NameUrlControl _nameUrlControl = new NameUrlControl("nameUrlControl");
    private ComboBox _cmbField = new ComboBox("cmField") { Label = "Suchfeld" };
    private ComboBox _cmbMethod = new ComboBox("cmbMethod") { Label = "Abfrage Methode" };

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public NewSearchItemControl(CmsItemTransistantInjectionServicePack servicePack,
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
        _gbField.AddControl(_cmbMethod);
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

        foreach (QueryMethod method in Enum.GetValues(typeof(QueryMethod)))
        {
            _cmbMethod.Options.Add(new ComboBox.Option(((int)method).ToString(), method.ToString()));
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
        if (_node is SearchItem)
        {
            ((SearchItem)_node).Fields = new string[] { _cmbField.SelectedItem?.ToString() };
            ((SearchItem)_node).Method = (QueryMethod)int.Parse(_cmbMethod.SelectedItem.ToString());
        }
    }

    #endregion
}
