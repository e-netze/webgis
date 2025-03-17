using System.Runtime.InteropServices;

namespace webgis.deploy;

static internal class Platform
{
    static public bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    static public bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    static public bool IsOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    static public string PlatformName =>
        Platform.IsWindows
        ? "win64"
        : Platform.IsLinux
           ? "linux64"
           : "unknown";
}
