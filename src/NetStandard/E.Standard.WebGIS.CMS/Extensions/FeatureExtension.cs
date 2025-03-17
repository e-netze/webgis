using E.Standard.WebMapping.Core.Collections;
using System.Text;

namespace E.Standard.WebGIS.CMS.Extensions;

static public class FeatureExtension
{
    public static string ToPattern(this FeatureCollection features, string pattern)
    {
        StringBuilder sb = new StringBuilder();

        foreach (var feature in features)
        {
            string text = Globals.SolveExpression(feature, pattern);
            sb.Append(text.Replace(@"\r\n", System.Environment.NewLine)
                          .Replace(@"\n", System.Environment.NewLine));
        }

        return sb.ToString();
    }
}
