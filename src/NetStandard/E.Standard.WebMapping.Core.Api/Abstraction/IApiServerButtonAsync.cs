using E.Standard.Localization.Abstractions;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiServerButtonAsync : IApiButton
{
    Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e);
}

public interface IApiServerButtonLocalizableAsync<T> : IApiButton
{
    Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<T> localizer);
}