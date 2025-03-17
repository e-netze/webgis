using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IMapServiceCollection
{
    IEnumerable<IMapServerCollectionItem> Items { get; }
}


public interface IMapServerCollectionItem : IClone<IMapServerCollectionItem, IMapServiceCollection>
{
    string Url { get; }
    MapServiceLayerVisibility LayerVisibility { get; }
}