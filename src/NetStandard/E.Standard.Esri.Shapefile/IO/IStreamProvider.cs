using System.IO;

namespace E.Standard.Esri.Shapefile.IO;

public interface IStreamProvider
{
    Stream CreateStream(string name);

    bool StreamExists(string name);

    Stream this[string name] { get; }

    void FlushAndReleaseStreams();
}
