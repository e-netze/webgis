using Microsoft.Extensions.DependencyInjection;

namespace E.Standard.Security.App.Extensions.DependencyInjection;

public class ApplicationSecurityUserManagerBuilder
{
    public ApplicationSecurityUserManagerBuilder(IServiceCollection services)
    {
        this.Services = services;
    }

    public readonly IServiceCollection Services;
}
