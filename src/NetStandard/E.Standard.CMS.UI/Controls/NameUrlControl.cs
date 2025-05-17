using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.UI.Abstraction;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace E.Standard.CMS.UI.Controls;

public class NameUrlControl : UserControl, IInitParameter
{
    private NameUrl _nameUrl = null;

    public NameUrlControl(string name = "")
        : base(name)
    {
        this.txtName = new Input("Name") { Label = "Name/Bezeichnung", Placeholder = "Anzeigename", Required = true };
        this.txtUrl = new Input("Url") { Label = "Url", Placeholder = "Kurzname, für Url Aufrufe (nur Kleinbuchstaben und Nummern)", Required = true };

        this.txtUrl.DependsFrom = "Name";
        this.txtUrl.ModifyMethods = new string[] { "toLowerCase" };
        List<KeyValuePair<string, string>> regexReplace = new List<KeyValuePair<string, string>>();
        regexReplace.Add(new KeyValuePair<string, string>("[ä]", "ae"));
        regexReplace.Add(new KeyValuePair<string, string>("[ö]", "oe"));
        regexReplace.Add(new KeyValuePair<string, string>("[ü]", "ue"));
        regexReplace.Add(new KeyValuePair<string, string>("[ß]", "ss"));
        regexReplace.Add(new KeyValuePair<string, string>("[ ]", "_"));
        regexReplace.Add(new KeyValuePair<string, string>("[^A-Za-z0-9_]", ""));
        this.txtUrl.RegexReplace = regexReplace;

        txtName.OnChange += txtName_TextChanged;
        txtUrl.OnChange += txtUrl_TextChanged;

        var groupBox = new GroupBox()
        {
            Label = "Allgemeine Eigenschaften"
        };
        groupBox.AddControls(new Control[] { this.txtName, this.txtUrl });

        this.AddControl(groupBox);
    }

    public NameUrlControl(NameUrlControl control)
        : this(control.Name)
    {
        //_nameUrl = control._nameUrl;
        this.InitParameter = control._nameUrl;

        this.NameIsVisible = control.NameIsVisible;
        this.UrlIsVisible = control.UrlIsVisible;
    }

    #region Properties

    private Input txtName { get; set; }
    private Input txtUrl { get; set; }

    #endregion

    #region IInitParameter Member

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public object InitParameter
    {
        set
        {
            _nameUrl = value as NameUrl;

            if (_nameUrl != null)
            {
                txtName.Value = _nameUrl.Name;
                txtUrl.Value = _nameUrl.Url;

                txtUrl.Enabled = _nameUrl.Create;
            }
        }
    }

    #endregion

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool UrlIsVisible
    {
        get { return txtUrl.Visible; }
        set { txtUrl.Visible = value; }
    }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool NameIsVisible
    {
        get { return txtName.Visible; }
        set { txtName.Visible = value; }
    }

    private void txtName_TextChanged(object sender, EventArgs e)
    {
        if (_nameUrl != null)
        {
            _nameUrl.Name = txtName.Value;
        }
    }

    private void txtUrl_TextChanged(object sender, EventArgs e)
    {
        if (_nameUrl != null)
        {
            _nameUrl.Url = txtUrl.Value;
        }
    }

    public void SetName(string name, bool resetUrl = false)
    {
        txtName.Value = name;
        if (resetUrl == true)
        {
            txtUrl.Value = NameToUrl(name);
        }
    }

    #region Helper

    public string NameToUrl(string name)
    {
        if (String.IsNullOrEmpty(name)) return "";

        name = name.ToLower();
        if (txtUrl.RegexReplace != null)
        {
            foreach (var regexReplace in txtUrl.RegexReplace)
            {
                name = Regex.Replace(name, regexReplace.Key, regexReplace.Value);
            }
        }
        return name;
    }

    #endregion
}
