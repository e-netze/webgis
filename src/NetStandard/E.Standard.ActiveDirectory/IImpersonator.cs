using System;

namespace E.Standard.ActiveDirectory;

public interface IImpersonator
{
    IDisposable ImpersonateContext(bool impersonate);
}
