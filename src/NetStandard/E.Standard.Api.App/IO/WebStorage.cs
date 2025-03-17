using E.Standard.Extensions.Collections;
using E.Standard.Json;
using E.Standard.WebMapping.Core.Api.IO;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace E.Standard.Api.App.IO;

public class WebStorage : IStorage
{
    public WebStorage(Bridge bridge, string storagePath)
    {
        this.Bridge = bridge;
        this._rootUrl = storagePath;
    }

    #region Properties

    private string _rootUrl = String.Empty;

    private Bridge Bridge { get; set; }

    #endregion

    #region IStorage

    public string CreateEncryptedName(string user, string name)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("user", user);
        parameters.Add("name", name);

        var response = GetStorageResponseObject<string>("CreateEncryptedName", parameters);

        return response;
    }

    public string AppendToName(string name, string appendix)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("name", name);
        parameters.Add("appendix", appendix);

        var response = GetStorageResponseObject<string>("AppendToName", parameters);

        return response;
    }

    public string DecryptName(string name)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("name", name);

        var response = GetStorageResponseObject<string>("DecryptName", parameters);

        return response;
    }

    public bool Exists(string name, StorageBlobType type = StorageBlobType.Normal)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("name", name);
        parameters.Add("type", type.ToString());

        var response = GetStorageResponseObject<bool>("Exists", parameters);

        return response;
    }

    public Dictionary<string, string[]> GetAllNames()
    {
        var response = GetStorageResponseObject<Dictionary<string, string[]>>("GetAllNames");

        return response;

    }

    public string[] GetDirectoryNames(string subDir = "", string filter = "")
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("subDir", subDir);
        parameters.Add("filter", filter);

        var response = GetStorageResponseObject<string[]>("GetDirectoryNames", parameters);

        return response;
    }

    public string[] GetNames(bool includeFiles = true, bool includeDirectories = true, StorageBlobType blobType = StorageBlobType.Normal)
    {
        var response = GetStorageResponseObject<string[]>("GetNames");

        return response;
    }

    public string OwnerName(string name)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("name", name);

        var response = GetStorageResponseObject<string>("Load", parameters);

        return response;
    }

    public byte[] Load(string name, StorageBlobType type = StorageBlobType.Normal)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("name", name);
        parameters.Add("type", type.ToString());

        var response = GetStorageResponseObject<byte[]>("Load", parameters);

        return response;
    }

    public string LoadString(string name, StorageBlobType type = StorageBlobType.Normal)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("name", name);
        parameters.Add("type", type.ToString());

        var response = GetStorageResponseObject<string>("LoadString", parameters);

        return response;
    }

    public Dictionary<string, string> LoadStrings(string[] names, StorageBlobType type = StorageBlobType.Normal)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("names", String.Join(",", names));
        parameters.Add("type", type.ToString());

        var response = GetStorageResponseObject<Dictionary<string, string>>("LoadStrings", parameters);

        return response;
    }

    public bool Save(string name, string dataString, StorageBlobType type = StorageBlobType.Normal)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("name", name);
        parameters.Add("dataString", dataString);
        parameters.Add("type", type.ToString());

        var response = GetStorageResponseObject<bool>("Save", parameters);

        return response;
    }

    public bool Save(string name, byte[] data, StorageBlobType type = StorageBlobType.Normal)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("name", name);
        parameters.Add("data", Convert.ToBase64String(data));
        parameters.Add("type", type.ToString());

        var response = GetStorageResponseObject<bool>("SaveData", parameters);

        return response;
    }

    public bool Remove(string name, StorageBlobType type = StorageBlobType.Normal, bool recursive = false)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("name", name);
        parameters.Add("recursive", recursive.ToString());
        parameters.Add("type", type.ToString());

        var response = GetStorageResponseObject<bool>("Remove", parameters);

        return response;
    }

    public bool SetUniqueIndexItem(string indexName, string key, string val)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("indexName", indexName);
        parameters.Add("key", key);
        parameters.Add("val", val);

        var response = GetStorageResponseObject<bool>("SetUniqueIndexItem", parameters);

        return response;
    }

    public string GetUniqueIndexItem(string indexName, string key)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("indexName", indexName);
        parameters.Add("key", key);

        var response = GetStorageResponseObject<string>("GetUniqueIndexItem", parameters);

        return response;
    }

    public string[] GetUniqueIndexKeys(string indexName)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("indexName", indexName);

        var response = GetStorageResponseObject<string[]>("GetUniqueIndexKeys", parameters);

        return response;
    }

    public bool RemoveUniqueIndexItem(string indexName, string key)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("indexName", indexName);
        parameters.Add("key", key);

        var response = GetStorageResponseObject<bool>("RemoveUniqueIndexItem", parameters);

        return response;
    }

    public string[] GetUserAccess(string name, Type storageToolType = null) { return null; }

    public bool SetUserAccess(string name, string[] access, Type storageToolType = null) { return false; }

    public string SaveTempDataString(string dataString, int expireMinutes)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("dataString", dataString);
        parameters.Add("expireMinutes", expireMinutes.ToString());

        var response = GetStorageResponseObject<string>("SaveTempDataString", parameters);

        return response;
    }

    public string LoadTempDataString(string id)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        parameters.Add("id", id);

        var response = GetStorageResponseObject<string>("LoadTempDataString", parameters);

        return response;
    }

    public bool SetItemOrder(string[] order)
    {
        return false;
    }

    public string[] GetItemOrder()
    {
        return null;
    }

    #endregion

    #region Helper

    async private Task<string> GetJson(string method, Dictionary<string, string> methodParameters = null)
    {
        NameValueCollection nvc = new NameValueCollection();

        nvc.Add("__un", EncryptParameterValue(Bridge.CurrentUser.Username));
        nvc.Add("__tid", Bridge.CurrentTool != null ? EncryptParameterValue(Bridge.CurrentTool.GetType().ToString().ToLower()) : "");
        nvc.Add("__res", Bridge.CurrentEventArguments != null ? EncryptParameterValue(Bridge.CurrentEventArguments.RawEventString) : "");
        if (methodParameters != null)
        {
            foreach (string key in methodParameters.Keys)
            {
                nvc.Add(key, EncryptParameterValue(methodParameters[key]));
            }
        }

        return await this.Bridge.HttpService.PostValues($"{_rootUrl}/storage/{method}", nvc.ToKeyValuePairs());
    }

    private string EncryptParameterValue(string val)
    {
        return Bridge.SecurityEncryptString(val);
    }

    private T GetStorageResponseObject<T>(string method, Dictionary<string, string> methodParameters = null)
    {
        string json = GetJson(method, methodParameters).Result;

        var storageResponse = JSerializer.Deserialize<StorageResponse<T>>(json);
        if (storageResponse.success == false)
        {
            throw new Exception(storageResponse.exception);
        }

        return storageResponse.response;
    }

    #endregion

    #region Result Classes

    private class StorageResponse<T>
    {
        public bool success { get; set; }
        public T response { get; set; }
        public string exception { get; set; }
    }

    #endregion
}
