using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;

static public class ApiToolEventArgumentsExtensions
{
    async static internal Task<FeatureCollection> FeaturesFromSelectionAsync(this ApiToolEventArguments e,
                                                                             IBridge bridge,
                                                                             QueryFields queryFields = QueryFields.All,
                                                                             SpatialReference featureSpatialReference = null,
                                                                             int[] objectIdsSubset = null,
                                                                             bool suppressResolveAttributeDomains = true)
    {
        if (e.SelectionInfo != null)
        {
            int[] selectedObjectIds = e.SelectionInfo.ObjectIds;

            if (objectIdsSubset != null)
            {
                var selectedSubsetObjectIds = new List<int>();

                foreach (var id in objectIdsSubset)
                {
                    if (selectedObjectIds.Contains(id))
                    {
                        selectedSubsetObjectIds.Add(id);
                    }
                }

                selectedObjectIds = selectedSubsetObjectIds.ToArray();
            }

            if (selectedObjectIds == null || selectedObjectIds.Length == 0)
            {
                return new FeatureCollection();
            }

            var features = await bridge.QueryLayerAsync(e.SelectionInfo.ServiceId,
                                                        e.SelectionInfo.LayerId,
                                                        new ApiOidsFilter(selectedObjectIds.ToArray())
                                                        {
                                                            Fields = queryFields,
                                                            FeatureSpatialReference = featureSpatialReference,
                                                            SuppressResolveAttributeDomains = suppressResolveAttributeDomains
                                                        });

            return features;
        }

        return new FeatureCollection();
    }

    async static internal Task<Feature> FirstFeatureFromSelection(this ApiToolEventArguments e,
                                                                  IBridge bridge,
                                                                  QueryFields queryFields = QueryFields.All,
                                                                  SpatialReference featureSpatialReference = null,
                                                                  bool SuppressResolveAttributeDomains = true)
    {
        if (e.SelectionInfo != null && e.SelectionInfo.ObjectIds.Length > 0)
        {
            int[] selectedObjectIds = new[] { e.SelectionInfo.ObjectIds.First() };

            var features = await bridge.QueryLayerAsync(e.SelectionInfo.ServiceId,
                                                        e.SelectionInfo.LayerId,
                                                        new ApiOidsFilter(selectedObjectIds.ToArray())
                                                        {
                                                            Fields = queryFields,
                                                            FeatureSpatialReference = featureSpatialReference,
                                                            SuppressResolveAttributeDomains = SuppressResolveAttributeDomains
                                                        });

            return features?.FirstOrDefault();
        }

        return null;
    }

    static internal EditEnvironment.EditTheme EditThemeFromSelection(this ApiToolEventArguments e, IBridge bridge, EditEnvironment editEnvironment = null)
    {
        if (e.SelectionInfo != null)
        {
            var editThemeBridge = bridge.GetEditThemes(e.SelectionInfo.ServiceId).Where(a => a.LayerId == e.SelectionInfo.LayerId).FirstOrDefault();
            if (editThemeBridge != null)
            {

                editEnvironment = editEnvironment != null ? editEnvironment : new EditEnvironment(bridge, e,
                    defaultEditThemeDefintion: new EditThemeDefinition()
                    {
                        ServiceId = e.SelectionInfo.ServiceId,
                        LayerId = e.SelectionInfo.LayerId,
                        EditThemeId = editThemeBridge.ThemeId
                    });

                return editEnvironment[editThemeBridge.ThemeId];
            }
        }

        return null;
    }

    static internal IEditThemeBridge FirstEditThemeBridgeFromSelection(this ApiToolEventArguments e, IBridge bridge)
    {
        if (e.SelectionInfo != null)
        {
            return bridge.GetEditThemes(e.SelectionInfo.ServiceId).Where(a => a.LayerId == e.SelectionInfo.LayerId).FirstOrDefault();
        }

        return null;
    }

    static internal EditEnvironment EditEvironmentFromSelectionInfoService(this ApiToolEventArguments e, IBridge bridge, string editThemeId)
    {
        if (e.SelectionInfo == null)
        {
            throw new Exception("No features selected");
        }

        var editTheme = bridge.GetEditTheme(e.SelectionInfo.ServiceId, editThemeId);
        if (editTheme == null)
        {
            throw new Exception($"EditTheme {editThemeId} not found in service {e.SelectionInfo.ServiceId}");
        }

        return new EditEnvironment(bridge, e,
                    defaultEditThemeDefintion: new EditThemeDefinition()
                    {
                        ServiceId = e.SelectionInfo.ServiceId,
                        LayerId = editTheme.LayerId,
                        EditThemeId = editTheme.ThemeId
                    });
    }

    static internal EditFeatureDefinition EditFeatureDefinitionFromSelection(this ApiToolEventArguments e, IBridge bridge, Feature feature)
    {
        if (e.SelectionInfo != null)
        {
            var editThemeBridge = bridge.GetEditThemes(e.SelectionInfo.ServiceId).Where(a => a.LayerId == e.SelectionInfo.LayerId).FirstOrDefault();
            if (editThemeBridge != null)
            {

                return new EditFeatureDefinition()
                {
                    ServiceId = e.SelectionInfo.ServiceId,
                    LayerId = e.SelectionInfo.LayerId,
                    FeatureOid = feature.Oid,
                    EditThemeId = editThemeBridge.ThemeId,
                    EditThemeName = editThemeBridge.Name,
                    Feature = feature
                };
            }
        }

        return null;
    }
}
