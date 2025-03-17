namespace E.Standard.GeoRSS.Abstraction;

public interface IItem
{
    string this[string attribute] { get; }
}
