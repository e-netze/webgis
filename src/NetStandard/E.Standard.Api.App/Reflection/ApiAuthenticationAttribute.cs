using E.Standard.Custom.Core;
using System;

namespace E.Standard.Api.App.Reflection;

public class ApiAuthenticationAttribute : Attribute
{
    public ApiAuthenticationAttribute(ApiAuthenticationTypes authenticationTypes)
    {
        AuthenticationTypes = authenticationTypes;
    }

    public ApiAuthenticationTypes AuthenticationTypes { get; }
}
