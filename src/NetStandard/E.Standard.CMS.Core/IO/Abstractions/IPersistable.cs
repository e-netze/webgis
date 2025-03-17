namespace E.Standard.CMS.Core.IO.Abstractions;

public interface IPersistable
{
    void Load(IStreamDocument stream);
    void Save(IStreamDocument stream);
}
