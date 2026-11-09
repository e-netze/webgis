using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace E.Standard.Platform;

/// <summary>
/// Central switch for validating TLS server certificates of outbound connections.
/// By default the normal system validation applies. Admins can opt out per application
/// (legacy internal services with self-signed certificates):
/// <list type="bullet">
/// <item>api.config: section <c>security</c>, key <c>allow-untrusted-server-certificates</c></item>
/// <item>portal.config: section <c>security</c>, key <c>allow-untrusted-server-certificates</c></item>
/// <item>cms settings.config: <c>allow_untrusted_server_certificates</c></item>
/// </list>
/// </summary>
public static class TlsCertificateValidation
{
    public static bool AllowUntrustedServerCertificates { get; set; } = false;

    // Signature of HttpClientHandler.ServerCertificateCustomValidationCallback
    public static bool Validate(HttpRequestMessage message, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        => AllowUntrustedServerCertificates || sslPolicyErrors == SslPolicyErrors.None;

    // Signature of ServicePointManager.ServerCertificateValidationCallback
    public static bool ValidateLegacy(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        => AllowUntrustedServerCertificates || sslPolicyErrors == SslPolicyErrors.None;
}
