using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Security.Services
{
    [Obsolete("Use E.Standard.Security.App assembly")]
    public interface ISecurityConfigurationService
    {
        string this[string key] { get; }
    }
}
