using E.Standard.CMS.Core.IO.Abstractions;
using System;
using System.ComponentModel;

namespace E.Standard.CMS.Core.Security;

public abstract class AuthorizableArrayItem
{
    private string _itemGuid;

    public AuthorizableArrayItem()
    {
        _itemGuid = "i" + GuidEncoder.Encode(Guid.NewGuid()).ToLower();
    }

    [Browsable(false)]
    public string ItemGuid
    {
        get { return _itemGuid; }
        set { _itemGuid = value; }
    }

    #region Persist
    private IStreamDocument _stream = null;
    private string _nodeName = string.Empty;
    internal void Save(IStreamDocument stream, string nodeName)
    {
        _stream = stream;
        _nodeName = nodeName;

        Save();

        _stream = null;
    }
    internal void Load(IStreamDocument stream, string nodeName)
    {
        _stream = stream;
        _nodeName = nodeName;

        Load();

        _stream = null;
    }

    protected void StreamSave(string path, object obj)
    {
        if (_stream != null && !string.IsNullOrEmpty(_nodeName))
        {
            _stream.Save(_nodeName + "_" + _itemGuid + "_" + path, obj);
        }
    }
    protected void StreamSaveOrRemoveIfEmpty(string path, string obj)
    {
        if (_stream != null && !string.IsNullOrEmpty(_nodeName))
        {
            _stream.SaveOrRemoveIfEmpty(_nodeName + "_" + _itemGuid + "_" + path, obj);
        }
    }
    protected object StreamLoad(string path, object defValue)
    {
        if (_stream != null && !string.IsNullOrEmpty(_nodeName))
        {
            return _stream.Load(_nodeName + "_" + _itemGuid + "_" + path, defValue);
        }
        return defValue;
    }

    #endregion

    abstract public void Load();

    abstract public void Save();

}
