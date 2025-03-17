using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using Newtonsoft.Json;
using System;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class MultilineStringEditor : UserControl, IUITypeEditor
{
    public MultilineStringEditor()
    {
        this.AddControl(textArea);
    }

    #region UI

    TextArea textArea = new TextArea("multlineText") { Label = "Text" };

    #endregion

    public IUIControl GetUIControl(ITypeEditorContext context)
    {
        textArea.Value = context.Value?.ToString();

        return this;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public object ResultValue
    {
        get
        {
            return textArea.Value ?? String.Empty;
        }
    }
}
