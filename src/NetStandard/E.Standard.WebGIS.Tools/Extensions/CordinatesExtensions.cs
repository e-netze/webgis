#nullable enable

using E.Standard.Extensions.Compare;
using E.Standard.Extensions.Security;
using E.Standard.GeoCoding.Extensions;
using E.Standard.GeoCoding.GeoCode;
using E.Standard.Localization.Abstractions;
using E.Standard.Platform;
using E.Standard.WebGIS.Tools.Helpers;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Setters;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Extensions;

internal static class CordinatesExtensions
{
    static public IEnumerable<Point> GetWGSFormCoordinatesTable(this ApiToolEventArguments e, IBridge bridge)
    {
        var tableData = e[Coordinates.CoordinatesTableId];
        char separator = ';';
        var data = tableData.Split(separator);

        var heightColumns = new RasterQueryHelper().HeightNameNodes(System.IO.Path.Combine(bridge.AppEtcPath, "coordinates", "h.xml"));
        var colCount = 3 + heightColumns.Count();

        for (int i = 0; i < data.Length; i += colCount)
        {
            if (data.Length > i + 2)
            {
                yield return new Point(data[i + 1].ToPlatformDouble(), data[i + 2].ToPlatformDouble());
            }
        }
    }

    static public string Identifier(this Coordinates.Projection projection, bool idOnly = false) 
        => idOnly || String.IsNullOrEmpty(projection.DisplayStyle)
            ? projection.Id.ToString()
            : $"{projection.Id}.{projection.DisplayStyle}";


    static public IGeoCoder? TryGetGeoCoderOrNull(this Coordinates.Projection projection)
        => projection?.DisplayStyle switch
        {
            string geoCoderName when geoCoderName.TryGetGeoCoderByName() is IGeoCoder geoCoder => geoCoder,
            _ => null
        };

    #region Coordinates / Grad Minutes Seconds

    static public string Deg2GMS(this double deg, int digits)
    {
        digits = digits>=0 ? digits : 1;

        int g = (int)Math.Floor(deg);
        deg -= g;
        int m = (int)Math.Floor(deg * 60);
        deg -= m / 60.0;
        double s = Math.Round(deg * 3600.0, 3);

        if (s >= 60.0) { m++; s = 0.0; }
        if (m == 60.0) { g++; m = 0; }

        //int digits=getCoordDigits();
        string digs = "";
        if (digits > 0)
        {
            digs = ".";
        }

        for (int i = 0; i < digits; i++)
        {
            digs += "0";
        }

        return String.Format("{0}°{1:00}'{2:00" + digs + "}''", g, m, s);
        //return g.ToString()+"°"+m.ToString()+"'"+s.ToString()+"''";
    }

    static public string Deg2GM(this double deg, int digits)
    {
        int g = (int)Math.Floor(deg);
        deg -= g;
        double m = deg * 60;
        if (m >= 60.0) { g++; m = 0.0; }

        //int digits=getCoordDigits();
        string digs = "";
        if (digits > 0)
        {
            digs = ".";
        }

        for (int i = 0; i < digits; i++)
        {
            digs += "0";
        }

        return String.Format("{0}°{1:00" + digs + "}'", g, m);
    }

    static public double ParseCoordinateValue(this string val)
    {
        try
        {
            val = val.Trim().Replace(",", ".");

            #region Sonderzeichen für Grad, Minuten, Sekunden durch Leerzeichen ersetzen und doppelte Leererzeichen entfernen

            foreach (var s in new string[] { "°", "\"", "'", "g", "´", "`" })
            {
                val = val.Replace(s, " ").Trim();
            }

            while (val.Contains("  "))
            {
                val = val.Replace("  ", " ");
            }

            int sign = 1;
            if (val.Contains(":") || val.Split(':').Length == 2)
            {
                if (val.ToLower().StartsWith("w:") || val.ToLower().StartsWith("s:"))   // West/South -> negative Koordinaten
                {
                    sign = -1;
                }
                val = val.Split(':')[1].Trim();
            }

            #endregion

            string[] v = val.Split(' ');

            switch (v.Length)
            {
                case 1:
                    return v[0].ToPlatformDouble() * sign;
                case 2:
                    return (v[0].ToPlatformDouble() + v[1].ToPlatformDouble() / 60D) * sign;
                case 3:
                    return (v[0].ToPlatformDouble() + v[1].ToPlatformDouble() / 60D + v[2].ToPlatformDouble() / 3600D) * sign;
                default:
                    throw new Exception("Koordinatenwert kann nicht ermittelt werden.");
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Fehler bei der Eingabe: " + val + "\n" + ex.Message);
        }
    }

    static public Point RoundCoordiantes(this Point coord, SpatialReference sRef)
    {
        var digits = sRef.IsProjective switch
        {
            true => 2,
            false => 7
        };

        return new Point(Math.Round(coord.X, digits), Math.Round(coord.Y, digits));
    }

    static public Point ToPoint(this GeoLocation geoLocation)
        => new Point(geoLocation.Longitude, geoLocation.Latitude);

    static public (double x, double y) ToTuple(this GeoLocation geoLocation)
        => (geoLocation.Longitude, geoLocation.Latitude);

    #endregion

    #region Parse Columns

    static private string[]? RowCells(this string row)
    {
        string[]? cells = null;

        foreach (var c in new char[] { ';', '\t', ' ' })
        {
            cells = row.Split(c);
            if (cells.Length >= 2)
            {
                break;
            }
        }

        return cells;
    }

    static public (string, string , string) ParseXYRow(this string row, int rowIndex)
    {
        var cells = RowCells(row).CheckSecurity().ToArray();

        return cells?.Length switch
        {
            2 => (rowIndex.ToString(), cells[0], cells[1]),
            int l when l >= 3 => (cells[0], cells[1], cells[2]),
            _ => throw new ArgumentException()
        };
    }

    static public (string, string, string) ParseCodeRow(this string row, IGeoCoder geoCoder, int rowIndex)
    {
        var cells = RowCells(row)?.CheckSecurity().ToArray();

        string number = cells?
            .Where(c => int.TryParse(c, out _))
            .FirstOrDefault() ?? rowIndex.ToString();
        
        string code = cells?
            .Where(c => geoCoder.IsValidGeoCode(c) && !int.TryParse(c, out _))  // geocode and not the number cell
            .FirstOrDefault()
            .ThrowIfNull(() => "Invalid row: no valid code found")!;

        return (number, code, "");
    }

    #endregion

    #region UI

    static public ApiEventResponse AddCoordiantesCalculatorUISetters(this ApiEventResponse response, Coordinates.Projection projection)
    {
        var geoCoder = projection.TryGetGeoCoderOrNull();

        var xySetterType = geoCoder is null 
            ? UICssSetter.SetterType.RemoveClass
            : UICssSetter.SetterType.AddClass;
        var codeSetterType = geoCoder is null
            ? UICssSetter.SetterType.AddClass
            : UICssSetter.SetterType.RemoveClass;

        return response
            .AddUISetters(
                new UICssSetter(xySetterType, "coordinates-input-x-label", UICss.HiddenUIElement),
                new UICssSetter(xySetterType, "coordinates-input-x-value", UICss.HiddenUIElement),
                new UICssSetter(xySetterType, "coordinates-input-y-label", UICss.HiddenUIElement),
                new UICssSetter(xySetterType, "coordinates-input-y-value", UICss.HiddenUIElement),
                new UICssSetter(codeSetterType, "coordinates-input-code-label", UICss.HiddenUIElement),
                new UICssSetter(codeSetterType, "coordinates-input-code-value", UICss.HiddenUIElement)
            );
    }

    #endregion
}
