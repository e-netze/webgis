using E.Standard.Web.Abstractions;
using E.Standard.Web.Extensions;
using gView.GraphicsEngine.Abstraction;
using System;
using System.IO;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Extensions;

static public class HttpServiceExtensions
{
    async static public Task<IBitmap> GetImageAsync(this IHttpService httpService,
                                                  string imagePath,
                                                  string imageUrl,
                                                  string outputPath = null)
    {
        string filename;

        try
        {
            if (!String.IsNullOrEmpty(imagePath))
            {
                filename = imagePath;
                if (!httpService.Legacy_AlwaysDownloadFrom(filename))
                {
                    FileInfo fi = new FileInfo(filename);
                    if (fi.Exists)
                    {
                        return await fi.FullName.ImageFromUri(httpService);
                    }
                }
            }
            if (!String.IsNullOrEmpty(outputPath))
            {
                filename = outputPath + @"\" + imageUrl.FileTitleFromUriString();
                FileInfo fi = new FileInfo(filename);
                if (fi.Exists)
                {
                    return await fi.FullName.ImageFromUri(httpService);
                }
            }
            if (!String.IsNullOrEmpty(imageUrl))
            {
                return await httpService.GetImageAsync(imageUrl);
            }
        }
        catch
        {
        }
        return null;
    }

    async static public Task<string> GetImagePathAsync(this IHttpService httpService,
                                                    string imagePath, string imageUrl,
                                                    string outputPath = null)
    {
        string filename;

        try
        {
            if (!String.IsNullOrEmpty(imagePath))
            {
                filename = imagePath;
                if (!httpService.Legacy_AlwaysDownloadFrom(filename))
                {
                    FileInfo fi = new FileInfo(filename);
                    if (fi.Exists)
                    {
                        return fi.FullName;
                    }
                }
            }
            if (outputPath != "")
            {
                filename = Path.Combine(outputPath, imageUrl.FileTitleFromUriString());
                FileInfo fi = new FileInfo(filename);
                if (fi.Exists)
                {
                    return fi.FullName;
                }
            }
            if (imageUrl != "")
            {
                //var stream = await DownloadImgBytesAsync(imageUrl);
                var stream = new MemoryStream(await httpService.GetDataAsync(imageUrl));
                filename = Path.Combine(outputPath, imageUrl.FileTitleFromUriString());

                await stream.SaveOrUpload(filename);
                return filename;
            }
        }
        catch /*(Exception ex)*/
        {

        }

        return String.Empty;
    }
}
