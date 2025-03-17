using E.Standard.Web.Abstractions;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.Web.Extensions;

static public class IOExtensions
{
    static public void SetRequestUrl(this HttpRequestMessage requestMessage, string url)
    {
        var uri = new Uri(url);

        if (!String.IsNullOrEmpty(uri.UserInfo))
        {
            requestMessage.Headers.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(uri.UserInfo))}");
            uri = new Uri($"{uri.Scheme}://{uri.Authority}{uri.PathAndQuery}");
        }

        requestMessage.RequestUri = uri;
    }

    private static HttpClient? _httpClient = null;
    async static public Task SaveOrUpload(this MemoryStream stream, string fileUri)
    {
        if (fileUri.StartsWith("http://") || fileUri.StartsWith("https://"))
        {
            string url = fileUri.Substring(0, fileUri.LastIndexOf("/"));
            string filename = fileUri.Substring(fileUri.LastIndexOf("/") + 1);

            //Console.WriteLine($"upload {filename} to {url}");

            var file_bytes = stream.ToArray();

            // reuse HttpClient
            var client = _httpClient ?? (_httpClient = new HttpClient());

            try
            {
                using (var requestMessage = new HttpRequestMessage())
                {
                    MultipartFormDataContent form = new MultipartFormDataContent();
                    form.Add(new ByteArrayContent(file_bytes, 0, file_bytes.Length), "file", filename);

                    requestMessage.Method = HttpMethod.Post;
                    requestMessage.Content = form;
                    requestMessage.SetRequestUrl(url);

                    HttpResponseMessage response = await client.SendAsync(requestMessage);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"SaveOrUpload: Upload status code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"SaveOrUpload: {ex.Message}", ex);
            }
        }
        else
        {
            File.WriteAllBytes(fileUri, stream.ToArray());
        }
    }

    static public Task SaveOrUpload(this byte[] bytes, string fileUri)
    {
        using (MemoryStream ms = new MemoryStream(bytes))
        {
            return ms.SaveOrUpload(fileUri);
        }
    }

    static public Task SaveOrUpload(this IBitmap bitmap, string fileUri, ImageFormat imageFormat)
    {
        if (bitmap != null)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, imageFormat);
            return ms.SaveOrUpload(fileUri);
        }

        return Task.CompletedTask;
    }

    async static public Task<IBitmap> BitmapFromUri(this string imageUri, IHttpService http)
    {
        if (imageUri.StartsWith("http://") || imageUri.StartsWith("https://"))
        {
            var imageData = await http.GetDataAsync(imageUri);
            return Current.Engine.CreateBitmap(new MemoryStream(imageData));
        }
        else
        {
            return Current.Engine.CreateBitmap(imageUri);
        }
    }

    async static public Task<IBitmap> ImageFromUri(this string imageUri, IHttpService http)
    {
        return await BitmapFromUri(imageUri, http);
    }

    async static public Task<MemoryStream> BytesFromUri(this string uri, IHttpService httpService)
    {
        if (uri.StartsWith("http://") || uri.StartsWith("https://"))
        {
            return new MemoryStream(await httpService.GetDataAsync(uri));
        }
        else
        {
            return new MemoryStream(File.ReadAllBytes(uri));
        }
    }

    static public bool IsEmptyImage(this string imageUri)
    {
        if (imageUri.StartsWith("http://") || imageUri.StartsWith("https://"))
        {
            return false;
        }
        else
        {
            using (var image = Current.Engine.CreateBitmap(imageUri))
            {
                return (image.Width <= 2 || image.Height <= 2);
            }
        }
    }

    static public void TryDelete(this string uri)
    {
        try
        {
            if (uri.StartsWith("http://") || uri.StartsWith("https://"))
            {
                // Do nothing
            }
            else
            {
                File.Delete(uri);
            }
        }
        catch { }
    }
}
