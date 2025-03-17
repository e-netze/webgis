using System.Collections.Generic;

namespace E.Standard.ActiveDirectory;

public interface IAdQuery : IInititalize
{

    string[] UserRoles(string username, bool recursive);

    IEnumerable<AdObject> FindAdObjects(string filter);
}
