using E.Standard.Extensions.Compare;
using E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using System;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Mobile.Advanced.Extentions;

static internal class BridgeExtensions
{
    static public EditEnvironment GetEditEnvironment(this IBridge bridge, ApiToolEventArguments e, string editFieldPrefix = null)
    {
        return new EditEnvironment(bridge, e, editFieldPrefix: editFieldPrefix)
        {
            CurrentMapScale = e.MapScale.GetValueOrDefault().OrTake(e.GetDouble(Edit.EditMapScaleId)),
            CurrentMapSrsId = e.MapCrs.GetValueOrDefault().OrTake(e.GetInt(Edit.EditMapCrsId))
        };
    }

    static public EditEnvironment GetEditEnvironment(this IBridge bridge, ApiToolEventArguments e, EditThemeDefinition editThemeDefintion, string editFieldPrefix = null)
    {
        return new EditEnvironment(bridge, editThemeDefintion, editFieldPrefix: editFieldPrefix)
        {
            CurrentMapScale = e.MapScale.GetValueOrDefault().OrTake(e.GetDouble(Edit.EditMapScaleId)),
            CurrentMapSrsId = e.MapCrs.GetValueOrDefault().OrTake(e.GetInt(Edit.EditMapCrsId))
        };
    }

    static public (EditEnvironment editEnvironment,
                   EditEnvironment.EditTheme editTheme,
                   EditThemeDefinition editThemeDefintion,
                   EditFeatureDefinition editFeatureDeftion) GetEditFeatureDefinitionContext(this IBridge bridge, ApiToolEventArguments e, string editFieldPrefix = null)
    {
        var editEnvironment = bridge.GetEditEnvironment(e, editFieldPrefix);
        var feature = editEnvironment.GetFeature(bridge, e);

        return (
            editEnvironment,
            editEnvironment[e],
            editEnvironment.EditThemeDefinition,
            editEnvironment.EditThemeDefinition.ToEditFeatureDefintion(feature));
    }

    async static public Task<(EditEnvironment editEnvironment,
                              EditEnvironment.EditTheme editTheme,
                              EditFeatureDefinition editFeatureDefinition)> QueryEditFeatureDefintionContext(this IBridge bridge, ApiToolEventArguments e, string editFieldPrefix = null)
    {
        var editEnvironment = bridge.GetEditEnvironment(e, editFieldPrefix);
        var featureOid = editEnvironment.GetFeatureOid(bridge, e);
        var editThemeDef = editEnvironment.EditThemeDefinition;

        var feature = await bridge.GetEditThemeFeature(editThemeDef, featureOid);
        if (feature == null)
        {
            throw new Exception("Can't query source feature");
        }

        return (
            editEnvironment,
            editEnvironment[e],
            editThemeDef.ToEditFeatureDefintion(feature));
    }
}
