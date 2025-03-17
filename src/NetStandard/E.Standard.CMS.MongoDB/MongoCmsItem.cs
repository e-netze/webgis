using E.Standard.CMS.Core.IO;
using E.Standard.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace E.Standard.CMS.MongoDB;

class MongoCmsItem
{
    public MongoCmsItem()
    {

    }

    public MongoCmsItem(string path,
                        Dictionary<string,
                        object> data = null)
    {
        this.Path = path;
        this.Parent = this.Path.PathToParent();
        this.CmsId = this.Path.PathToCmsId();

        if (data != null)
        {
            this.DataDictionary = data;
        }

        this.ItemType = CmsItemType.File;
    }

    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("CmsId")]
    public string CmsId { get; set; }

    [BsonElement("Path")]
    public string Path { get; set; }

    [BsonElement("Parent")]
    public string Parent { get; set; }

    [BsonElement("Type")]
    public int ItemTypeValue
    {
        get
        {
            return (int)ItemType;
        }
        set
        {
            ItemType = (CmsItemType)value;
        }
    }

    [BsonIgnore]
    public CmsItemType ItemType
    {
        get; set;
    }

    [BsonElement("Data")]
    public byte[] Data { get; set; }

    [BsonIgnore]
    public Dictionary<string, object> DataDictionary
    {
        get
        {
            if (this.Data == null || this.Data.Length == 0)
            {
                return new Dictionary<string, object>();
            }

            return JSerializer.Deserialize<Dictionary<string, object>>(DocumentFactory.DecryptString(this.Data.ToUTF8String()));
        }
        set
        {
            if (value == null)
            {
                this.Data = null;
            }
            else
            {
                this.Data = DocumentFactory.EncryptString(JSerializer.Serialize(value)).ToUTF8Bytes();
            }
        }
    }

    [BsonIgnore]
    public string DataString
    {
        get
        {
            if (this.Data == null || this.Data.Length == 0)
            {
                return String.Empty;
            }

            return DocumentFactory.DecryptString(this.Data.ToUTF8String());
        }
        set
        {
            if (String.IsNullOrEmpty(value))
            {
                this.Data = null;
            }
            else
            {
                this.Data = DocumentFactory.EncryptString(value).ToUTF8Bytes();
            }
        }
    }
}
