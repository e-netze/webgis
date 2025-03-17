using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.Json.Test;

internal class FakeData
{
    public FeatureCollection CreateFeatureCollectionWithPointLinePolygon()
    {
        return new FeatureCollection([
                new Feature(CreateFeatureAttributes())
                {
                    Oid=1,
                    Shape = new Point(12.23, 45.67)
                    {

                    }
                },
                new Feature(CreateFeatureAttributes())
                {
                    Oid=2,
                    Shape = CreatePolyLine()
                },
                new Feature(CreateFeatureAttributes())
                {
                    Oid=3,
                    Shape = CreatePolygon()
                },
                new Feature(CreateFeatureAttributes())
                {
                    Oid=4,
                    Shape = CreateMultiPolyLine()
                },
                new Feature(CreateFeatureAttributes())
                {
                    Oid=5,
                    Shape = CreateMultiPolygon()
                }]);
    }

    private Polyline CreatePolyLine()
        => new Polyline([
                new Point(12.23, 45.67),
                new Point(121.23, 451.67),
                new Point(122.23, 452.67)
            ]);

    private Polyline CreateMultiPolyLine()
        => new Polyline([
                new WebMapping.Core.Geometry.Path([
                    new Point(12.23, 45.67),
                    new Point(121.23, 451.67),
                    new Point(122.23, 452.67)
                ]),
                new WebMapping.Core.Geometry.Path([
                    new Point(112.23, 345.67),
                    new Point(1121.23, 3451.67),
                    new Point(1122.23, 3452.67)
                ])
            ]);

    private Polygon CreatePolygon()
        => new Polygon([
            new Ring([
                    new Point(12.23, 45.67),
                    new Point(121.23, 451.67),
                    new Point(122.23, 452.67)
                ])
            ]);

    private Polygon CreateMultiPolygon()
        => new Polygon([
            new Ring([
                    new Point(12.23, 45.67),
                    new Point(121.23, 451.67),
                    new Point(122.23, 452.67)
                ]),
            new Ring([
                    new Point(22.23, 35.67),
                    new Point(221.23, 351.67),
                    new Point(222.23, 352.67)
                ])
            ]);

    private IEnumerable<WebMapping.Core.Attribute> CreateFeatureAttributes()
        => [
            new WebMapping.Core.Attribute("ATTR_INT", "1", typeof(int)),
            new WebMapping.Core.Attribute("ATTR_FLOAT", "1.2", typeof(float)),
            new WebMapping.Core.Attribute("ATTR_DOUBLE", "1.99", typeof(int)),
            new WebMapping.Core.Attribute("ATTR_DATE", "1.1.2001", typeof(int)),
            new WebMapping.Core.Attribute("ATTR_STRING", "hello", typeof(int)),
            new WebMapping.Core.Attribute("ATTR_GUID", "3BF971C9-8A4F-4797-8E99-5AAC61A14F6E", typeof(int)),
        ];

    #region Static Members

    static public void SetFakeProperties(object obj)
    {
        foreach (var propertyInfo in obj.GetType().GetProperties())
        {
            if (propertyInfo.SetMethod == null)
            {
                continue;
            }

            object val = null!;

            if (propertyInfo.PropertyType == typeof(string))
            {
                val = "AsRandomString >=<!$%&/()?\"";
            }
            else if (propertyInfo.PropertyType == typeof(int))
            {
                val = Random.Shared.Next();
            }
            else if (propertyInfo.PropertyType == typeof(long))
            {
                val = Random.Shared.NextInt64();
            }
            else if (propertyInfo.PropertyType == typeof(double))
            {
                val = Random.Shared.NextDouble();
            }
            else if (propertyInfo.PropertyType == typeof(float))
            {
                val = Random.Shared.NextSingle();
            }

            if (val != null)
            {
                propertyInfo.SetValue(obj, val);
            }
        }
    }

    #endregion
}
