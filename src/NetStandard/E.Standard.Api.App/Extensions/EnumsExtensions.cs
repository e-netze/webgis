using E.Standard.WebGIS.CMS;

namespace E.Standard.Api.App.Extensions;

static internal class EnumsExtensions
{
    static public string ToXmlOperatorString(this MaskValidationOperators op)
    {
        switch (op)
        {
            case MaskValidationOperators.Ident:
                return "==";
            case MaskValidationOperators.Equals:
                return "=";
            default:
                return op.ToString();
        }
    }
}
