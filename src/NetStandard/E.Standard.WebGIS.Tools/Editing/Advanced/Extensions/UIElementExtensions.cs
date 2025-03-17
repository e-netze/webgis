using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;

static internal class UIElementExtensions
{
    static public ICollection<IUIElement> AppendRequiredDeleteOriginalHiddenElements(this IEnumerable<IUIElement> currentElements,
                                                                       IEnumerable<int> objectIdSubset = null)
    {
        var elements = new List<IUIElement>(currentElements);

        elements.AddRange(new IUIElement[]
        {
            new UIHidden() {
                id = EditEnvironment.EditTheme.EditNewFeatuerCounterId,
                css = UICss.ToClass(new[]{ UICss.ToolParameter })
            },
            new UIHidden() {
                id = EditEnvironment.EditTheme.EditNewFeaturePreviewDataId,
                css = UICss.ToClass(new[]{ UICss.ToolParameter })
            },
            new UIHidden() {
                id = EditEnvironment.EditTheme.EditOriginalFeaturePreviewDataId,
                css = UICss.ToClass(new[]{ UICss.ToolParameter })
            }
        });

        if (objectIdSubset != null && objectIdSubset.Count() >= 0)
        {
            elements.Add(new UIHidden()
            {
                id = EditEnvironment.EditTheme.EditFeatureIdsSubsetId,
                css = UICss.ToClass(new[] { UICss.ToolParameter })
            });
        }

        return elements;
    }

}
