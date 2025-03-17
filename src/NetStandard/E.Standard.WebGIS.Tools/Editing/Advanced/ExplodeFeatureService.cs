using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Advanced;


public class ExplodeFeatureService
{
    async internal Task<ApiEventResponse> MaskResponse(IBridge bridge,
                                                       EditEnvironment.EditTheme editTheme,
                                                       EditFeatureDefinition editFeatureDef,
                                                       Feature feature)
    {
        var mask = await editTheme.ParseMask(bridge,
                                             editFeatureDef.ToEditThemeDefinition(),
                                             EditOperation.Explode,
                                             feature);

        return new ApiEventResponse()
        {
            UIElements = mask.UIElements,
            UISetters = mask.UISetters
        };
    }

    async internal Task ExplodeAndInsertAsync(EditEnvironment editEnvironment,
                                              EditEnvironment.EditTheme editTheme,
                                              Feature originalFeature)
    {
        var originalShape = originalFeature.Shape;

        //
        // DoTo: Transaction? Was ist, wenn Update oder Insert nicht hinhauen!?
        // Vorher überprüfen ob Editthema insert und update Rechte hat...
        // Eventuell den Update erst als letztes machen, damit Orignal Feature erst am schluss überschrieben wird?
        //

        var newFeatures = originalShape.Multiparts.Select(shape =>
        {
            var feature = originalFeature.Clone(false);
            feature.Oid = 0;
            feature.Shape = shape;

            return feature;
        });

        if (!await editEnvironment.InserFeatures(editTheme, newFeatures))
        {
            throw new Exception("Unknon error: Can't insert new features");
        }
    }
}