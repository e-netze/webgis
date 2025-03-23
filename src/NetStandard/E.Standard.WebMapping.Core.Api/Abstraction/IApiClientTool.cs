using E.Standard.Localization.Abstractions;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiClientTool : IApiTool
{
    ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e);
}

public interface IApiClientToolLocalizable<T> : IApiTool
{
    ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<T> localizer);
}
