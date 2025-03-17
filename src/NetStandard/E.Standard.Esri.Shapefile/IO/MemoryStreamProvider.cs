using System;
using System.Collections.Generic;
using System.IO;

namespace E.Standard.Esri.Shapefile.IO;

abstract public class MemoryStreamProvider : IStreamProvider, IDisposable
{
    protected readonly Dictionary<string, MemoryStream> _streams = new Dictionary<string, MemoryStream>();

    public Stream CreateStream(string name)
    {
        if (StreamExists(name))
        {
            throw new Exception("Stream already exists");
        }

        var memoryStream = new MemoryStream();
        _streams[name] = memoryStream;

        return memoryStream;
    }

    public void Dispose()
    {
        FlushAndReleaseStreams();
    }

    abstract public void FlushAndReleaseStreams();

    public bool StreamExists(string name)
    {
        return _streams.ContainsKey(name);
    }

    public Stream this[string name] => StreamExists(name) ? _streams[name] : CreateStream(name);
}
