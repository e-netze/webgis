namespace E.Standard.CMS.Core.Schema.Abstraction;

public interface IImportable
{
    string[] ImportMethods { get; }

    bool Import(string methode, string urlPath);
}
