using E.Standard.WebGIS.Core.Mvc.Wrapper;
using Microsoft.AspNetCore.Hosting;

namespace Portal.AppCode.Mvc.Wrapper;

public class CurrentHttpContextWrapper : ICurrentHttpContextWrapper
{
    public CurrentHttpContextWrapper(IWebHostEnvironment env)
    {
        this.Server = new ServerWrapper(env);
    }

    public IServerWrapper Server { get; private set; }
}
