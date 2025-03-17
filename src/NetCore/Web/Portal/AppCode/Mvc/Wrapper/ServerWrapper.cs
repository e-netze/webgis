using E.Standard.WebGIS.Core.Mvc.Wrapper;
using Microsoft.AspNetCore.Hosting;

namespace Portal.AppCode.Mvc.Wrapper;

public class ServerWrapper : IServerWrapper
{
    public ServerWrapper(IWebHostEnvironment env)
    {
        this.WebRootPath = env.WebRootPath;
        this.ContentRootPath = env.ContentRootPath;
    }


    private string WebRootPath { get; set; }
    private string ContentRootPath { get; set; }

    public string MapPath(string path)
    {
        if (path == "." || path == "~")
        {
            return this.ContentRootPath;
        }

        if (path.StartsWith("~"))
        {
            return this.ContentRootPath + path.Substring(1);
        }

        return this.ContentRootPath + path;
    }

    public string AppBinPath => System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

}
