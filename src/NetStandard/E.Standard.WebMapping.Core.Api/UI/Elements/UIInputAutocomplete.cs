using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Extensions;
using Newtonsoft.Json;
using System;
using System.Text;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIInputAutocomplete : UIValidation, IUIElementLabel
{
    public UIInputAutocomplete(string dataSource, int minLength = 1)
        : base("input-autocomplete")
    {
        this.source = dataSource;
        this.minlength = minLength;
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string label { get; set; }

    public string source { get; set; }
    public int minlength { get; set; }

    public static string MethodSource(WebMapping.Core.Api.Bridge.IBridge bridge, Type toolType, string method, object parameters = null, bool encryptParameters = false)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(bridge.AppRootUrl + "/rest/toolmethod?toolId=" + toolType.ToToolId() + "&method=" + method);

        if (encryptParameters)
        {
            sb.Append("&_encparameters=true");
        }

        if (parameters != null)
        {
            foreach (var propInfo in parameters.GetType().GetProperties())
            {
                var val = propInfo.GetValue(parameters) != null ? propInfo.GetValue(parameters).ToString() : String.Empty;

                sb.Append("&");
                sb.Append(propInfo.Name);
                sb.Append("=");
                sb.Append(encryptParameters ? bridge.SecurityEncryptString(val) : val);
            }
        }

        return sb.ToString();
    }
}
