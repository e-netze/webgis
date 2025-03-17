using E.Standard.Caching.Abstraction;
using E.Standard.Extensions.Compare;
using Microsoft.Extensions.Options;
using System.IO;
using System.Threading.Tasks;

namespace E.Standard.Caching.FileSystem;

public class FileSystemTempDataByteCache : ITempDataByteCache
{
    private readonly string _rootPath;

    public FileSystemTempDataByteCache(IOptionsMonitor<FileSystemTempDataByteCacheOptions> optionsMonitor)
    {
        _rootPath = Path.Combine(
            optionsMonitor?.CurrentValue?.RootPath.OrTake(Path.GetTempPath()),
            optionsMonitor?.CurrentValue?.SubFolder ?? "_tempdatacache");

        //Console.WriteLine($"FileSystemTempDataCache: { _rootPath }");
    }

    #region IKeyValueCache

    public byte[] Get(string key)
    {
        var fi = new FileInfo(Path.Combine(_rootPath, FilenameFromKey(key)));
        if (fi.Exists)
        {
            return File.ReadAllBytes(fi.FullName);
        }

        return null;
    }

    public void Remove(string key)
    {
        var fi = new FileInfo(Path.Combine(_rootPath, FilenameFromKey(key)));
        if (fi.Exists)
        {
            fi.Delete();
        }
    }

    public void Set(string key, byte[] data)
    {
        try
        {
            var fi = new FileInfo(Path.Combine(_rootPath, FilenameFromKey(key)));
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            if (fi.Exists)
            {

                fi.Delete();
            }

            File.WriteAllBytes(fi.FullName, data);
        }
        catch { }
    }

    #endregion

    #region ICacheClearable

    public Task<bool> Clear()
    {
        try
        {
            foreach (var fi in new DirectoryInfo(_rootPath).GetFiles())
            {
                try
                {
                    fi.Delete();
                }
                catch { }
            }
        }
        catch { }

        return Task.FromResult(true);
    }

    #endregion

    #region Helper

    private string FilenameFromKey(string key)
    {
        string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

        foreach (char c in invalid)
        {
            key = key.Replace(c.ToString(), "");
        }

        return key;
    }

    #endregion
}
