using E.Standard.Web.Abstractions;
using E.Standard.Web.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.ServiceResponses;

public class ImageLocation : ServiceResponse
{
    public string ImagePath;
    public string ImageUrl;

    public ImageLocation(int index, string serviceID)
        : base(index, serviceID)
    {
        ImagePath = String.Empty;
        ImageUrl = String.Empty;
    }

    public ImageLocation(int index, string serviceID, string path, string url)
        : base(index, serviceID)
    {
        ImagePath = path;
        ImageUrl = url;
    }

    async public override Task<bool> IsEmpty(IHttpService httpService)
    {
        bool empty = false;
        try
        {
            if (!String.IsNullOrEmpty(this.ImagePath))
            {
                empty = this.ImagePath.IsEmptyImage();
            }
            else if (this.ImageUrl.EndsWith("/empty.gif"))
            {
                empty = true;
            }
            else if (!String.IsNullOrEmpty(this.ImageUrl))
            {
                if (this.ImageUrl.ToLower().StartsWith("http://") || this.ImageUrl.ToLower().StartsWith("https://"))
                {
                    using (var image = await httpService.GetImageAsync(this.ImageUrl))
                    {
                        if (image.Width <= 2 || image.Height <= 2)
                        {
                            empty = true;
                        }
                    }
                }
                else  // relative url...
                {
                    return false;
                }
            }
            else
            {
                empty = true;
            }
        }
        catch /*(Exception ex)*/
        {
            empty = true;
        }

        return empty;
    }

    public bool IsEmptyImage => this is EmptyImage || (this.ImageUrl != null && this.ImageUrl.EndsWith("/empty.gif"));

    private string TypGetAppConfigPath
    {
        get
        {
            throw new NotImplementedException();

            //try
            //{
            //    //string appConfigPath = new DirectoryInfo(System.Web.HttpContext.Current.Server.MapPath(".")).Parent.FullName + @"\config";
            //    //DirectoryInfo di = new DirectoryInfo(appConfigPath);
            //    //if (!di.Exists)
            //    //{
            //    //    appConfigPath = new DirectoryInfo(System.Web.HttpContext.Current.Server.MapPath(".")).Parent.Parent.FullName + @"\config";
            //    //}

            //    //return appConfigPath;
            //}
            //catch
            //{
            //    return String.Empty;
            //}
        }
    }

    public Envelope Extent { get; set; }
    public double Scale { get; set; }
}
