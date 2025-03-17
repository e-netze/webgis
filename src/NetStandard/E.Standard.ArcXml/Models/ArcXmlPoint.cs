using E.Standard.Platform;
using System.Xml;

namespace E.Standard.ArcXml.Models;

public class ArcXmlPoint
{
    private double _x, _y, _z = 0.0;
    public ArcXmlPoint(double x, double y)
    {
        _x = x;
        _y = y;
    }
    public ArcXmlPoint(double x, double y, double z)
        : this(x, y)
    {
        _z = z;
    }
    public double X
    {
        get { return _x; }
        set { _x = value; }
    }
    public double Y
    {
        get { return _y; }
        set { _y = value; }
    }
    public double Z
    {
        get { return _z; }
        set { _z = value; }
    }
    public void Write(XmlTextWriter xml, char commaFormat)
    {
        xml.WriteStartElement("POINT");
        xml.WriteAttributeString("x", _x.ToPlatformNumberString().Replace(".", commaFormat.ToString()));
        xml.WriteAttributeString("y", _y.ToPlatformNumberString().Replace(".", commaFormat.ToString()));
        xml.WriteEndElement();
    }
}
