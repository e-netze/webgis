#nullable enable

using E.Standard.WebMapping.Core.Abstraction;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Extensions;

static public class DisplayExtensions
{
    static public void AddTimeEpoch(this IDisplay display, string serviceId, TimeEpochDefinition? timeEpochDefinition)
    {
        if (timeEpochDefinition == null)
        {
            return;
        }

        display.TimeEpoch ??= new Dictionary<string, TimeEpochDefinition>();
        display.TimeEpoch[serviceId] = timeEpochDefinition;
    }

    static public TimeEpochDefinition? GetTimeEpoch(this IDisplay display, string serviceId)
    {
        if (display.TimeEpoch != null
            && display.TimeEpoch.TryGetValue(serviceId, out TimeEpochDefinition? timeEpochDefinition))
        {
            return timeEpochDefinition;
        }

        return null;
    }
}
