using System.Collections.Specialized;

namespace E.Standard.CMS.Core.IO.Abstractions;

public interface IStreamDocument
{
    IDocumentInfo ConfigFile { get; }

    void Init(string path = "", NameValueCollection stringReplace = null);

    bool Save(string path, object obj);
    bool Save(string path, object obj, object unauthorizedDefalut);
    bool SaveOrRemoveIfEmpty(string path, object obj);
    object Load(string path, object defValue);
    bool Remove(string path);

    void SaveDocument();
    void SaveDocument(string path);

    bool SetParent(string path);

    NameValueCollection StringReplace { get; }

    event ParseEncryptedValue OnParseBeforeEncryptValue;
    string FireParseBoforeEncryptValue(string value);
}
