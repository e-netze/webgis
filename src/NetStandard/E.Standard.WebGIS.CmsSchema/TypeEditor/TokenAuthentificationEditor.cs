using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using Newtonsoft.Json;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class TokenAuthentificationEditor : UserControl, IUITypeEditor
{
    public TokenAuthentificationEditor()
    {
        this.AddControl(txtToken);
    }

    #region UI

    Input txtToken = new Input("txtToken") { Label = "Token" };

    #endregion

    public IUIControl GetUIControl(ITypeEditorContext context)
    {
        txtToken.Value = context.Value?.ToString();

        return this;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public object ResultValue
    {
        get
        {
            return txtToken.Value;
        }
    }
}
