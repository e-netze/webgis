using System;

namespace E.Standard.WebGIS.Tools.Extensions;

static public class CoordinatesExtensions
{
    static public string ToCoordinatesGlobalOid(this int counter)
    {
        return $"{Guid.NewGuid():N}_{counter}";
    }

    static public int GetCounterFromCoordiantesGlobalOid(this string id)
    {
        if (id.Contains("_"))
        {
            return int.Parse(id.Split('_')[1]);
        }

        return 0;
    }
}
