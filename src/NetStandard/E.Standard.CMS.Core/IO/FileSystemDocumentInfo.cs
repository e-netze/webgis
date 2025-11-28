using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.Platform;
using System.IO;

namespace E.Standard.CMS.Core.IO;

public class FileSystemDocumentInfo : IDocumentInfo
{
    private FileInfo _fi;

    public FileSystemDocumentInfo(string path)
    {
        _fi = new FileInfo(
            SystemInfo.IsLinux 
            ? path.ToPlatformPath().RemoveDoubleSlashes() 
            : path);
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
