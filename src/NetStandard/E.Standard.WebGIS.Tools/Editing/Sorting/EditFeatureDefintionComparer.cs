using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;

namespace E.Standard.WebGIS.Tools.Editing.Sorting;

class EditFeatureDefintionComparer : IComparer<EditFeatureDefinition>
{
    private readonly Point _point;

    public EditFeatureDefintionComparer(Point point)
    {
        _point = point;
    }

    #region IComparer<EditFeatureDefinition>

    public int Compare(EditFeatureDefinition x, EditFeatureDefinition y)
    {
        try
        {
            if (x?.Feature?.Shape == null && y?.Feature?.Shape == null)
            {
                return x.EditThemeName.CompareTo(y.EditThemeName);
            }

            if (x?.Feature?.Shape == null)
            {
                return 1;
            }

            if (y?.Feature?.Shape == null)
            {
                return -1;
            }

            return SpatialAlgorithms.Point2ShapeDistance(x.Feature.Shape, _point).CompareTo(SpatialAlgorithms.Point2ShapeDistance(y.Feature.Shape, _point));
        }
        catch
        {
            if (!String.IsNullOrEmpty(x?.EditThemeId) && !String.IsNullOrEmpty(y?.EditThemeId))
            {
                return x.EditThemeName.CompareTo(y.EditThemeName);
            }

            return 0;
        }
    }

    #endregion
}
