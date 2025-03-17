using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class GdiPropertiesEditThemeEditor : UserControl, IUITypeEditor
{
    public GdiPropertiesEditThemeEditor()
    {
    }

    #region UI

    #endregion

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

    public IUIControl GetUIControl(ITypeEditorContext context)
    {
        int index = 0;

        if (context.Value is GdiPropertiesEditTheme[])
        {
            var editThemes = (GdiPropertiesEditTheme[])context.Value;

            foreach (var editTheme in editThemes)
            {
                GroupBox gb = new GroupBox() { Label = "Edit Thema: " + editTheme.AliasName, Collapsed = true, ItemUrl = editTheme.ItemGuid };
                gb.AddControl(new Input("txt_Alilasname_" + index) { Label = "Aliasname *", Value = editTheme.AliasName });
                gb.AddControl(new Input("txt_EditThemeId_" + index) { Label = "Editthema (id) *", Value = editTheme.EditThemeId });

                var comboEditService = new ComboBox("cmb_EditService_" + index)
                {
                    Label = "(webGIS 5) Edit Service"
                };
                comboEditService.Options.Add(new ComboBox.Option("true", "Ja"));
                comboEditService.Options.Add(new ComboBox.Option("false", "Nein"));
                comboEditService.Value = editTheme.EditService.ToString().ToLower();
                gb.Add(comboEditService);

                var comboVisible = new ComboBox("cmb_Visible_" + index)
                {
                    Label = "(webGIS 5) Sichtbar"
                };
                comboVisible.Options.Add(new ComboBox.Option("true", "Ja"));
                comboVisible.Options.Add(new ComboBox.Option("false", "Nein"));
                comboVisible.Value = editTheme.Visible.ToString().ToLower();
                gb.Add(comboVisible);

                this.AddControl(gb);
                index++;
            }
        }

        GroupBox @new = new GroupBox() { Label = "Neues Edit Thema", Collapsed = index > 0 };
        @new.AddControl(new Input("txt_Alilasname_") { Label = "Aliasname" });
        @new.AddControl(new Input("txt_EditThemeId_") { Label = "Editthema (id)" });

        var newComboEditService = new ComboBox("cmb_EditService_")
        {
            Label = "(webGIS 5) Edit Service"
        };
        newComboEditService.Options.Add(new ComboBox.Option("true", "Ja"));
        newComboEditService.Options.Add(new ComboBox.Option("false", "Nein"));
        newComboEditService.Value = "false";
        @new.Add(newComboEditService);

        var newComboVisible = new ComboBox("cmb_Visible_")
        {
            Label = "(webGIS 5) Sichtbar"
        };
        newComboVisible.Options.Add(new ComboBox.Option("true", "Ja"));
        newComboVisible.Options.Add(new ComboBox.Option("false", "Nein"));
        newComboVisible.Value = "true";
        @new.Add(newComboVisible);

        this.AddControl(@new);

        if (index > 0)
        {
            this.AddControl(new InfoText()
            {
                Text = "Tipp: Um bestehende Editthemen zu löschen, im jeweiligen Thema die Felder 'Aliasname' und 'Editthema (id)' leeren und diesen Dialog mit 'Übernehmen' bestätigen."
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
            List<GdiPropertiesEditTheme> editThemes = new List<GdiPropertiesEditTheme>();

            int index = 0;
            while (true)
            {
                if (this.ChildControls.Where(c => c.Name == "txt_Alilasname_" + index).FirstOrDefault() == null)
                {
                    break;
                }

                string aliasName = ((IInputUIControl)GetControl("txt_Alilasname_" + index))?.Value;
                string editThemeId = ((IInputUIControl)GetControl("txt_EditThemeId_" + index))?.Value;
                bool editService = ((IInputUIControl)GetControl("cmb_EditService_" + index))?.Value == "true";
                bool visible = ((IInputUIControl)GetControl("cmb_Visible_" + index))?.Value == "true";

                if (!String.IsNullOrWhiteSpace(aliasName) || !String.IsNullOrWhiteSpace(editThemeId))  // sonst löschen
                {
                    editThemes.Add(new GdiPropertiesEditTheme()
                    {
                        AliasName = aliasName,
                        EditThemeId = editThemeId,
                        EditService = editService,
                        Visible = visible
                    });
                }

                index++;
            }

            // Neues Object übernehem
            string newAliasName = ((IInputUIControl)GetControl("txt_Alilasname_"))?.Value;
            string newEditThemeId = ((IInputUIControl)GetControl("txt_EditThemeId_"))?.Value;
            bool newEditService = ((IInputUIControl)GetControl("cmb_EditService_"))?.Value == "true";
            bool newVisible = ((IInputUIControl)GetControl("cmb_Visible_"))?.Value == "true";
            if (!String.IsNullOrWhiteSpace(newAliasName) || !String.IsNullOrWhiteSpace(newEditThemeId))
            {
                editThemes.Add(new GdiPropertiesEditTheme()
                {
                    AliasName = newAliasName,
                    EditThemeId = newEditThemeId,
                    EditService = newEditService,
                    Visible = newVisible
                });
            }

            return editThemes.Count > 0 ? editThemes.ToArray() : null;
        }
    }
}
