using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Geometry;
using System;
using static E.Standard.WebMapping.Core.CoreApiGlobals;

namespace E.Standard.WebGIS.Tools.Extensions;

static internal class FilterExtensions
{
    static public void SetClickQueryShape(this ApiSpatialFilter filter, IQueryBridge query, double mapScale, ApiToolEventArguments e)
    {
        var click = e.ToMapProjectedClickEvent();

        double pixelTolerance = e.IdentifyTolerance(query);

        if (pixelTolerance == 0.0)
        {
            filter.QueryShape = new Point(click.WorldX, click.WorldY);
        }
        else
        {
            double toleranceX, toleranceY;
            toleranceX = toleranceY = pixelTolerance * mapScale / (96.0 / 0.0254);
            if (click.SRef.IsProjective == false)
            {
                toleranceX = toleranceX * ToDeg / WorldRadius * Math.Cos(click.Latitude * ToRad);
                toleranceY = toleranceY * ToDeg / WorldRadius;
            }
            filter.QueryShape = new Envelope(
                click.WorldX - toleranceX, click.WorldY - toleranceY,
                click.WorldX + toleranceX, click.WorldY + toleranceY);
        }
    }
}