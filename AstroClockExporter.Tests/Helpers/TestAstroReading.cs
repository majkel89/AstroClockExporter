using AstroClockExporter.Core.Astro;

namespace AstroClockExporter.Tests.Helpers;

internal static class TestAstroReading
{
    public static AstroReading Deterministic() => new()
    {
        SunAltitudeDegrees = 42.125,
        SunAzimuthDegrees = 180.5,
        MoonAltitudeDegrees = -10.25,
        MoonAzimuthDegrees = 95.75,
        MoonIlluminationFraction = 0.625,
        MoonPhaseAngleDegrees = 78.5,
        MoonDistanceKilometres = 384_400.0,
        SunriseUnix = 1_750_000_000,
        SunsetUnix = 1_750_050_000,
        SolarNoonUnix = 1_750_025_000,
        CivilDawnUnix = 1_749_998_000,
        CivilDuskUnix = 1_750_052_000,
        NauticalDawnUnix = 1_749_996_000,
        NauticalDuskUnix = 1_750_054_000,
        AstronomicalDawnUnix = 1_749_994_000,
        AstronomicalDuskUnix = 1_750_056_000,
        MoonriseUnix = 1_750_010_000,
        MoonsetUnix = 1_750_060_000,
        MoonTransitUnix = 1_750_035_000,
        CalculationSeconds = 0.012345,
    };

    public static AstroReading WithSpecialFloats() => Deterministic() with
    {
        SunAltitudeDegrees = double.NaN,
        MoonAzimuthDegrees = double.PositiveInfinity,
        MoonDistanceKilometres = double.NegativeInfinity,
        SunriseUnix = double.NaN,
        SunsetUnix = double.NaN,
        SolarNoonUnix = double.NaN,
        CivilDawnUnix = double.NaN,
        CivilDuskUnix = double.NaN,
        NauticalDawnUnix = double.NaN,
        NauticalDuskUnix = double.NaN,
        AstronomicalDawnUnix = double.NaN,
        AstronomicalDuskUnix = double.NaN,
        MoonriseUnix = double.NaN,
        MoonsetUnix = double.NaN,
        MoonTransitUnix = double.NaN,
    };
}
