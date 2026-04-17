using System;

using E.Standard.Custom.Core;

namespace E.Standard.Api.App.Reflection;

public class ApiAuthenticationAttribute : Attribute
{
    public ApiAuthenticationAttribute(ApiAuthenticationTypes authenticationTypes)
    {
        AuthenticationTypes = authenticationTypes;
    }

    public ApiAuthenticationTypes AuthenticationTypes { get; }
}
