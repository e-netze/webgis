using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CMS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class GdiPropertiesPresentationEditor : UserControl, IUITypeEditor
{
    public GdiPropertiesPresentationEditor()
    {

    }
    public IUIControl GetUIControl(ITypeEditorContext context)
    {
        int index = 0;

        if (context.Value is Presentation.GdiProperties[])
        {
            var presentations = (Presentation.GdiProperties[])context.Value;

            foreach (var presentation in presentations)
            {
                GroupBox gb = new GroupBox() { Label = "Element " + presentation.ContainerUrl, Collapsed = true };
                CreateProperties(gb, index.ToString(), presentation);
                this.AddControl(gb);

                index++;
            }
        }

        GroupBox newGb = new GroupBox() { Label = "Neues Element", Collapsed = index > 0 };
        CreateProperties(newGb, String.Empty, null);
        this.AddControl(newGb);

        if (index > 0)
        {
            this.AddControl(new InfoText()
            {
                Text = "Tipp: Um bestehendes Element zu löschen, im entsprechenden Element das Felder 'Container Url' und 'Gdi Group Name' leeren und diesen Dialog mit 'Übernehmen' bestätigen."
            });
        }

        return this;
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

    #region UI Helper

    private void CreateProperties(Control parent, string index, Presentation.GdiProperties property)
    {
        // Container
        parent.AddControl(new Heading() { Label = "Container" });
        parent.AddControl(new Input("txt_containerurl_" + index) { Label = "Container Url *", Value = property?.ContainerUrl });
        var conboContainerDefault = new ComboBox("cmb_containerdefault_" + index)
        {
            Label = "(webGIS 5) Edit Service"
        };
        conboContainerDefault.Options.Add(new ComboBox.Option("true", "Ja"));
        conboContainerDefault.Options.Add(new ComboBox.Option("false", "Nein"));
        conboContainerDefault.Value = (property?.IsContainerDefault.ToString() ?? "true").ToLower();
        parent.AddControl(conboContainerDefault);

        // Gruppe
        parent.AddControl(new Heading() { Label = "Container" });
        parent.AddControl(CreateEnumCombo("cmb_gdigroupdisplysyle_" + index, "Gdi Group Display Style *", typeof(PresentationGroupStyle), (property?.GdiGroupDisplayStyle ?? PresentationGroupStyle.Button)));
        parent.AddControl(new Input("txt_gdigroupname_" + index) { Label = "Gdi Group Name", Value = property?.GdiGroupName });
        var cmoboVisibleWithService = new ComboBox("cmb_visiblewithservice_" + index)
        {
            Label = "Sichtbar, wenn dieser Dienst in Karte"
        };
        cmoboVisibleWithService.Options.Add(new ComboBox.Option("true", "Ja"));
        cmoboVisibleWithService.Options.Add(new ComboBox.Option("false", "Nein"));
        cmoboVisibleWithService.Value = (property?.VisibleWithService.ToString() ?? "true").ToLower();
        parent.AddControl(cmoboVisibleWithService);
        parent.AddControl(new Input("txt_visiblewithoneofservices_" + index) { Label = "Sichtbar, wenn einer dieser Dienste in der Karte vorkommt", Value = property?.VisibleWithOneOfServices });

        // Sonstiges
        parent.AddControl(new Heading() { Label = "Sonstiges" });
        parent.AddControl(CreateEnumCombo("cmb_affecting_" + index, "Gdi Affecting", typeof(PresentationAffecting), property?.GdiAffecting ?? PresentationAffecting.Service));
        parent.AddControl(CreateEnumCombo("cmb_displaystyle_" + index, "Gdi Display Style", typeof(PresentationCheckMode), property?.GdiDisplayStyle ?? PresentationCheckMode.Button));
    }

    private Presentation.GdiProperties GetGdiProperties(string index)
    {
        var gdiProperties = new Presentation.GdiProperties();

        gdiProperties.ContainerUrl = ((IInputUIControl)GetControl("txt_containerurl_" + index))?.Value;
        gdiProperties.IsContainerDefault = ((IInputUIControl)GetControl("cmb_containerdefault_" + index))?.Value == "true";

        gdiProperties.GdiGroupDisplayStyle = (PresentationGroupStyle)Enum.Parse(typeof(PresentationGroupStyle), ((IInputUIControl)GetControl("cmb_gdigroupdisplysyle_" + index)).Value);
        gdiProperties.GdiGroupName = ((IInputUIControl)GetControl("txt_gdigroupname_" + index))?.Value;
        gdiProperties.VisibleWithService = ((IInputUIControl)GetControl("cmb_visiblewithservice_" + index))?.Value == "true";
        gdiProperties.VisibleWithOneOfServices = ((IInputUIControl)GetControl("txt_visiblewithoneofservices_" + index))?.Value;

        gdiProperties.GdiAffecting = (PresentationAffecting)Enum.Parse(typeof(PresentationAffecting), ((IInputUIControl)GetControl("cmb_affecting_" + index)).Value);
        gdiProperties.GdiDisplayStyle = (PresentationCheckMode)Enum.Parse(typeof(PresentationCheckMode), ((IInputUIControl)GetControl("cmb_displaystyle_" + index)).Value);

        if (String.IsNullOrWhiteSpace(gdiProperties.ContainerUrl) && String.IsNullOrWhiteSpace(gdiProperties.GdiGroupName))
        {
            return null;
        }

        return gdiProperties;
    }

    private ComboBox CreateEnumCombo(string name, string label, Type enumType, object value)
    {
        var combo = new ComboBox(name) { Label = label };

        foreach (var val in Enum.GetNames(enumType))
        {
            combo.Options.Add(new ComboBox.Option(val));
        }
        combo.Value = value?.ToString();

        return combo;
    }

    #endregion

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public object ResultValue
    {
        get
        {
            List<Presentation.GdiProperties> properties = new List<Presentation.GdiProperties>();

            int index = 0;
            while (true)
            {
                if (this.ChildControls.Where(c => c.Name == "txt_containerurl_" + index).FirstOrDefault() == null)
                {
                    break;
                }

                var gdiProperties = GetGdiProperties(index.ToString());
                if (gdiProperties != null)
                {
                    properties.Add(gdiProperties);
                }

                index++;
            }

            var newGdiProperties = GetGdiProperties(String.Empty);
            if (newGdiProperties != null)
            {
                properties.Add(newGdiProperties);
            }

            return properties.Count > 0 ? properties.ToArray() : null;
        }
    }
}
