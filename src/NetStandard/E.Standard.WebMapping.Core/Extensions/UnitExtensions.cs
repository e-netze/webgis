using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Extensions;

public enum Unit {
    Meter,
    Kilometer,
    Mile,
    Foot,
    Yard,
    Inch,
    NauticalMile,
    Hectare,
    Acre,
    Ar
}

public static class UnitExtensions
{
    public static string ToAbbreviation(this Unit unit)
    {
        return unit switch
        {
            Unit.Meter => "m",
            Unit.Kilometer => "km",
            Unit.Mile => "mi",
            Unit.Foot => "ft",
            Unit.Yard => "yd",
            Unit.Inch => "in",
            Unit.NauticalMile => "nmi",
            Unit.Hectare => "ha",
            Unit.Acre => "ac",
            Unit.Ar => "ar", // 1 are = 100 square meters
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }

    public static string ToSquareAbbreviation(this Unit unit)
    {
        return unit switch
        {
            Unit.Meter => "m²",
            Unit.Kilometer => "km²",
            Unit.Mile => "mi²",
            Unit.Foot => "ft²",
            Unit.Yard => "yd²",
            Unit.Inch => "in²",
            Unit.NauticalMile => "nmi²",
            Unit.Hectare => "ha",
            Unit.Acre => "ac",
            Unit.Ar => "ar", // 1 are = 100 square meters
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }

    public static Unit FromUnitAbbreviation(this string abbreviation, bool allowSquareInAbbreviation = true) // also allow m2, m², etc => will return Unit.Meter
    {
        if (string.IsNullOrWhiteSpace(abbreviation))
            throw new ArgumentNullException(nameof(abbreviation));

        var abbr = abbreviation.Trim().ToLowerInvariant();

        // Handle square abbreviations if allowed
        if (allowSquareInAbbreviation)
        {
            if (abbr == "m2" || abbr == "m²")
                return Unit.Meter;
            if (abbr == "km2" || abbr == "km²")
                return Unit.Kilometer;
            if (abbr == "mi2" || abbr == "mi²")
                return Unit.Mile;
            if (abbr == "ft2" || abbr == "ft²")
                return Unit.Foot;
            if (abbr == "yd2" || abbr == "yd²")
                return Unit.Yard;
            if (abbr == "in2" || abbr == "in²")
                return Unit.Inch;
            if (abbr == "nmi2" || abbr == "nmi²")
                return Unit.NauticalMile;
        }

        return abbr switch
        {
            "m" => Unit.Meter,
            "km" => Unit.Kilometer,
            "mi" => Unit.Mile,
            "ft" => Unit.Foot,
            "yd" => Unit.Yard,
            "in" => Unit.Inch,
            "nmi" => Unit.NauticalMile,
            "ha" => Unit.Hectare,
            "ac" => Unit.Acre,
            "ar" => Unit.Ar,
            "a" => Unit.Ar,
            _ => throw new ArgumentOutOfRangeException(nameof(abbreviation), abbreviation, null)
        };
    }

    public static double ToConversionFactor(this Unit unit)
    {
        return unit switch
        {
            Unit.Meter => 1.0,
            Unit.Kilometer => 1000.0,
            Unit.Mile => 1609.34,
            Unit.Foot => 0.3048,
            Unit.Yard => 0.9144,
            Unit.Inch => 0.0254,
            Unit.NauticalMile => 1852.0,
            Unit.Hectare => 10000.0, // 1 hectare = 10,000 square meters
            Unit.Acre => 4046.86, // 1 acre = 4046.86 square meters
            Unit.Ar => 100.0, // 1 are = 100 square meters
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }

    public static bool IsSquareUnit(this Unit unit)
        => unit switch
        {
            Unit.Hectare => true,
            Unit.Acre => true,
            Unit.Ar => true,
            _ => false
        };
    
    public static double ToMeters(this double number, Unit unit)
        => number * unit.ToConversionFactor();

    public static double MetersToUnit(this double meters, Unit unit)
        => meters / unit.ToConversionFactor();

    public static double SquareMetersToSquareUnit(this double squareMeters, Unit unit)
        => unit.IsSquareUnit()
            ? squareMeters / unit.ToConversionFactor()
            : squareMeters / (unit.ToConversionFactor() * unit.ToConversionFactor());
}
