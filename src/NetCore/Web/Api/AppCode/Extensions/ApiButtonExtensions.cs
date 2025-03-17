using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;

namespace Api.Core.AppCode.Extensions;

static public class ApiButtonExtensions
{
    static public object ServerCommandInstance(this IApiButton button, IBridge bridge, ApiToolEventArguments e)
    {
        if (button == null)
        {
            return null;
        }

        if (button is IServerCommandInstanceProvider)
        {
            return ((IServerCommandInstanceProvider)button).ServerCommandInstance(bridge, e);
        }

        return button;
    }
}
