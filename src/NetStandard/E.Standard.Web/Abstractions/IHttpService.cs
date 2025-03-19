using E.Standard.Web.Models;
using gView.GraphicsEngine.Abstraction;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.Web.Abstractions;

public interface IHttpService
{
    Task<byte[]> GetDataAsync(string url, RequestAuthorization? authentication = null, int timeOutSeconds = 20);
    Task<IBitmap> GetImageAsync(string url, RequestAuthorization? authentication = null, int timeOutSeconds = 20);
    Task<string> GetStringAsync(string url, RequestAuthorization? authorization = null, int timeOutSeconds = 20, Encoding? encoding = null);

    Task<IEnumerable<T>> GetDataAsync<T>(IEnumerable<T> data,
                                         RequestAuthorization? authentication = null,
                                         int timeOutSeconds = 20)
        where T : IUrlData;

    Task<string> PostFormUrlEncodedStringAsync(string url,
                                               string postData,
                                               RequestAuthorization? authorization = null,
                                               int timeOutSeconds = 20);

    Task<(byte[] data, string contentType)> PostFormUrlEncodedAsync(string url,
                                         byte[] postData,
                                         RequestAuthorization? authorization = null,
                                         int timeOutSeconds = 20);

    Task<(byte[] data, string contentType)> PostDataAsync(string url,
                               byte[] postData,
                               RequestAuthorization? authorization = null,
                               int timeOutSeconds = 20);

    Task<string> PostJsonAsync(string url,
                               string json,
                               RequestAuthorization? authorization = null,
                               int timeOutSeconds = 20);

    Task<string> PostXmlAsync(string url,
                              string xml,
                              RequestAuthorization? authorization = null,
                              int timeOutSeconds = 20,
                              Encoding? encoding = null);

    Task<string> PostValues(string url,
                            IEnumerable<KeyValuePair<string, string>> values,
                            RequestAuthorization? authorization = null,
                            int timeOutSeconds = 20,
                            Encoding? encoding = null);

    Task<bool> UploadFileAsync(string url,
                                         byte[] fileData,
                                         string fileName,
                                         string formFieldName = "file",
                                         RequestAuthorization? authorization = null,
                                         int timeOutSeconds = 20);

    WebProxy? GetProxy(string server);

    string AppendParametersToUrl(string url, string parameters);

    string ApplyUrlOutputRedirection(string url);

    bool Legacy_AlwaysDownloadFrom(string filename);

    HttpClient Create(string url);
}
