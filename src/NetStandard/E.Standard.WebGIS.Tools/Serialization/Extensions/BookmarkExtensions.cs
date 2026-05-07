using System;

using E.Standard.Localization.Abstractions;
using E.Standard.WebMapping.Core.Api.Bridge;

namespace E.Standard.WebGIS.Tools.Serialization.Extensions;

static internal class BookmarkExtensions
{
    static public void ThrowIfAnonymous(this IBridgeUser user, ILocalizer<Bookmarks> localizer)
    {
        if (user.IsAnonymous)
        {
            throw new Exception(localizer.Localize("exception-anonymous-user"));
        }
    }
}
