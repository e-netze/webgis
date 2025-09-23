#nullable enable

using System.ComponentModel;

namespace Cms.AppCode.Extensions;

static internal class AttributeExtensions
{
    static public string? LocalizedDisplayName(this DisplayNameAttribute? attribute)
        => attribute?.DisplayName;
}
