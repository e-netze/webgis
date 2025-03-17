using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Geometry;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.EventResponse.Models;

public class NamedSketch
{
    public NamedSketch()
    {
        this.SetSketch = true;
    }

    public string Name { get; set; }
    public string SubText { get; set; }
    public Shape Sketch { get; set; }
    public bool ZoomOnPreview { get; set; }
    public bool SetSketch { get; set; }

    public IEnumerable<IUISetter> UISetters { get; set; }
}
