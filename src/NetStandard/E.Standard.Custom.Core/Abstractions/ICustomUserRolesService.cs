using System.Collections.Generic;

namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomUserRolesService
{
    IEnumerable<string> CustomUserRoles();
}
