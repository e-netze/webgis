using E.Standard.WebMapping.Core.Abstraction;
using System;

namespace E.Standard.WebMapping.Core;

public class ServiceHelper
{
    static public void SetLayerScale(IMapService service, Layer layer)
    {
        if (service == null || layer == null)
        {
            return;
        }

        if (layer.MinScale == 0)
        {
            layer.MinScale = -1;
        }

        if (layer.MaxScale == 0)
        {
            layer.MaxScale = -1;
        }

        if (service.MinScale > 0.0)
        {
            if (layer.MinScale == 0)
            {
                layer.MinScale = service.MinScale;
            }
            else
            {
                layer.MinScale = Math.Max(layer.MinScale, service.MinScale);
            }
        }
        if (service.MaxScale > 0.0)
        {
            if (layer.MaxScale == 0)
            {
                layer.MaxScale = service.MaxScale;
            }
            else
            {
                layer.MaxScale = Math.Min(layer.MaxScale, service.MaxScale);
            }
        }

    }

    static public bool VisibleInScale(IMapService service, IMap map)
    {
        if (map == null || service == null)
        {
            return false;
        }

        if (service.MinScale < 1.0 && service.MaxScale < 1.0)
        {
            return true;
        }

        if (service.MinScale >= 1.0 && map.MapScale < service.MinScale + 0.5)
        {
            return false;
        }

        if (service.MaxScale >= 1.0 && map.MapScale > service.MaxScale - 0.5)
        {
            return false;
        }

        return true;
    }
}
