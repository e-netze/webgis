using E.Standard.Localization.Abstractions;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiServerButton : IApiButton
{
    ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e);
}

public interface IApiServerButtonLocalizable<T> : IApiButton
{
    ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<T> localizer);
}