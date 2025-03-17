using E.Standard.Api.App;
using System;

namespace Api.Core.AppCode.Reflection;

public class AppRoleAttribute : Attribute
{
    public AppRoleAttribute(AppRoles appRole)
    {
        this.AppRole = appRole;
    }

    public AppRoles AppRole { get; }
}
