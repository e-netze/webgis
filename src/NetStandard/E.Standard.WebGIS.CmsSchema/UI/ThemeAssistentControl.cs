using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CmsSchema.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class ThemeAssistentControl : UserControl
{
    private string _relPath = String.Empty;
    private CMSManager _cms = null;
    private ISchemaNode _node = null;

    private ListBox lstThemes = new ListBox("lstThemes") { Label = "Themen" };

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public ThemeAssistentControl(CmsItemTransistantInjectionServicePack servicePack,
                                 ISchemaNode schemaNode)
    {
        _servicePack = servicePack;

        if (schemaNode != null)
        {
            _cms = schemaNode.CmsManager;
            _relPath = schemaNode.RelativePath;
            if (!_relPath.EndsWith("/") && !_relPath.EndsWith(@"\"))
            {
                _relPath += "/";
            }
        }
        _node = schemaNode;

        this.AddControl(lstThemes);

        FillGridView();
    }

    private void FillGridView()
    {
        if (_cms != null || !String.IsNullOrEmpty(_relPath))
        {
            #region Themen aus Dienst auslesen

            object[] objects = null;
            if (_node is PresentationThemeAssistent)
            {
                objects = _cms.SchemaNodeInstances(_servicePack, _relPath.TrimAndAppendSchemaNodePath(2, "Themes"), true);
            }

            List<string> themes = new List<string>();

            if (objects != null)
            {
                foreach (object obj in objects.Where(o => o is ServiceLayer).OrderBy(l => ((ServiceLayer)l).Name))
                {
                    themes.Add(((ServiceLayer)obj).Name);
                }
            }
            #endregion

            #region Datatable erzeugen

            foreach (string field in themes)
            {
                lstThemes.Options.Add(new ListBox.Option(field));
            }

            #endregion
        }
    }

    public string[] SelectedThemes
    {
        get
        {
            List<string> fields = new List<string>();
            foreach (string item in lstThemes.SelectedItems)
            {
                fields.Add(item);
            }
            return fields.ToArray();
        }
    }
}
