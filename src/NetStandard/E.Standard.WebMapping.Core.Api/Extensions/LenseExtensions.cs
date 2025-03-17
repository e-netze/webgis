using E.Standard.WebMapping.Core.Api.EventResponse.Models;

namespace E.Standard.WebMapping.Core.Api.Extensions;

static public class LenseExtensions
{
    static public Lense WithWidth(this Lense lense, double with)
    {
        if (lense != null)
        {
            lense.Width = with;
        }

        return lense;
    }

    static public Lense WithHeight(this Lense lense, double height)
    {
        if (lense != null)
        {
            lense.Height = height;
        }

        return lense;
    }

    static public Lense WithSize(this Lense lense, double width, double height)
        => lense.WithWidth(width).WithHeight(height);

    static public Lense ZommTo(this Lense lense, bool zoom = true)
    {
        if (lense != null)
        {
            lense.Zoom = zoom;
        }

        return lense;
    }

    static public Lense WithScale(this Lense lense, double? scale)
    {
        if (lense != null)
        {
            lense.LenseScale = scale;
        }

        return lense;
    }

    static public Lense WithScaleControlId(this Lense lense, string controlId)
    {
        if (lense != null)
        {
            lense.ScaleControl = controlId;
        }

        return lense;
    }
}
