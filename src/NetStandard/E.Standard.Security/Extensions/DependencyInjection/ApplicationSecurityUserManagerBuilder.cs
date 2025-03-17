using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Security.Extensions.DependencyInjection
{
    [Obsolete("Use E.Standard.Security.App assembly")]
    public class ApplicationSecurityUserManagerBuilder
    {
        public ApplicationSecurityUserManagerBuilder(IServiceCollection services)
        {
            this.Services = services;
        }

        public readonly IServiceCollection Services;
    }
}
