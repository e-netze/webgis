using System;

namespace E.Standard.WebGIS.CmsSchema;

class Crypto
{
    static public string GetID() => $"i{Standard.CMS.Core.GuidEncoder.Encode(Guid.NewGuid())}";
}
