using E.Standard.Extensions.Compare;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.Reflection;
using System;

namespace E.Standard.WebGIS.Tools.Serialization;

[Export(typeof(IApiButton))]
[ToolStorageId("WebGIS.Tools.Serialization/{user}/{0}/{1}/presentations")]
[ToolStorageIsolatedUser(isUserIsolated: true)]
[ToolClient("backend")]
public class UserFavoritiesProjectPresentations : UserFavoritiesPresentations
{
    protected override string ButtonId => "webgis.tools.serialization.userfavoritiesprojectpresentations";

    #region IStorageInteractions

    override public string StoragePathFormatParameter(IBridge bridge, int index)
    {
        if (!bridge.CurrentUser.IsAnonymous)
        {
            switch (index)
            {
                case 0:
                    return bridge.CurrentEventArguments[HiddenPageId].OrTake(bridge.CurrentEventArguments["page"]);
                case 1:
                    return (bridge.CurrentEventArguments[HiddenMapName].OrTake(bridge.CurrentEventArguments["map"])).MapName2StorageDirectory();
            }
        }

        return String.Empty;
    }

    #endregion
}
