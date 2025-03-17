//using E.Standard.WebGIS.Core.Models;
using E.Standard.WebMapping.Core.Api;

namespace E.Standard.WebGIS.Tools.Extensions;

static public class ToolArgumentsExtension
{
    static public bool UseMobileBehavior(this ApiToolEventArguments e)
    {
        if (e?.DeviceInfo == null)
        {
            return true;
        }

        return e.DeviceInfo.IsMobileDevice == true && e.DeviceInfo.ScreenWidth <= 1024;
    }

    static public bool UseAdvancedToolsBehaviour(this ApiToolEventArguments e)
    {
        if (e?.DeviceInfo == null)
        {
            return false;
        }

        return e.DeviceInfo.AdvancedToolBehaviour;
    }

    static public bool UseSimpleToolsBehaviour(this ApiToolEventArguments e)
    {
        return !e.UseAdvancedToolsBehaviour();
    }
}
