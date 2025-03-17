using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Api.Bridge;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;

static internal class EditThemeDefinitionExtensions
{
    static public EditFeatureDefinition ToEditFeatureDefintion(this EditThemeDefinition editThemeDef, Feature feature)
    {
        return new EditFeatureDefinition()
        {
            ServiceId = editThemeDef.ServiceId,
            LayerId = editThemeDef.LayerId,
            FeatureOid = feature.Oid,
            EditThemeId = editThemeDef.EditThemeId,
            EditThemeName = editThemeDef.EditThemeName,
            Feature = feature
        };
    }

    async static public Task<LayerGeometryType> GeometryTpyeOrLayerGeometryType(this EditEnvironment.EditTheme editTheme, IBridge bridge, string serviceId)
    {
        if (editTheme == null)
        {
            return LayerGeometryType.unknown;
        }

        if (editTheme.GeometryType != LayerGeometryType.unknown)
        {
            return editTheme.GeometryType;
        }

        var editThemeBridge = bridge.GetEditTheme(serviceId, editTheme.Name);

        return (await bridge.GetService(serviceId))?
                            .FindLayer(editThemeBridge.ThemeId)?
                            .GeometryType ?? LayerGeometryType.unknown;
    }
}
