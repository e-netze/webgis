using System;

namespace E.Standard.GeoRSS20;

public class RssGeometry
{
    public static RssGeometry FromItem(item item)
    {
        if (item.longSpecified &&
            item.latSpecified)
        {
            return new RssPoint(item.lat, item.@long);
        }
        if (!String.IsNullOrEmpty(item.point))
        {
            return new RssPoint(item.point);
        }
        if (!String.IsNullOrEmpty(item.box))
        {
            return new RssBox(item.box);
        }
        if (!String.IsNullOrEmpty(item.line))
        {
            return new RssLine(item.line);
        }
        return null;
    }

    public static void AppendGeometry(item item, RssGeometry geometry, RssGeometryType type)
    {
        if (item == null || geometry == null)
        {
            return;
        }

        if (type == RssGeometryType.georss_simple)
        {
            if (geometry is RssPoint)
            {
                item.point = ((RssPoint)geometry).ToString();
            }
            else if (geometry is RssBox)
            {
                item.box = ((RssBox)geometry).ToString();
            }
        }
        else if (type == RssGeometryType.w3c_geo)
        {
            if (geometry is RssPoint)
            {
                item.longSpecified = item.latSpecified = true;
                item.@long = ((RssPoint)geometry).Long;
                item.lat = ((RssPoint)geometry).Lat;
            }
        }
    }
}
