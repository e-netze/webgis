using E.Standard.CMS.Core.Abstractions;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.Platform;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.CMS.Core.IO;

public class FileSystemPathInfo : IPathInfo, IDatabasePath
{
    private DirectoryInfo _di;
    public FileSystemPathInfo(string path)
    {
        _di = new DirectoryInfo(
            SystemInfo.IsLinux ?
            path.ToPlatformPath().RemoveDoubleSlashes() :
            path);
    }

    #region IPathInfo

    public string Name => _di.Name;

    public string FullName => _di.FullName;

    public bool Exists => _di.Exists;

    public IPathInfo Parent => _di.Parent != null ? new FileSystemPathInfo(_di.Parent.FullName) : null;

    public void Create()
    {
        _di.Create();
    }


    public IPathInfo CreateSubdirectory(string path)
    {
        return new FileSystemPathInfo(_di.CreateSubdirectory(path).FullName);
    }

    public void Delete()
    {
        _di.Delete();
    }

    public void Delete(bool recursive)
    {
        _di.Delete(recursive);
    }

    
    public IEnumerable<IPathInfo> GetDirectories()
    {
        return _di.GetDirectories().Select(d => new FileSystemPathInfo(d.FullName));
    }

    public IEnumerable<IDocumentInfo> GetFiles(string filter)
    {
        return _di.GetFiles(filter).Select(f => new FileSystemDocumentInfo(f.FullName));
    }

    public IEnumerable<IDocumentInfo> GetFiles()
    {
        return _di.GetFiles().Select(f => new FileSystemDocumentInfo(f.FullName));
    }

    #endregion

    #region IDatabasePath

    public void CreateDatabase()
    {
        this.Create();
    }

    public Task<bool> DeleteDatabase(IConsoleOutputStream outstream)
    {
        this.Delete(true);

        return Task.FromResult(true);
    }

    #endregion
}
