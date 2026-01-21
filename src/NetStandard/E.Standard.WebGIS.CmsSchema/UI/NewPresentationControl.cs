using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CmsSchema.Extensions;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class NewPresentationControl : NameUrlUserConrol, IInitParameter, ISubmit
{
    private string _relPath = String.Empty;
    private CMSManager _cms = null;
    private ISchemaNode _node = null;

    private GroupBox _gbField = new GroupBox() { Label = "Layer" };
    private NameUrlControl _nameUrlControl = new NameUrlControl("nameUrlControl");
    private ListBox _lstLayers = new ListBox("lstLayers") { Label = "Schaltbare Layer" };

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public NewPresentationControl(CmsItemTransistantInjectionServicePack servicePack,
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

        _gbField.AddControl(_lstLayers);
        this.AddControl(_gbField);
        this.AddControl(_nameUrlControl);

        FillList();
    }

    public override NameUrlControl NameUrlControlInstance => _nameUrlControl;

    private void FillList()
    {
        if (_cms != null || !String.IsNullOrEmpty(_relPath))
        {
            #region Felder aus Dienst auslesen

            object[] objects = null;
            if (_node is Presentation)
            {
                objects = _cms.SchemaNodeInstances(_servicePack, _relPath.TrimAndAppendSchemaNodePath(2, "Themes"), true);
            }

            if (objects != null)
            {
                foreach (object obj in objects.Where(o => o is ServiceLayer).OrderBy(l => ((ServiceLayer)l).Name))
                {
                    _lstLayers.Options.Add(new ListBox.Option(((ServiceLayer)obj).Name));
                }
            }
            #endregion
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
        if (_node is Presentation)
        {
            var layerNames = String.Join(";", _lstLayers.SelectedItems ?? new string[0]);
            ((Presentation)_node).LayerNames = layerNames;
        }
    }

    #endregion

}
