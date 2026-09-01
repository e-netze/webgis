using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UISketchInfoContainer : UIElement
{
    /// <param name="allowFallback">
    /// If the map does not provide a sketch-info overlay container (docked above the coordinate
    /// display), the client used to fall back to rendering this element inline in the tool dialog.
    /// Set to false to disable that fallback: the element is then simply ignored by
    /// webgis_uibuilder.js when no overlay container is available, instead of taking up space in
    /// the dialog again.
    /// </param>
    public UISketchInfoContainer(bool allowFallback = true)
        : base("sketch-info-container")
    {
        this.AllowFallback = allowFallback;
    }

    [JsonProperty("allow_fallback", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("allow_fallback")]
    public bool AllowFallback { get; set; }
}
