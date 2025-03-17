using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class RegExTypeEditor : UserControl, IUITypeEditor
{
    public RegExTypeEditor()
    {
        this.AddControl(_cmbTemplates);
        this.AddControl(_btnSetTemplate);
        this.AddControl(_txtRegex);

        this.AddControl(_gbTest);
        _gbTest.AddControl(_txtTest);
        _gbTest.AddControl(_btnTest);
        _gbTest.AddControl(_txtTestResult);

        _btnTest.OnClick += btnTest_OnClick;
        _btnSetTemplate.OnClick += btnSetTemplate_OnClick;


    }

    #region UIControls

    ComboBox _cmbTemplates = new ComboBox("cmbTemplates") { Label = "Vorlagen" };
    Button _btnSetTemplate = new Button("btnSetTemplate") { Label = "Vorlage übernehmen >>" };
    Input _txtRegex = new Input("result") { Label = "Regulärer Ausdruck (Regex)" };

    GroupBox _gbTest = new GroupBox() { Label = "Mögliche Eingaben testen" };
    Input _txtTest = new Input("txtTest") { Label = "Eingabe" };
    Button _btnTest = new Button("btnTest") { Label = "Testen..." };
    Input _txtTestResult = new Input("txtTestResult") { Label = "Ergebnis", Enabled = false };

    #endregion

    public IUIControl GetUIControl(ITypeEditorContext context)
    {
        if (context.Value is string)
        {
            _txtRegex.Value = (string)context.Value;
        }

        try
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"/regex.xml");

            foreach (XmlNode exnode in doc.SelectNodes("//regex['@name']"))
            {
                if (exnode.Attributes["expression"] == null)
                {
                    continue;
                }

                _cmbTemplates.Options.Add(new ComboBox.Option(exnode.Attributes["expression"].Value, exnode.Attributes["name"].Value));
            }
        }
        catch { }

        return this;
    }

    private void btnSetTemplate_OnClick(object sender, EventArgs e)
    {
        _txtRegex.Value = _cmbTemplates.Value;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public object ResultValue
    {
        get
        {
            return _txtRegex.Value;
        }
    }

    private void btnTest_OnClick(object sender, EventArgs e)
    {
        string text = _txtTest.Value.Trim();

        if (Regex.IsMatch(text, _txtRegex.Value))
        {
            _txtTestResult.Value = "Eingabe Ok";
        }
        else
        {
            _txtTestResult.Value = "Eingabe nicht Ok";
        }
    }
}
