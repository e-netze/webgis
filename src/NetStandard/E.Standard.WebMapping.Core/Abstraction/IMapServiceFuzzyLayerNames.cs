namespace E.Standard.WebMapping.Core.Abstraction;

public interface IMapServiceFuzzyLayerNames : IMapService
{
    string FuzzyLayerNameSeperator { get; }
}
