using System.Collections.Generic;

namespace E.Standard.CMS.Core.IO.Abstractions;

public interface IPathInfo    // DirectoryInfo
{
    bool Exists { get; }
    string Name { get; }
    string FullName { get; }
    IPathInfo Parent { get; }

    void Delete();
    void Delete(bool recursive);
    void Create();
    IPathInfo CreateSubdirectory(string path);

    IEnumerable<IPathInfo> GetDirectories();
    IEnumerable<IDocumentInfo> GetFiles(string filter);
    IEnumerable<IDocumentInfo> GetFiles();
}
