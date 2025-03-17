using E.Standard.Drawing.Extensions;
using ExifLibrary;
using System;
using System.IO;

namespace E.Standard.Drawing.Pro;

public class ImageMetadata
{
    public double? Longitute { get; private set; }
    public double? Latitude { get; private set; }

    public DateTime? DateTimeOriginal { get; private set; }

    public ExifLibrary.Orientation Orientation { get; private set; }

    public void ReadExif(Stream stream)
    {
        var file = ImageFile.FromStream(stream);

        var latTag = file.Properties.Get<GPSLatitudeLongitude>(ExifTag.GPSLatitude);
        var lngTag = file.Properties.Get<GPSLatitudeLongitude>(ExifTag.GPSLongitude);

        var latRefTag = file.Properties.Get<ExifEnumProperty<GPSLatitudeRef>>(ExifTag.GPSLatitudeRef);
        var lngRefTag = file.Properties.Get<ExifEnumProperty<GPSLongitudeRef>>(ExifTag.GPSLongitudeRef);

        var time = file.Properties.Get<ExifDateTime>(ExifTag.DateTimeOriginal);

        this.Orientation = file.Properties.Get<ExifEnumProperty<Orientation>>(ExifTag.Orientation)
            ?? Orientation.Normal;

        if (latTag != null && lngTag != null)
        {
            Latitude = latTag.Value.ToGeoCoord();
            Longitute = lngTag.Value.ToGeoCoord();
            //Latitude = latTag.ToString().FromGMS();
            //Longitute = lngTag.ToString().FromGMS();

            if (latRefTag == GPSLatitudeRef.South)
            {
                Latitude = -Latitude;
            }

            if (lngRefTag == GPSLongitudeRef.West)
            {
                Longitute = -Longitute;
            }
        }

        if (time != null)
        {
            DateTimeOriginal = time.Value;
        }

    }

    #region Helper



    #endregion
}
