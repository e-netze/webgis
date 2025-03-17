using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.Api.App.Services;

public interface IExpectableUserRoleNamesProvider
{
    string GroupName { get; }
    Task<IEnumerable<string>> ExpectableUserNames();

    Task<IEnumerable<string>> ExpectableUserRoles();
}
