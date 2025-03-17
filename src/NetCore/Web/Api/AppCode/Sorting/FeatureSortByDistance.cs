using E.Standard.Platform;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;

namespace Api.Core.AppCode.Sorting;

public class FeatureSortByDistance : IComparer<Feature>
{
    private Point _referencePoint;
    private bool _calcSphericeDistance = false;
    private bool _writeDistanceAttribute = false;

    public FeatureSortByDistance(Point referencePoint, bool calcSphericeDistance, bool writeDistanceAttribute)
    {
        _referencePoint = referencePoint;
        _calcSphericeDistance = calcSphericeDistance;
        _writeDistanceAttribute = writeDistanceAttribute;
    }

    public int Compare(Feature x, Feature y)
    {
        if (x.Shape == null && y.Shape == null)
        {
            return 0;
        }

        if (x.Shape == null)
        {
            return 1;
        }

        if (y.Shape == null)
        {
            return -1;
        }

        double distX, distY;
        if (_calcSphericeDistance)
        {
            distX = SphericHelper.SphericDistance(_referencePoint, x.Shape.ShapeEnvelope.CenterPoint);
            distY = SphericHelper.SphericDistance(_referencePoint, y.Shape.ShapeEnvelope.CenterPoint);
        }
        else
        {
            distX = x.Shape.ShapeEnvelope.CenterPoint.Distance2D(_referencePoint);
            distY = y.Shape.ShapeEnvelope.CenterPoint.Distance2D(_referencePoint);
        }

        if (_writeDistanceAttribute)
        {

            if (x.Attributes["_distance"] == null)
            {
                x.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("_distance", Math.Round(distX, 2).ToPlatformNumberString()));
                x.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("_distanceString", Math.Round(distX, 2).ToPlatformDistanceString()));
            }

            if (y.Attributes["_distance"] == null)
            {
                y.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("_distance", Math.Round(distY, 2).ToPlatformNumberString()));
                y.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("_distanceString", Math.Round(distY, 2).ToPlatformDistanceString()));
            }
        }

        return distX.CompareTo(distY);
    }
}
