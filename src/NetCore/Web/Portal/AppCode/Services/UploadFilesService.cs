using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Portal.Core.AppCode.Services;

public class UploadFilesService
{
    private Dictionary<string, FormFile> _files = null;

    public UploadFilesService()
    {

    }

    public Dictionary<string, FormFile> GetFiles(HttpRequest request)
    {
        if (_files != null)
        {
            return _files;
        }

        _files = new Dictionary<string, FormFile>();
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

    #endregion
}
