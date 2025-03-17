using System.Collections.Generic;

namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomSubscriberIdenticationService
{
    bool HasSubscriberRole(IEnumerable<string> currentUserRoles);

    string ToSubscriberFullName(string username, IEnumerable<string> currentUserRoles);
}
