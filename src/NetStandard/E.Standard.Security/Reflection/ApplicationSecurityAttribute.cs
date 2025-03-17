using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Security.Reflection
{
    [Obsolete("Use E.Standard.Security.App assembly")]
    public class ApplicationSecurityAttribute : Attribute
    {
        public ApplicationSecurityAttribute(bool checkSecurity=true)
        {
            CheckSecurity = checkSecurity;
        }

        public bool CheckSecurity { get; set; }
    }
}
