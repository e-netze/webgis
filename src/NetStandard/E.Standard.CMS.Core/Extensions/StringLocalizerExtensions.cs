using E.Standard.Localization.Abstractions;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.CMS.Core.Extensions;

static public class StringLocalizerExtensions
{
    static public ILocalizer CreateCmsLocalizer(this IStringLocalizerFactory factory, Type type)
    {
        IStringLocalizer stringLocalizer = factory.Create(type);
        var cmsLocalizerType = (typeof(CmsLocalizer<>).MakeGenericType(type));

        return Activator.CreateInstance(cmsLocalizerType, stringLocalizer) as ILocalizer;
    }
}
