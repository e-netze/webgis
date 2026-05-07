using System.Collections.Generic;

using E.Standard.WebMapping.Core.Api.UI.Elements;

namespace E.Standard.WebMapping.Core.Api.UI.Setters;

public class UISelectOptionsSetter : UISetter
{
    public UISelectOptionsSetter(string elementId, string newValue, IEnumerable<UISelect.Option> options)
        : base(elementId, newValue)
    {
        this.options = options;
    }

    public IEnumerable<UISelect.Option> options { get; set; }
}
