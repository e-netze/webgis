using E.Standard.WebMapping.Core.Api.Reflection;

namespace E.Standard.WebGIS.Tools.Editing.Models;

class EditFeatureDefinition
{
    [ArgumentObjectProperty(0)]
    public string ServiceId { get; set; }

    [ArgumentObjectProperty(1)]
    public string LayerId { get; set; }

    [ArgumentObjectProperty(2)]
    public long FeatureOid { get; set; }

    [ArgumentObjectProperty(3)]
    public string EditThemeId { get; set; }

    [ArgumentObjectProperty(4)]
    public string EditThemeName { get; set; }

    public WebMapping.Core.Feature Feature { get; set; }

    public EditThemeDefinition ToEditThemeDefinition()
    {
        return new EditThemeDefinition()
        {
            ServiceId = this.ServiceId,
            LayerId = this.LayerId,
            EditThemeId = this.EditThemeId,
            EditThemeName = this.EditThemeName
        };
    }
}
