#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Renderer;
using E.Standard.WebMapping.Core.Renderer.UserRenderers;

namespace Api.Core.AppCode.Extensions;


static public class ServiceExtensions
{
    static public bool CmsThemeIncludedForStrictUse(this IMapService2 service2, ILayer layer)
    {
        if (service2?.ServiceThemes != null)
        {
            return service2.ServiceThemes.Where(t => t.Id == layer.ID || t.Name == layer.Name).Any();
        }

        return true;
    }

    static public void AddLabeling(this IMapService service,
                                   IEnumerable<LabelingDefinitionDTO> labelingDefinitions,
                                   CacheService cache,
                                   CmsDocument.UserIdentification ui)
    {
        if (service == null || labelingDefinitions == null || !labelingDefinitions.Any())
        {
            return;
        }

        var serviceLabelings = cache.GetLabeling(service.Url, ui).labelings;

        if (serviceLabelings != null && serviceLabelings.Count() > 0)
        {
            foreach (var labelingDefinition in labelingDefinitions)
            {
                if (!String.IsNullOrWhiteSpace(labelingDefinition.ServiceId) && labelingDefinition.ServiceId != service.Url)
                {
                    continue;
                }

                var serviceLabeling = serviceLabelings.Where(s => s.Id == labelingDefinition.Id).FirstOrDefault();
                if (serviceLabeling == null)
                {
                    continue;
                }

                var layer = service.Layers.FindByLayerId(serviceLabeling.LayerId) as ILabelableLayer;
                if (layer == null)
                {
                    continue;
                }

                layer.UseLabelRenderer = true;
                var labelRenderer = new LabelRenderer()
                {
                    LabelField = labelingDefinition.Field,
                    FontSize = labelingDefinition.FontSize,
                    FontColor = labelingDefinition.FontColor.HexToColor(),
                    BorderColor = labelingDefinition.BorderColor.HexToColor()
                };

                if (labelRenderer.BorderColor.A != 0)
                {
                    labelRenderer.LabelBorderStyle = LabelRenderer.LabelBorderStyleEnum.blockout;
                }

                LabelRenderer.LabelStyleEnum labelStyle;
                if (Enum.TryParse<LabelRenderer.LabelStyleEnum>(labelingDefinition.FontStyle, true, out labelStyle))
                {
                    labelRenderer.LabelStyle = labelStyle;
                }

                LabelRenderer.HowManyLabelsEnum howManyLabels;
                if (Enum.TryParse<LabelRenderer.HowManyLabelsEnum>(labelingDefinition.HowManyLabels, true, out howManyLabels))
                {
                    labelRenderer.HowManyLabels = howManyLabels;
                }

                labelRenderer.LabelBorderStyle = LabelRenderer.LabelBorderStyleEnum.glowing;

                layer.LabelRenderer = labelRenderer;
            }
        }
    }

    /// <summary>
    /// Applies the user defined renderers (request parameter "renderers") to the layers of the service.
    /// Renderers are only applied, if allowed (api.config, service capabilities, CMS) and valid.
    /// Invalid renderers are ignored => layer is drawn with its default renderer.
    /// </summary>
    /// <returns>Ids of the layers, where the user renderer was rejected</returns>
    static public IEnumerable<string> ApplyUserRenderers(this IMapService service,
                                                         IEnumerable<UserRendererDefinitionDTO>? userRendererDefinitions,
                                                         CacheService cache,
                                                         CmsDocument.UserIdentification ui)
    {
        var rejected = new List<string>();

        if (service == null || userRendererDefinitions == null)
        {
            return rejected;
        }

        var serviceDefinitions = userRendererDefinitions
            .Where(d => d?.Renderer != null && (String.IsNullOrWhiteSpace(d.ServiceId) || d.ServiceId == service.Url))
            .ToArray();

        if (serviceDefinitions.Length == 0)
        {
            return rejected;
        }

        if (!UserRendererSettings.Allowed
            || service is not IMapServiceUserRenderers { SupportsUserRenderers: true }
            || !cache.IsServiceUserRenderersAllowed(service.Url, ui))
        {
            return serviceDefinitions.Select(d => d.LayerId).ToArray();
        }

        var allowedQueryBuilderFieldNames = cache.GetAllowedQueryBuilderFields(service.Url, ui)
            .Select(f => f.Name)
            .Where(n => !String.IsNullOrEmpty(n))
            .ToArray();
        var validator = new UserRendererValidator();

        foreach (var definition in serviceDefinitions)
        {
            var layer = service.Layers.FindByLayerId(definition.LayerId);

            if (layer is not IUserRenderableLayer { SupportsUserRenderer: true } userRenderableLayer)
            {
                rejected.Add(definition.LayerId);
                continue;
            }

            var layerFieldNames = layer.Fields?.Select(f => f.Name).ToArray() ?? [];
            var allowedFields = allowedQueryBuilderFieldNames.Any(n => n == "*")
                ? layerFieldNames
                : allowedQueryBuilderFieldNames.Where(n => layerFieldNames.Any(l => l.Equals(n, StringComparison.OrdinalIgnoreCase)));

            if (!validator.IsValid(definition.Renderer, layer.Type, allowedFields))
            {
                rejected.Add(definition.LayerId);
                continue;
            }

            userRenderableLayer.UserRenderer = definition.Renderer;
        }

        return rejected;
    }

    static public string[]? ServiceCapabilities(this IMapService service)
    {
        if (service is IMapServiceCapabilities mapServiceCapabilities)
        {
            return mapServiceCapabilities.Capabilities?.Select(c => c.ToString().ToLower()).ToArray();
        }

        return null;
    }
}
