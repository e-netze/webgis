using E.Standard.Localization.Abstractions;
using E.Standard.WebMapping.Core.Api.Bridge;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IServerCommandInstanceProvider<T>
{
    object ServerCommandInstance(IBridge bridge, ApiToolEventArguments e, ILocalizer<T> localizer);
}
