using E.Standard.Extensions.Compare;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace E.Standard.WebGIS.Tools.Editing.Extensions;

static internal class ApiEventResponseExtensions
{
    async static public Task<ApiEventResponse> ToSelectFeaturesResponse(this ApiEventResponse response,
                                                                        IBridge bridge,
                                                                        ApiToolEventArguments e,
                                                                        EditEnvironment editEnvironment,
                                                                        EditThemeDefinition editThemeDef)
    {
        if (editEnvironment.CommitedObjectIds != null && editEnvironment.CommitedObjectIds.Count() > 0)
        {
            // Select feature
            var service = await bridge.GetService(editThemeDef.ServiceId);
            var layer = service?.Layers?.Where(l => l.Id == editThemeDef.LayerId).FirstOrDefault();
            var query = await bridge.GetFirstLayerQuery(editThemeDef.ServiceId, editThemeDef.LayerId);

            if (query != null && layer != null)
            {
                int crsId = e.GetInt(Edit.EditMapCrsId).OrTake(e.MapCrs.ValueOrDefault());
                var sRef = bridge.CreateSpatialReference(crsId);

                var filter = new ApiQueryFilter();
                filter.FeatureSpatialReference = sRef;
                filter.QueryItems["#oid#"] = editEnvironment.CommitedObjectIds.First().ToString();
                filter.Tool = "edit";

                var selectedFeature = await bridge.QueryLayerAsync(editThemeDef.ServiceId, editThemeDef.LayerId, filter);

                if (selectedFeature.Count == 1)
                {
                    return new ApiFeaturesEventResponse(response)
                    {
                        Features = selectedFeature,
                        Query = query,
                        Filter = filter,
                        FeatureSpatialReference = sRef,
                        ZoomToResults = false,
                        SelectResults = true,
                        RefreshSelection = true
                    };
                }
            }
        }

        return response;
    }

    static public void SetEditLayerVisibility(this ApiEventResponse response,
                                              EditThemeDefinition editThemeDef)
    {
        response.SetLayerVisility = new Dictionary<string, Dictionary<string, bool>>()
        {
            {
                editThemeDef.ServiceId,
                new Dictionary<string, bool>()
                {
                    { editThemeDef.LayerId, true }
                }
            }
        };
    }

    static public ApiEventResponse TryAddApplyEditingThemeProperty(
            this ApiEventResponse response,
            ApiToolEventArguments e,
            string editThemeDefintionParameter = "_editfield_edittheme_def"
        )
    {
        try
        {
            if (!String.IsNullOrEmpty(e[editThemeDefintionParameter]))
            {
                var editFeatureDef = ApiToolEventArguments.FromArgument<EditThemeDefinition>(e[editThemeDefintionParameter]);
                if (!String.IsNullOrEmpty(editFeatureDef.EditThemeId)
                    && !String.IsNullOrEmpty(editFeatureDef.ServiceId))
                {
                    response.ApplyEditingTheme = new EditingThemeDefDTO(
                            editFeatureDef.EditThemeId,
                            editFeatureDef.ServiceId
                        );
                }
            }
        }
        catch { }

        return response;
    }
}
