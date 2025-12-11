using E.Standard.CMS.Core.Abstractions;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.CMS.Core.IO;

public class FileSystemPathInfo_no_FileInfo_Experiment : IPathInfo, IDatabasePath
{
    private string _path;

    public FileSystemPathInfo_no_FileInfo_Experiment(string path)
    {
        _path = SystemInfo.IsLinux 
            ? path.ToPlatformPath().RemoveDoubleSlashes() 
            : path;
    }

    #region IPathInfo

    public string Name
    {
        get
        {
            var lastSlash = Math.Max(
                _path.LastIndexOf("/"),
                _path.LastIndexOf("\\")
            );

            return _path.Substring(lastSlash + 1);
        }
    }

    public string FullName => _path;

    public bool Exists => Directory.Exists(_path);

    public IPathInfo Parent
    {
        get
        {
            var lastSlash = Math.Max(
                _path.LastIndexOf("/"),
                _path.LastIndexOf("\\")
            );

            if (lastSlash < 0) return null;

            return new FileSystemPathInfo(_path.Substring(0, lastSlash));
        }
    }

    public void Create()
    {
        var di = new DirectoryInfo(_path);  
        di.Create();
    }


    public IPathInfo CreateSubdirectory(string path)
    {
        var di = new DirectoryInfo(_path);
        return new FileSystemPathInfo(di.CreateSubdirectory(path).FullName);
    }

    public void Delete()
    {
        var di = new DirectoryInfo(_path);
        di.Delete();
    }

    public void Delete(bool recursive)
    {
        var di = new DirectoryInfo(_path);
        di.Delete(recursive);
    }


    public IEnumerable<IPathInfo> GetDirectories()
    {
        return Directory.GetDirectories(_path).Select(d => new FileSystemPathInfo(d)).ToArray();
    }

    public IEnumerable<IDocumentInfo> GetFiles(string filter)
    {
        return Directory.GetFiles(_path, filter).Select(f => new FileSystemDocumentInfo(f)).ToArray();
    }

    public IEnumerable<IDocumentInfo> GetFiles()
    {
        return Directory.GetFiles(_path).Select(f => new FileSystemDocumentInfo(f)).ToArray();
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
        return _di.GetDirectories().Select(d => new FileSystemPathInfo(d.FullName)).ToArray();
    }

    public IEnumerable<IDocumentInfo> GetFiles(string filter)
    {
        return _di.GetFiles(filter).Select(f => new FileSystemDocumentInfo(f.FullName)).ToArray();
    }

    public IEnumerable<IDocumentInfo> GetFiles()
    {
        return _di.GetFiles().Select(f => new FileSystemDocumentInfo(f.FullName)).ToArray();
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
