using System;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Services;

internal class TestServiceProviderLivetimeService : IDisposable
{
    public void Dispose()
    {
        Console.WriteLine("TestServiceProviderLivetimeService: Disposed");
    }
}
