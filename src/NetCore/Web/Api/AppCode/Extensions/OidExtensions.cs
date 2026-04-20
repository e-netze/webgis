namespace Api.Core.AppCode.Extensions;

static public class OidExtensions
{
    static public string ToSearchServiceFeatureOid(this int oid) => $"#service:#default:{oid}";

    static public bool IsSearchServiceFeatureOid(this string oidString)
        => oidString.StartsWith("#service:#default:");
}
