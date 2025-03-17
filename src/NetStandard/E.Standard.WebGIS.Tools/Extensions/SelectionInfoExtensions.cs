using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using System;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Extensions;

static public class SelectionInfoExtensions
{
    async static public Task<ILayerBridge> GetLayer(this ApiToolEventArguments.SelectionInfoClass seletionInfo, IBridge bridge)
    {
        if (seletionInfo == null)
        {
            return null;
        }

        return (await bridge?.GetService(seletionInfo.ServiceId))?.FindLayer(seletionInfo.LayerId);
    }

    async static public Task<string> GetQueryName(this ApiToolEventArguments.SelectionInfoClass selectionInfo, IBridge bridge)
    {
        if (selectionInfo == null)
        {
            return string.Empty;
        }

        return (await bridge?.GetQuery(selectionInfo.ServiceId, selectionInfo.QueryId))?.Name ?? String.Empty;
    }

    static public string QueryGlobalId(this ApiToolEventArguments.SelectionInfoClass selectionInfo)
    {
        if (selectionInfo == null)
        {
            return string.Empty;
        }

        return $"{selectionInfo.ServiceId}:{selectionInfo.QueryId}";
    }

    static public int ObjectCount(this ApiToolEventArguments.SelectionInfoClass selectionInfo)
    {
        if (selectionInfo?.ObjectIds == null)
        {
            return 0;
        }

        return selectionInfo.ObjectIds.Length;
    }
}
