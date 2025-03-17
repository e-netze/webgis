using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.IO;

public enum StorageBlobType
{
    Normal = 0,
    Data = 1,
    Metadata = 2,
    Acl = 3,
    Folder = 4
}

public interface IStorage
{
    bool Save(string name, byte[] data, StorageBlobType type = StorageBlobType.Normal);
    bool Save(string name, string dataString, StorageBlobType type = StorageBlobType.Normal);
    bool Exists(string name, StorageBlobType type = StorageBlobType.Normal);
    byte[] Load(string name, StorageBlobType type = StorageBlobType.Normal);
    string LoadString(string name, StorageBlobType type = StorageBlobType.Normal);

    Dictionary<string, string> LoadStrings(string[] names, StorageBlobType type = StorageBlobType.Normal);

    bool Remove(string name, StorageBlobType type = StorageBlobType.Normal, bool recursive = false);
    string[] GetNames(bool includeFiles = true, bool includeDirectories = true, StorageBlobType blobType = StorageBlobType.Normal);
    Dictionary<string, string[]> GetAllNames();

    string[] GetDirectoryNames(string subDir = "", string filter = "");

    string CreateEncryptedName(string user, string name);

    string AppendToName(string name, string appendix);

    string DecryptName(string name);

    bool SetUniqueIndexItem(string indexName, string key, string val);

    string GetUniqueIndexItem(string indexName, string key);

    string[] GetUniqueIndexKeys(string indexName);

    bool RemoveUniqueIndexItem(string indexName, string key);

    string[] GetUserAccess(string name, Type storageToolType = null);
    bool SetUserAccess(string name, string[] access, Type storageToolType = null);

    string SaveTempDataString(string dataString, int expireMinutes);

    string LoadTempDataString(string id);

    bool SetItemOrder(string[] order);
    string[] GetItemOrder();

    string OwnerName(string name);
}

public interface IStorage2
{
    string GetDecodedName(string encName);
}
