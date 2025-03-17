using System;

namespace E.Standard.Security.App.Reflection;

public class ApplicationSecurityAttribute : Attribute
{
    public ApplicationSecurityAttribute(bool checkSecurity = true)
    {
        CheckSecurity = checkSecurity;
    }

    public bool CheckSecurity { get; set; }
}
