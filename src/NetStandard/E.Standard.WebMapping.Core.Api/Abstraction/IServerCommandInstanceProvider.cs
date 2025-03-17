using E.Standard.WebMapping.Core.Api.Bridge;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IServerCommandInstanceProvider
{
    object ServerCommandInstance(IBridge bridge, ApiToolEventArguments e);
}
