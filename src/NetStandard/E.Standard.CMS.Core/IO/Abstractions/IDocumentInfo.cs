namespace E.Standard.CMS.Core.IO.Abstractions;

public interface IDocumentInfo  // FileInfo
{
    bool Exists { get; }
    string Name { get; }
    string FullName { get; }
    string Extension { get; }

    string Title { get; }

    IPathInfo Directory { get; }

    void Delete();
    IDocumentInfo CopyTo(string path);
    IDocumentInfo CopyTo(string path, bool overwrite);

    void Write(string data);
    string ReadAll();
}
