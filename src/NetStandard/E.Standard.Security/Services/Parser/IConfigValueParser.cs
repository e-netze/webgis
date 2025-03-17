using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Security.Services.Parser
{
    [Obsolete("Use E.Standard.Security.App assembly")]
    public interface IConfigValueParser
    {
        string Parse(string configValue);
    }
}
