using System;

namespace E.Standard.ActiveDirectory;

public interface IImpersonateUser : IDisposable
{
    void Impersonate(string domainName, string userName, string password);

    void Undo();
}
