using System;
using System.Reflection;

namespace E.Standard.CMS.Core;

class VersionInfo
{
    public string AssemblyName = "?";
    public Version AssemblyVersion = new Version();

    public VersionInfo() { }
    public VersionInfo(string assembly, Version version)
    {
        AssemblyName = assembly;
        AssemblyVersion = version;
    }
    public VersionInfo(Assembly assembly)
    {
        if (assembly != null)
        {
            AssemblyName = assembly.GetName().Name;
            AssemblyVersion = assembly.GetName().Version;
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is VersionInfo)
        {
            return AssemblyName.Equals(((VersionInfo)obj).AssemblyName) &&
                   AssemblyVersion.Equals(((VersionInfo)obj).AssemblyVersion);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
