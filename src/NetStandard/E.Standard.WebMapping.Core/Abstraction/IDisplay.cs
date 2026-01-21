using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IDisplay
{
    int ImageWidth { get; set; }
    int ImageHeight { get; set; }

    double Dpi { get; set; }

    Envelope Extent { get; }

    Point WorldToImage(Point worldPoint);
    Point ImageToWorld(Point imagePoint);

    double DisplayRotation { get; set; }

    ArgbColor BackgroundColor { get; set; }

    IDictionary<string, TimeEpochDefinition> TimeEpoch { get; set; }
}
