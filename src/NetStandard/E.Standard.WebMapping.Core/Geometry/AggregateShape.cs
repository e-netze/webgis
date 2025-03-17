using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Geometry;

public sealed class AggregateShape : Shape
{
    private readonly List<Shape> _shapes;

    public AggregateShape()
    {
        _shapes = new List<Shape>();
    }

    public void AddShape(Shape shape)
    {
        if (_shapes != null)
        {
            _shapes.Add(shape);
        }
    }

    public int CountShapes { get { return _shapes.Count; } }
    public Shape this[int i]
    {
        get
        {
            if (i < 0 || i >= _shapes.Count)
            {
                return null;
            }

            return _shapes[i];
        }
    }
}
