#nullable enable

using System.Net;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IMapServiceAuthentication : IMapService
{
    string Username { get; }
    string Password { get; }

    string StaticToken { get; }

    int TokenExpiration { get; set; }

    public ICredentials? HttpCredentials { get; set; }

    public string ServiceUrl { get; }
}
