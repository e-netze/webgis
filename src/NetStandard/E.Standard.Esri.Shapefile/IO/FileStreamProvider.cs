namespace E.Standard.Esri.Shapefile.IO;

public class FileStreamProvider : MemoryStreamProvider
{
    private readonly string _path;

    public FileStreamProvider(string path)
    {
        _path = path;
    }

    public override void FlushAndReleaseStreams()
    {
        foreach (var name in _streams.Keys)
        {
            _streams[name].Flush();
            string filename = $"{_path}/{name}";

            System.IO.File.WriteAllBytes(filename, _streams[name].ToArray());
        }

        _streams.Clear();
    }
}
