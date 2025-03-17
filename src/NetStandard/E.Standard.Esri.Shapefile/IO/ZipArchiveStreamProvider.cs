using System.IO;
using System.IO.Compression;

namespace E.Standard.Esri.Shapefile.IO;

public class ZipArchiveStreamProvider : MemoryStreamProvider
{
    private readonly Stream _stream;

    public ZipArchiveStreamProvider(Stream stream)
    {
        _stream = stream;

    }

    public override void FlushAndReleaseStreams()
    {
        using (var zipArchive = new ZipArchive(_stream, ZipArchiveMode.Create, true))
        {
            foreach (var name in _streams.Keys)
            {
                var data = _streams[name].ToArray();

                var entry = zipArchive.CreateEntry(name);
                using (var entryStream = entry.Open())
                {
                    entryStream.Write(data, 0, data.Length);
                }
            }
        }

        _streams.Clear();
    }
}
