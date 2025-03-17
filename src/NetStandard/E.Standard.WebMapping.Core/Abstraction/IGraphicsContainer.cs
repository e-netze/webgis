using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IGraphicsContainer : IEnumerable<IGraphicElement>
{
    void Add(IGraphicElement element);
    bool Remove(IGraphicElement element);
    void Clear();
    int Count { get; }

    void Remove(Type type);
    bool Contains(Type type);

    IEnumerable<IGraphicElement> GetElements(Type type);
}
