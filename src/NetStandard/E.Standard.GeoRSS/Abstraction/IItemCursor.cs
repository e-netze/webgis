using System;

namespace E.Standard.GeoRSS.Abstraction;

public interface IItemCursor : IDisposable
{
    IItem NextItem { get; }
}
