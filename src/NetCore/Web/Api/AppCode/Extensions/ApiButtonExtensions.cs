using E.Standard.DependencyInjection;
using E.Standard.DependencyInjection.Abstractions;
using E.Standard.Extensions.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;

namespace Api.Core.AppCode.Extensions;

static public class ApiButtonExtensions
{
    static public object ServerCommandInstance(this IApiButton button, IBridge bridge, ApiToolEventArguments e, IDependencyProvider dependencyProvider)
    {
        if (button == null)
        {
            return null;
        }

        if (button.GetType().ImplementsAnyInterface(typeof(IServerCommandInstanceProvider<>)))
        {
            return Invoker.Invoke<object>(button, "ServerCommandInstance", dependencyProvider);
        }

        return button;
    }
}
