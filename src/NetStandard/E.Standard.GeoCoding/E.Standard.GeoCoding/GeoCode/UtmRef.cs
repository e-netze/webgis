using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace E.Standard.GeoCoding.GeoCode;

public class UtmRef : IGeoCode
{
    private const double SemiMajorAxis = 6378137.0;                  
    private const double Flattening = 1.0 / 298.257223563;           

    private const double UtmScaleFactor = 0.9996;                    

    private static readonly double e2 = Flattening * (2 - Flattening);            //EccentricitySquared                             
    private static readonly double ep2 = e2 / (1 - e2);                           //SecondEccentricitySquared

    private const string eastingLetters = "ABCDEFGHJKLMNPQRSTUVWXYZ";

    private const string northingLettersOdd = "ABCDEFGHJKLMNPQRSTUV";
    private const string northingLettersEven = "FGHJKLMNPQRSTUVABCDE";

    public string Encode(double lon, double lat, int precesion)
    {
        if (double.IsNaN(lat) || double.IsNaN(lon) || double.IsInfinity(lat) || double.IsInfinity(lon))
        {
            return "Latitude or longitude cannot be NaN or Infinity";
        }

        if (lat < -80 || lat > 84)
        {
            Console.WriteLine("lat value (" + lat + ") not in [-80,84]");
            return "UTM/MGRS not defined beyond -80 to 84 degrees latitude.";
        }

        if (lon < -180 || lon > 180)
        {
            Console.WriteLine("lat value (" + lat + ") not in [-80,84]");
            return $"Longitude value not in [-180,180]";
        }

        if (precesion < 1 || precesion > 5)
        {
            return $"Precision value must be between 1 and 5";
        }

        try
        {
            //zone
            int utmZone = (int)Math.Floor((lon + 180) / 6) + 1;

            //band
            char latBand = GetLatBand(lat);

            // voodoo idk
            double φ = lat * Math.PI / 180; 
            double λ = lon * Math.PI / 180; 
            double λ0 = (utmZone * 6 - 183) * Math.PI / 180; 

            double sinφ = Math.Sin(φ);
            double cosφ = Math.Cos(φ);
            double tanφ = Math.Tan(φ);

            double N = SemiMajorAxis / Math.Sqrt(1 - e2 * sinφ * sinφ); 
            double T = tanφ * tanφ; 
            double C = ep2 * cosφ * cosφ; 
            double A = (λ - λ0) * cosφ;

            double M = SemiMajorAxis * (
                (1 - e2 / 4 - 3 * e2 * e2 / 64 - 5 * e2 * e2 * e2 / 256) * φ
              - (3 * e2 / 8 + 3 * e2 * e2 / 32 + 45 * e2 * e2 * e2 / 1024) * Math.Sin(2 * φ)
              + (15 * e2 * e2 / 256 + 45 * e2 * e2 * e2 / 1024) * Math.Sin(4 * φ)
              - (35 * e2 * e2 * e2 / 3072) * Math.Sin(6 * φ)
            );

            double utmEasting = UtmScaleFactor * N * (A +
                (1 - T + C) * Math.Pow(A, 3) / 6 +
                (5 - 18 * T + T * T + 72 * C - 58 * ep2) * Math.Pow(A, 5) / 120)
                 + 500000.0;

            double utmNorthing = UtmScaleFactor * (M + N * tanφ *
                (A * A / 2 +
                 (5 - T + 9 * C + 4 * C * C) * Math.Pow(A, 4) / 24 +
                 (61 - 58 * T + T * T + 600 * C - 330 * ep2) * Math.Pow(A, 6) / 720));

            if (lat < 0)
                utmNorthing += 10000000.0;

            int eIndex = (int)(utmEasting / 100000) - 1;
            int nIndex = ((int)(utmNorthing / 100000)) % 20;

            int eBase = ((utmZone - 1) % 3) * 8;
            char eLetter = eastingLetters[(eBase + eIndex) % eastingLetters.Length];

            string nTable = (utmZone % 2 == 0) ? northingLettersEven : northingLettersOdd;
            char nLetter = nTable[nIndex];

            //100x100km kastal
            string gridSquareLetters = $"{eLetter}{nLetter}";

            int div = (int)Math.Pow(10, 5 - precesion);

            //entfernung östlich vom zentralen meridian der zone
            int easting = (int)(utmEasting % 100000) / div;
            //entfernung nördlich vom äquator
            int northing = (int)(utmNorthing % 100000) / div;

            return $"{utmZone}{latBand}{gridSquareLetters}{easting.ToString($"D{precesion}")}{northing.ToString($"D{precesion}")}";
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            return "Error computing MGRS: " + e.Message;
        }       
    }

    public string Encode(GeoLocation geoLocation, int precision)
    {
        double lon = geoLocation.Longitude;
        double lat = geoLocation.Latitude;

        string result = Encode(lon, lat, precision);

        return result;
    }

    public GeoLocation Decode(string geoCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(geoCode))
            {
                throw new ArgumentException("MGRS code cannot be null or empty");
            }

            geoCode = geoCode.ToUpper().Replace(" ", "");

            int idx = 0;

            // zone
            int zoneStart = idx;
            while (idx < geoCode.Length && char.IsDigit(geoCode[idx]))
                idx++;

            if (idx == zoneStart)
                throw new ArgumentException("Invalid MGRS code: missing zone number");

            int utmZone = int.Parse(geoCode.Substring(zoneStart, idx - zoneStart));

            if (utmZone < 1 || utmZone > 60)
                throw new ArgumentException($"Invalid UTM zone: {utmZone}");

            if (idx >= geoCode.Length)
                throw new ArgumentException("Invalid MGRS code: missing latitude band");

            char latBand = geoCode[idx++];

            //100x100km kastl
            if (idx + 1 >= geoCode.Length)
                throw new ArgumentException("Invalid MGRS code: missing grid square letters");

            char eLetter = geoCode[idx++];
            char nLetter = geoCode[idx++];

            // easting northing
            string remaining = geoCode.Substring(idx);

            if (remaining.Length % 2 != 0)
                throw new ArgumentException("Invalid MGRS code: easting and northing must have equal precision");

            int precision = remaining.Length / 2;

            if (precision < 1 || precision > 5)
                throw new ArgumentException($"Invalid precision: {precision}");

            string eastingStr = remaining.Substring(0, precision);
            string northingStr = remaining.Substring(precision, precision);


            int eBase = ((utmZone - 1) % 3) * 8;
            int eIndex = eastingLetters.IndexOf(eLetter);

            if (eIndex == -1)
                throw new ArgumentException($"Invalid easting letter: {eLetter}");

            int eCol = (eIndex - eBase);
            if (eCol < 0)
                eCol += eastingLetters.Length;

            string nTable = (utmZone % 2 == 0) ? northingLettersEven : northingLettersOdd;
            int nIndex = nTable.IndexOf(nLetter);

            if (nIndex == -1)
                throw new ArgumentException($"Invalid northing letter: {nLetter}");

            // lat band
            double approxLat = GetBandCenterLatitude(latBand);
            bool isNorthernHemisphere = approxLat >= 0;

            int nSquare = nIndex;

            int div = (int)Math.Pow(10, 5 - precision);
            double easting = ((eCol + 1) * 100000.0) + (int.Parse(eastingStr) * div);
            double northing = (nSquare * 100000.0) + (int.Parse(northingStr) * div);

            easting += div / 2.0;
            northing += div / 2.0;

            if (isNorthernHemisphere)
            {
                double targetLat = approxLat;
                double testNorthing = northing;

                double bestNorthing = testNorthing;
                double bestLatDiff = double.MaxValue;

                for (int cycle = -1; cycle <= 10; cycle++)
                {
                    double tryNorthing = testNorthing + (cycle * 2000000.0);
                    var (tryLon, tryLat) = UtmToLatLon(easting, tryNorthing, utmZone, true);
                    double latDiff = Math.Abs(tryLat - targetLat);

                    if (latDiff < bestLatDiff)
                    {
                        bestLatDiff = latDiff;
                        bestNorthing = tryNorthing;
                    }
                }

                northing = bestNorthing;
            }
            else
            {
                double targetLat = approxLat;
                double testNorthing = northing; 

                double bestNorthing = testNorthing;
                double bestLatDiff = double.MaxValue;

                for (int cycle = 0; cycle <= 5; cycle++)
                {
                    double tryNorthing = testNorthing + (cycle * 2000000.0);
                    var (tryLon, tryLat) = UtmToLatLon(easting, tryNorthing, utmZone, false);
                    double latDiff = Math.Abs(tryLat - targetLat);

                    if (latDiff < bestLatDiff)
                    {
                        bestLatDiff = latDiff;
                        bestNorthing = tryNorthing;
                    }
                }

                northing = bestNorthing;
            }

            // utm zu lat und lon
            var (lon,lat) =  UtmToLatLon(easting, northing, utmZone, isNorthernHemisphere);
            GeoLocation loc = new GeoLocation { Latitude = lat, Longitude = lon };
            return loc;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new GeoLocation { ErrorMessage = e.Message };
        }
    }

    public string Description(string language = "en")
    {
        if (language == "de")
        {
            return "Das UTM-Referenzsystem (UTMREF), ist ein rechtwinkliges Planquadrat-orientiertes geografisches Meldesystem und basiert auf dem UTM-Koordinatensystem sowie für die Polarregionen dem UPS-Koordinatensystem.";
        }
        else
        {
            return "The Universal Transverse Mercator (UTM) is a projected coordinate system based on the transverse Mercator map projection of the Earth spheroid.";
        }
    }

    public bool IsValidGeoCode(string geoCode)
    {
        if (string.IsNullOrWhiteSpace(geoCode))
            return false;

        geoCode = geoCode.ToUpper().Replace(" ", "");

        int idx = 0;

        // zone number (1-60)
        int zoneStart = idx;
        while (idx < geoCode.Length && char.IsDigit(geoCode[idx]))
            idx++;

        if (idx == zoneStart || idx > zoneStart + 2)
            return false;

        if (!int.TryParse(geoCode.Substring(zoneStart, idx - zoneStart), out int utmZone))
            return false;

        if (utmZone < 1 || utmZone > 60)
            return false;

        // latitude band
        if (idx >= geoCode.Length)
            return false;

        char latBand = geoCode[idx++];
        if (!"CDEFGHJKLMNPQRSTUVWX".Contains(latBand))
            return false;

        // grid square easting letter
        if (idx >= geoCode.Length)
            return false;

        char eLetter = geoCode[idx++];
        if (!eastingLetters.Contains(eLetter))
            return false;

        // grid square northing letter
        if (idx >= geoCode.Length)
            return false;

        char nLetter = geoCode[idx++];
        if (!northingLettersOdd.Contains(nLetter)) // same set for both odd/even
            return false;

        // numeric part — must be even length, 2–10 digits
        string remaining = geoCode.Substring(idx);

        if (remaining.Length == 0 || remaining.Length % 2 != 0 || remaining.Length > 10)
            return false;

        if (!remaining.All(char.IsDigit))
            return false;

        return true;
    }

    public string DisplayName => "MRGS/UTMRefCode";

    public string[] Links => new string[] { "https://en.wikipedia.org/wiki/Military_Grid_Reference_System", "https://de.wikipedia.org/wiki/UTM-Referenzsystem" };

    public string[] Examples => new string[] { "32UMD7403" };

    #region Helpers

    private static char GetLatBand(double lat)
    {
        if (lat < -72) return 'C';
        if (lat < -64) return 'D';
        if (lat < -56) return 'E';
        if (lat < -48) return 'F';
        if (lat < -40) return 'G';
        if (lat < -32) return 'H';
        if (lat < -24) return 'J';
        if (lat < -16) return 'K';
        if (lat < -8) return 'L';
        if (lat < 0) return 'M';
        if (lat < 8) return 'N';
        if (lat < 16) return 'P';
        if (lat < 24) return 'Q';
        if (lat < 32) return 'R';
        if (lat < 40) return 'S';
        if (lat < 48) return 'T';
        if (lat < 56) return 'U';
        if (lat < 64) return 'V';
        if (lat < 72) return 'W';
        return 'X';
    }

    private static double GetBandCenterLatitude(char latBand)
    {
        return latBand switch
        {
            'C' => -76,
            'D' => -68,
            'E' => -60,
            'F' => -52,
            'G' => -44,
            'H' => -36,
            'J' => -28,
            'K' => -20,
            'L' => -12,
            'M' => -4,
            'N' => 4,
            'P' => 12,
            'Q' => 20,
            'R' => 28,
            'S' => 36,
            'T' => 44,
            'U' => 52,
            'V' => 60,
            'W' => 68,
            'X' => 76,
            _ => throw new ArgumentException($"Invalid latitude band: {latBand}")
        };
    }

    private static (double longitude, double latitude) UtmToLatLon(double easting, double northing, int zone, bool isNorthernHemisphere)
    {
        double x = easting - 500000.0;
        double y = northing;

        if (!isNorthernHemisphere)
            y -= 10000000.0;

        double λ0 = (zone * 6 - 183) * Math.PI / 180.0;

        double M = y / UtmScaleFactor;

        double μ = M / (SemiMajorAxis * (1 - e2 / 4 - 3 * e2 * e2 / 64 - 5 * e2 * e2 * e2 / 256));

        double e1 = (1 - Math.Sqrt(1 - e2)) / (1 + Math.Sqrt(1 - e2));

        double φ1 = μ +
            (3 * e1 / 2 - 27 * e1 * e1 * e1 / 32) * Math.Sin(2 * μ) +
            (21 * e1 * e1 / 16 - 55 * e1 * e1 * e1 * e1 / 32) * Math.Sin(4 * μ) +
            (151 * e1 * e1 * e1 / 96) * Math.Sin(6 * μ) +
            (1097 * e1 * e1 * e1 * e1 / 512) * Math.Sin(8 * μ);

        double sinφ1 = Math.Sin(φ1);
        double cosφ1 = Math.Cos(φ1);
        double tanφ1 = Math.Tan(φ1);

        double N1 = SemiMajorAxis / Math.Sqrt(1 - e2 * sinφ1 * sinφ1);
        double T1 = tanφ1 * tanφ1;
        double C1 = ep2 * cosφ1 * cosφ1;
        double R1 = SemiMajorAxis * (1 - e2) / Math.Pow(1 - e2 * sinφ1 * sinφ1, 1.5);
        double D = x / (N1 * UtmScaleFactor);

        double φ = φ1 -
            (N1 * tanφ1 / R1) * (
                D * D / 2 -
                (5 + 3 * T1 + 10 * C1 - 4 * C1 * C1 - 9 * ep2) * Math.Pow(D, 4) / 24 +
                (61 + 90 * T1 + 298 * C1 + 45 * T1 * T1 - 252 * ep2 - 3 * C1 * C1) * Math.Pow(D, 6) / 720
            );

        double λ = λ0 + (
            D -
            (1 + 2 * T1 + C1) * Math.Pow(D, 3) / 6 +
            (5 - 2 * C1 + 28 * T1 - 3 * C1 * C1 + 8 * ep2 + 24 * T1 * T1) * Math.Pow(D, 5) / 120
        ) / cosφ1;

        double latitude = φ * 180.0 / Math.PI;
        double longitude = λ * 180.0 / Math.PI;

        return (longitude, latitude);
    }

    #endregion Helpers
}
