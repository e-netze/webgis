using E.Standard.Extensions.Compare;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Graphics.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;

static internal class BridgeExtensions
{
    static public Task<IUISetter[]> RequiredDeleteOriginalSetters(this IBridge bridge,
                                                                  IEnumerable<Shape> newShapes,
                                                                  Shape originalShape)
    {
        return bridge.RequiredDeleteOriginalSetters(newShapes, new Shape[] { originalShape });
    }

    static public Task<IUISetter[]> RequiredDeleteOriginalSetters(this IBridge bridge,
                                                                  Shape newShape,
                                                                  IEnumerable<Shape> originalShapes)
    {
        return bridge.RequiredDeleteOriginalSetters(new Shape[] { newShape }, originalShapes);
    }

    async static public Task<IUISetter[]> RequiredDeleteOriginalSetters(this IBridge bridge,
                                                                        IEnumerable<Shape> newShapes,
                                                                        IEnumerable<Shape> originalShapes,
                                                                        IEnumerable<int> objectIdSubset = null)
    {
        var bbox = newShapes.BoundingBox().UnionWith(originalShapes.BoundingBox());

        var newFeaturePreviewData = await newShapes.CreateImage(bridge, 320, 240, bbox);
        var originalFeaturePreviewData = await originalShapes.CreateImage(bridge, 320, 240, bbox);

        List<IUISetter> setters = new List<IUISetter>();

        setters.AddRange(new IUISetter[]
        {
            new UISetter(EditEnvironment.EditTheme.EditNewFeatuerCounterId, newShapes.Count().ToString()),
            new UISetter(EditEnvironment.EditTheme.EditNewFeaturePreviewDataId, newFeaturePreviewData != null ? Convert.ToBase64String(newFeaturePreviewData) : null),
            new UISetter(EditEnvironment.EditTheme.EditOriginalFeaturePreviewDataId, originalFeaturePreviewData != null ? Convert.ToBase64String(originalFeaturePreviewData) : null)
        });

        if (objectIdSubset != null && objectIdSubset.Count() >= 0)
        {
            setters.Add(new UISetter(EditEnvironment.EditTheme.EditFeatureIdsSubsetId,
                String.Join(",", objectIdSubset)));
        }

        return setters.ToArray();
    }


    async static internal Task<Feature> GetEditThemeFeature(this IBridge bridge, EditThemeDefinition editThemeDef, int oid, QueryFields queryFields = QueryFields.All, SpatialReference featureSpatialReference = null)
    {

        var features = await bridge.QueryLayerAsync(editThemeDef.ServiceId,
                                                    editThemeDef.LayerId,
                                                    new ApiOidsFilter(new[] { oid })
                                                    {
                                                        Fields = queryFields,
                                                        FeatureSpatialReference = featureSpatialReference
                                                    });

        return features?.Count > 0 ? features[0] : null;
    }

    static internal void ValidateMask(this IBridge bridge, EditEnvironment.EditTheme.Validation validation, string value)
    {
        if (validation == null)
        {
            return;
        }

        if (value == null)
        {
            throw new Exception(validation.Message);
        }

        string validator = validation.Validator;

        if (validator.ToLower().StartsWith("role-parameter:"))  // eg: gem_nr in role-parameter:GKZ
        {
            validator = validator.Substring("role-parameter:".Length);
            var validators = bridge.CurrentUser?.UserRoleParameters?
                       .Where(p => p.StartsWith($"{validator}="))
                       .Select(p => p.Substring(validator.Length + 1))
                       .ToArray();
            if (validators == null || validator.Length == 0)
            {
                throw new Exception(validation.Message);
            }

            validator = String.Join(",", validators);
        }

        var message = validation.Message.OrTake("Validation error!");

        switch (validation.Operator)
        {
            case "=":
                if (value.ToLower() != validator.ToLower())
                {
                    throw new Exception(message);
                }

                break;
            case "in":
                if (validator.Split(',').Where(v => v.Trim().ToLower() == value.Trim().ToLower()).Count() == 0)
                {
                    throw new Exception(message);
                }

                break;
            case "IN":
                if (validator.Split(',').Where(v => v == value).Count() == 0)
                {
                    throw new Exception(message);
                }

                break;
            case "inside":
                foreach (string valItem in value.Replace(";", ",").Split(','))
                {
                    if (validator.Split(',').Where(v => v.Trim().ToLower() == valItem.Trim().ToLower()).Count() == 0)
                    {
                        throw new Exception(message);
                    }
                }
                break;
            case "INSIDE":
                foreach (string valItem in value.Replace(";", ",").Split(','))
                {
                    if (validator.Split(',').Where(v => v.Trim().ToLower() == valItem.Trim().ToLower()).Count() == 0)
                    {
                        throw new Exception(message);
                    }
                }
                break;
            default: // ==
                if (value != validator)
                {
                    throw new Exception(message);
                }

                break;
        }
    }
}
