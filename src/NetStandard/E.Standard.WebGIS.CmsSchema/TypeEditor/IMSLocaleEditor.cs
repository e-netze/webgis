using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using Newtonsoft.Json;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class IMSLocaleEditor : UserControl, IUITypeEditor
{
    public IMSLocaleEditor()
    {
        this.AddControl(_infoText);
        this.AddControl(_txtLanguage);
        this.AddControl(_txtCountry);
    }

    #region UI

    InfoText _infoText = new InfoText() { Text = "Das Überschreiben dieser Werte soll nur in Ausnahmefällen durchgeführt werden! Grundsätzlich soll immer versucht werden, die Dienste serverseitig richtig zu Lokalisieren!" };
    Input _txtLanguage = new Input("txtLanguage") { Label = "Language" };
    Input _txtCountry = new Input("txtCountry") { Label = "Country" };

    #endregion

    public IUIControl GetUIControl(ITypeEditorContext context)
    {
        if (context.Value is IMSLocale)
        {
            _txtLanguage.Value = ((IMSLocale)context.Value).Language;
            _txtCountry.Value = ((IMSLocale)context.Value).Country;
        }
        return this;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public object ResultValue
    {
        get
        {
            return new IMSLocale()
            {
                Language = _txtLanguage.Value,
                Country = _txtCountry.Value
            };
        }
    }
}
