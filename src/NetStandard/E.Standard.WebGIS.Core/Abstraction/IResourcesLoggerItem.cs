using System;

namespace E.Standard.WebGIS.Core.Abstraction;

public interface IResourcesLoggerItem
{
    int Milliseconds { get; set; }
    int ContentSize { get; set; }
    string TypeName { get; set; }
    int Count { get; set; }

    string Custom1 { get; set; }
    string Custom2 { get; set; }
    string Custom3 { get; set; }
    string Custom4 { get; set; }
    string Custom5 { get; set; }

    string UserName { get; set; }

    DateTime Created { get; set; }

    bool CanAggregate(IResourcesLoggerItem item);
    void Aggregate(IResourcesLoggerItem item);
}
