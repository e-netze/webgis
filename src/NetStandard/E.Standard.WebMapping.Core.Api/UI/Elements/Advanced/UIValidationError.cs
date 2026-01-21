using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using System;
using System.IO;
using System.Linq;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UIValidationError : UIDiv
{
    public UIValidationError(string errorMessage)
    {
        if (!String.IsNullOrEmpty(errorMessage))
        {
            errorMessage = $"⚠ {errorMessage}";

            this.css = UICss.ToClass(new string[] { "webgis-validation-error" });
            this.elements = new IUIElement[]
            {
                new UILiteral()
                {
                     literal = errorMessage
                }
            };
        }
    }
}

public class UIValidationErrorSummary : UIDiv
{
    public UIValidationErrorSummary(string errorMessage) : this([errorMessage]) { }
    
    public UIValidationErrorSummary(string[] errorMessages)
    {
        errorMessages = errorMessages?.Where(errorMessages => !String.IsNullOrWhiteSpace(errorMessages)).ToArray();

        if (errorMessages?.Any() == true)
        {
            this.css = UICss.ToClass(new string[] { "webgis-validation-error-summary" });
            IUIElement[] elements = new IUIElement[errorMessages.Length];
            for (int i = 0; i < errorMessages.Length; i++)
            {
                elements[i] = new UILiteral()
                {
                    literal = $"⚠ {errorMessages[i]}"
                };
            }
            this.elements = elements;
        }
    }
}
