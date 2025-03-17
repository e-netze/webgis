using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class GdiPropertiesVisFilterEditor : UserControl, IUITypeEditor
{
    public GdiPropertiesVisFilterEditor()
    {

    }

    #region Overrides 

    public override IUIControl GetControl(string name)
    {
        var control = base.GetControl(name);
        if (control != null)
        {
            return control;
        }

        if (name.StartsWith("txt_"))
        {
            var txtControl = new Input(name);
            this.AddControl(txtControl);
            return txtControl;
        }
        if (name.StartsWith("cmb_"))
        {
            var cmbControl = new ComboBox(name);
            this.AddControl(cmbControl);
            return cmbControl;
        }

        return null;
    }

    #endregion

    #region IUITypeEditor

    public IUIControl GetUIControl(ITypeEditorContext context)
    {
        int index = 0;

        if (context.Value is VisFilter.GdiProperties[])
        {
            var gdiProperties = (VisFilter.GdiProperties[])context.Value;

            foreach (var gdiProperty in gdiProperties)
            {
                GroupBox gb = new GroupBox() { Label = "Gruppenname: " + gdiProperty.GdiGroupName, Collapsed = true, ItemUrl = gdiProperty.GdiGroupName };

                gb.AddControl(new Input("txt_GroupName_" + index) { Label = "Gruppenname *", Value = gdiProperty.GdiGroupName });
                this.AddControl(gb);
                index++;
            }
        }

        GroupBox @new = new GroupBox() { Label = "Neuer Gruppenname", Collapsed = index > 0 };
        @new.AddControl(new Input("txt_GroupName_") { Label = "Gruppenname *", Value = String.Empty });
        this.AddControl(@new);


        //if (index > 0)
        {
            this.AddControl(new InfoText()
            {
                Text = "Tipp: Hier können auch Gruppen ohne Namen (leere Zeichenkette) erstellt werden. Diese ist beispielsweise bei versteckten Filtern für WebGIS 4 notwendig. Um so eine Gruppe zu erstellen, geben sie einfache mindesten ein Leerzeichen als neuen Gruppennamen an. Um bestehende Gruppennamen zu löschen, in das Feld 'Gruppenname' den Löschcode #13 eintragen und den Dialog mit 'Übernehmen' bestätigen."
            });
        }

        return this;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public object ResultValue
    {
        get
        {
            var gdiProperties = new List<VisFilter.GdiProperties>();

            int index = 0;
            while (true)
            {
                if (this.ChildControls.Where(c => c.Name == "txt_GroupName_" + index).FirstOrDefault() == null)
                {
                    break;
                }

                string groupName = ((IInputUIControl)GetControl("txt_GroupName_" + index))?.Value.Trim() ?? String.Empty;

                if (/*!String.IsNullOrWhiteSpace(groupName)*/ groupName != "#13")
                {
                    gdiProperties.Add(new VisFilter.GdiProperties()
                    {
                        GdiGroupName = groupName
                    });
                }

                index++;
            }

            // Neues Object erstellen
            string newGroupName = ((IInputUIControl)GetControl("txt_GroupName_"))?.Value;
            if (!String.IsNullOrEmpty(newGroupName))  // Whitespaces allowed
            {
                gdiProperties.Add(new VisFilter.GdiProperties()
                {
                    GdiGroupName = newGroupName.Trim()  // Trim
                });
            }

            return gdiProperties.Count > 0 ? gdiProperties.ToArray() : null;
        }
    }

    #endregion
}
