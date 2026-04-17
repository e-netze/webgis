using System;

using E.Standard.Api.App;

namespace Api.Core.AppCode.Reflection;

public class AppRoleAttribute : Attribute
{
    public AppRoleAttribute(AppRoles appRole)
    {
        this.AppRole = appRole;
    }

    public AppRoles AppRole { get; }
}
