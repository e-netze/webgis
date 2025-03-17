using E.Standard.Api.App;
using System;

namespace Api.Core.AppCode.Exceptions;

public class AppRoleNotAllowedException : Exception
{
    public AppRoleNotAllowedException(AppRoles appRole)
        : base($"AppRole {appRole} is not allowed for this instance")
    {

    }
}
