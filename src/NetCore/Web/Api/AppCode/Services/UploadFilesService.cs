using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Api.Core.AppCode.Services;

public class UploadFilesService
{
    private FilesDictionary _files = null;

    public UploadFilesService()
    {

    }

    public IFiles GetFiles(HttpRequest request)
    {
        if (_files != null)
        {
            return _files;
        }

        _files = new FilesDictionary();
        try
        {
            foreach (var formFile in request.Form.Files)
            {
                byte[] data = new byte[formFile.Length];

                formFile.OpenReadStream().ReadExactly(data, 0, data.Length);
                _files.Add(formFile.Name,
                    new FormFile()
                    {
                        Data = data,
                        FileName = formFile.FileName,
                        ContentDisposition = formFile.ContentDisposition,
                        ContentType = formFile.ContentType
                    });
            }
        }
        catch { }
        return _files;
    }

    public bool HasFiles(HttpRequest request)
    {
        return GetFiles(request).Count > 0;
    }

    #region Classes

    public class FormFile
    {
        public int ContentLength
        {
            get
            {
                return this.Data != null ? this.Data.Length : 0;
            }
        }

        public string ContentDisposition { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public byte[] Data { get; set; }
    }

    public interface IFiles
    {
        FormFile this[string name] { get; }
        int Count { get; }
        IEnumerable<string> Keys { get; }
    }

    private class FilesDictionary : IFiles
    {
        private readonly Dictionary<string, FormFile> _dict = new Dictionary<string, FormFile>();

        internal void Add(string name, FormFile file)
        {
            _dict.Add(name, file);
        }

        #region IFiles

        public FormFile this[string name]
        {
            get
            {
                if (!_dict.ContainsKey(name))
                {
                    return null;
                }

                return _dict[name];
            }
        }

        public int Count => _dict.Count;

        public IEnumerable<string> Keys => _dict.Keys;

        #endregion
    }

    #endregion
}
