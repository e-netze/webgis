using E.Standard.Platform;
using System;
using System.Collections.Generic;
using System.IO;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image.Models;

public class ImageWorldfile
{
    public ImageWorldfile(string content)
    {
        try
        {
            List<string> lines = new List<string>();
            using (var reader = new StringReader(content))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!String.IsNullOrWhiteSpace(line))
                    {
                        lines.Add(line);
                    }
                }
            }

            if (lines.Count < 6)
            {
                throw new Exception("Invalid world file");
            }

            this.Vector1X = lines[0].ToPlatformDouble();
            this.Vector1Y = lines[1].ToPlatformDouble();

            this.Vector2X = lines[2].ToPlatformDouble();
            this.Vector2Y = lines[3].ToPlatformDouble();

            this.OriginX = lines[4].ToPlatformDouble();
            this.OriginY = lines[5].ToPlatformDouble();

            this.IsValid = true;
        }
        catch
        {
            this.IsValid = false;
        }
    }

    public double OriginX { get; }
    public double OriginY { get; }

    public double Vector1X { get; }
    public double Vector1Y { get; }

    public double Vector2X { get; }
    public double Vector2Y { get; }

    public bool IsValid { get; }
}
