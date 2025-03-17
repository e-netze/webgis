using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Renderer;
using System;
using System.Collections.Generic;
using System.Linq;

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
}
