using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.Platform;
using System;
using System.IO;

namespace E.Standard.CMS.Core.IO;

public class FileSystemDocumentInfo_no_FileInfo_Experiment : IDocumentInfo
{
    private string _path;

    public FileSystemDocumentInfo_no_FileInfo_Experiment(string path)
    {
        _path = SystemInfo.IsLinux 
            ? path.ToPlatformPath().RemoveDoubleSlashes()
            : path;
    }

    #region IDocumentInfo

    public bool Exists => File.Exists(_path);

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

    public string Extension 
    {
        get
        {
            var name = this.Name;

            if (!name.Contains("."))
                return string.Empty;

            return name.Substring(name.LastIndexOf("."));
        }
    }

    public IPathInfo Directory
    {
        get
        {
            var lastSlash=Math.Max(
                _path.LastIndexOf("/"),
                _path.LastIndexOf("\\")
            );
            return new FileSystemPathInfo(_path.Substring(0, lastSlash));
        }
    }

    public string Title
    {
        get
        {
            var name = this.Name;
            if (!name.Contains("."))
                return name;

            return name.Substring(0, name.LastIndexOf("."));
        }
    }

    public void Delete()
    {
        File.Delete(_path);
    }

    public IDocumentInfo CopyTo(string path)
    {
        var fi = new FileInfo(path);
        return new FileSystemDocumentInfo(fi.CopyTo(path).FullName);
    }

    public IDocumentInfo CopyTo(string path, bool overwrite)
    {
        var fi = new FileInfo(path);    
        return new FileSystemDocumentInfo(fi.CopyTo(path, overwrite).FullName);
    }

    public void Write(string data)
    {
        File.WriteAllText(_path, data);
    }
    public string ReadAll()
    {
        return File.ReadAllText(_path);
    }

    #endregion
}


public class FileSystemDocumentInfo : IDocumentInfo
{
    private FileInfo _fi;

    public FileSystemDocumentInfo(string path)
    {
        _fi = new FileInfo(
            SystemInfo.IsLinux ?
            path.ToPlatformPath().RemoveDoubleSlashes() :
            path);
    }

    #region IDocumentInfo

    public bool Exists => _fi.Exists;

    public string Name => _fi.Name;

    public string FullName => _fi.FullName;

    public string Extension => _fi.Extension;

    public IPathInfo Directory => new FileSystemPathInfo(_fi.Directory.FullName);

    public string Title => _fi.Name.Substring(0, _fi.Name.Length - _fi.Extension.Length);

    public void Delete()
    {
        _fi.Delete();
    }

    public IDocumentInfo CopyTo(string path)
    {
        return new FileSystemDocumentInfo(_fi.CopyTo(path).FullName);
    }

    public IDocumentInfo CopyTo(string path, bool overwrite)
    {
        return new FileSystemDocumentInfo(_fi.CopyTo(path, overwrite).FullName);
    }

    public void Write(string data)
    {
        File.WriteAllText(_fi.FullName, data);
    }
    public string ReadAll()
    {
        return File.ReadAllText(_fi.FullName);
    }

    #endregion
}
