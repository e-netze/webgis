using E.Standard.Localization.Abstractions;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiServerTool : IApiTool
{
    ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e);
    ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e);
}

public interface IApiServerToolLocalizable<T> : IApiTool
{
    ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<T> localizer);
    ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e, ILocalizer<T> localizer);
}
