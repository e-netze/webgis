using Microsoft.AspNetCore.Routing;

namespace E.Standard.WebApp.Abstraction;

public interface IApiEndpoint
{
    void Register(IEndpointRouteBuilder app);
}
