#nullable enable

using Api.Core.AppCode.Reflection;

using E.Standard.Api.App;
using E.Standard.Api.App.Endpoints.Metadata;
using E.Standard.Api.App.Reflection;
using E.Standard.Custom.Core;

using Microsoft.AspNetCore.Builder;

namespace Api.Core.AppCode.Extensions.Endpoints;

static public class EndpointConventionsBuilderExtensions
{
    extension<TBuilder>(TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        public TBuilder AddWebGISApiEndpointMetadata(
                bool disableAntiforgery = true,
                // Authentication
                ApiAuthenticationTypes? authTypes = ApiAuthenticationTypes.Hmac,  // should be the default!!
                                                                                  // Etag
                double? etag_expiraionDays = null,
                bool etag_appendResponseHeaders = true,
                // AppRoles
                AppRoles appRoles = AppRoles.WebgisApi  // should be the default!!
            )
        {
            builder.Finally(builder =>
            {
                var reflectionMetadata = new ApiEndpointReclectionMetadata();

                if (authTypes.HasValue)
                {
                    reflectionMetadata.Add(new ApiAuthenticationAttribute(authTypes.Value));
                }

                if (etag_expiraionDays.HasValue)
                {
                    reflectionMetadata.Add(new EtagAttribute(etag_expiraionDays.Value, etag_appendResponseHeaders));
                }

                if (appRoles != AppRoles.None)
                {
                    reflectionMetadata.Add(new AppRoleAttribute(appRoles));
                }

                if (reflectionMetadata.GetAllAttributes().Length > 0)
                {
                    builder.Metadata.Add(reflectionMetadata);
                }
            });

            if (disableAntiforgery)
            {
                builder.DisableAntiforgery();
            }

            return builder;
        }
    }
}

