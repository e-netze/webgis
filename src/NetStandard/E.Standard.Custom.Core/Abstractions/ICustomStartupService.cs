using E.Standard.Configuration.Services;
using E.Standard.Security.Cryptography.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomStartupService
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    void Configure(IApplicationBuilder app, ConfigurationService config);

    bool ImplementsCryptoService(IConfiguration config);
    bool ImplementsLicenseService(IConfiguration config);

    CryptoServiceOptions GetCryptoServiceOptions();
}
