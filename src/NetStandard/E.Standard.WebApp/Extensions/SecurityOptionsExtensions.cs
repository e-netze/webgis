using E.Standard.WebApp.Options;

namespace E.Standard.WebApp.Extensions;

public static class SecurityOptionsExtensions
{
    extension(SecurityOptions options)
    {
        public bool HasBasicAuthentication
            => !String.IsNullOrEmpty(options.EndpointAuthorizationBasicUsername)
            && !String.IsNullOrEmpty(options.EndpointAuthorizationBasicPassword);

        public bool HasUrlPasswordAuthentication
            => !String.IsNullOrEmpty(options.EndpointAuthorizationUrlPassword);

        public bool HasBearerAuthentication
            => !String.IsNullOrEmpty(options.EndpointAuthorizationBearerUsername);

        public bool IsConfigured => options.HasBasicAuthentication || options.HasUrlPasswordAuthentication;
    }
}
