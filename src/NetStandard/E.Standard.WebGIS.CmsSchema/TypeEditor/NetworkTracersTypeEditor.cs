using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using Newtonsoft.Json;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class NetworkTracersTypeEditor : UserControl, IUITypeEditor
{
    public NetworkTracersTypeEditor()
    {
        this.AddControl(Defaults.NotImplementedEditorInfoText);
    }

    public IUIControl GetUIControl(ITypeEditorContext context) { return this; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public object ResultValue { get; }
}
