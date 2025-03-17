namespace webgis.deploy.Extensions;

internal static class StringExtensions
{
    static public bool IsColor(this string colString)
    {
        colString = colString?.Trim().ToLower();
        if (String.IsNullOrEmpty(colString))
        {
            return false;
        }

        return colString.StartsWith("#") || colString.StartsWith("rgb(") || colString.StartsWith("rgba");
    }

    static public (int red, int green, int blue) ToRgbTuple(this string colString)
    {
        colString = colString.Trim().ToLower();

        try
        {
            if (colString.StartsWith("#") || colString.StartsWith("0x"))
            {
                if (colString.StartsWith("#"))
                {
                    colString = colString.Substring(1);
                }
                else if (colString.StartsWith("0x"))
                {
                    colString = colString.Substring(2);
                }

                string r, g, b;
                if (colString.Length == 3) // #f00
                {
                    r = colString[0].ToString() + colString[0].ToString();
                    g = colString[1].ToString() + colString[1].ToString();
                    b = colString[2].ToString() + colString[2].ToString();
                }
                else // #ff0000
                {
                    r = colString[0].ToString() + colString[1].ToString();
                    g = colString[2].ToString() + colString[3].ToString();
                    b = colString[4].ToString() + colString[5].ToString();
                }

                return (int.Parse(r, System.Globalization.NumberStyles.HexNumber),
                        int.Parse(g, System.Globalization.NumberStyles.HexNumber),
                        int.Parse(b, System.Globalization.NumberStyles.HexNumber));
            }
            else if (colString.StartsWith("rgb("))
            {
                string[] rgb = colString
                    .Replace("rgb(", "")
                    .Replace(")", "")
                    .Trim()
                    .Split(',');

                if (rgb.Length == 3)
                {
                    return (int.Parse(rgb[0]),
                            int.Parse(rgb[1]),
                            int.Parse(rgb[2]));
                }
            }
            else if (colString.Split(',').Length == 3)
            {
                string[] rgb = colString.Split(',');
                return (int.Parse(rgb[0]),
                        int.Parse(rgb[1]),
                        int.Parse(rgb[2]));
            }
        }
        catch { }

        return (255, 0, 0);  // red
    }

    static public string ToCssRgbColor(this string colString)
    {
        var col = colString.ToRgbTuple();
        return $"rgb({col.red}, {col.green}, {col.blue})";
    }
}
