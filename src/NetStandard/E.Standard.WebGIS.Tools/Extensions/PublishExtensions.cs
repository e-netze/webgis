using System;
using System.IO;
using System.Text;

namespace E.Standard.WebGIS.Tools.Extensions;

static class PublishExtensions
{
    #region Description

    static public string RemoveSectionsPlaceholders(this string description)
    {
        var sb = new StringBuilder();

        using (var sr = new StringReader(description))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("@section:") || line.StartsWith("@endsection"))
                {
                    continue;
                }

                sb.Append(line);
                sb.Append(Environment.NewLine);
            }
        }

        return sb.ToString();
    }

    #endregion
}
