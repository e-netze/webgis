using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Geometry;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.Extensions;

static public class NamedSketchExtensions
{
    static public NamedSketch WithName(this NamedSketch namedSketch, string name)
    {
        if (namedSketch != null)
        {
            namedSketch.Name = name;
        }

        return namedSketch;
    }

    static public NamedSketch WithSubText(this NamedSketch namedSketch, string subText)
    {
        if (namedSketch != null)
        {
            namedSketch.SubText = subText;
        }

        return namedSketch;
    }

    static public NamedSketch WithShape(this NamedSketch namedSketch, Shape shape)
    {
        if (namedSketch != null)
        {
            namedSketch.Sketch = shape;
        }

        return namedSketch;
    }

    static public NamedSketch DoZoomToPreview(this NamedSketch namedSketch, bool zoomToPreview = true)
    {
        if (namedSketch != null)
        {
            namedSketch.ZoomOnPreview = zoomToPreview;
        }

        return namedSketch;
    }

    static public NamedSketch DoSetSketch(this NamedSketch namedSketch, bool setSketch = true)
    {
        if (namedSketch != null)
        {
            namedSketch.SetSketch = setSketch;
        }

        return namedSketch;
    }

    static public NamedSketch AddSetter(this NamedSketch namedSketch, IUISetter setter)
        => namedSketch.AddSetters(new[] { setter });

    static public NamedSketch AddSetters(this NamedSketch namedSketch, IEnumerable<IUISetter> setters)
    {
        if (namedSketch != null && setters != null)
        {
            if (namedSketch.UISetters != null)
            {
                namedSketch.UISetters = new List<IUISetter>(namedSketch.UISetters);
            }
            else
            {
                namedSketch.UISetters = new List<IUISetter>();
            }

            ((List<IUISetter>)namedSketch.UISetters).AddRange(setters);
        }

        return namedSketch;
    }
}
