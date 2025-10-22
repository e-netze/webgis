using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Services
{
    public class DynamicCorsPolicyProvider : ICorsPolicyProvider
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DynamicCorsPolicyProvider(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public Task<CorsPolicy> GetPolicyAsync(
            HttpContext context, 
            string policyName)
        {
            using var scope = _scopeFactory.CreateScope();

            var urlLHelper = scope.ServiceProvider
                .GetRequiredService<UrlHelperService>();

            string apiUrl = urlLHelper.ApiUrl(context.Request);

            var builder = new CorsPolicyBuilder();
            builder
                .WithOrigins(apiUrl)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();

            return Task.FromResult(builder.Build());
        }
    }
}
