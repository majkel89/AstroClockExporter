using AstroClockExporter.Core.Astro;
using AstroClockExporter.Core.Configuration.DTO;

namespace AstroClockExporter.Tests.Astro;

public class AstroCalculatorTests
{
    private static readonly Location Warsaw = new() { Latitude = 52.2297, Longitude = 21.0122, Elevation = 110 };
    private static readonly Location Equator = new() { Latitude = 0, Longitude = 0, Elevation = 0 };
    private static readonly Location Tromso = new() { Latitude = 69.6492, Longitude = 18.9553, Elevation = 10 };

    private readonly AstroCalculator _calculator = new();

    [Fact]
    public Task Calculate_WarsawSummerSolsticeNoon_MatchesSnapshot()
    {
        var utc = new DateTime(2025, 6, 21, 12, 0, 0, DateTimeKind.Utc);

        var reading = _calculator.Calculate(Warsaw, utc);

        return Verify(Project(reading));
    }

    [Fact]
    public Task Calculate_EquatorEquinoxSunrise_MatchesSnapshot()
    {
        var utc = new DateTime(2025, 3, 20, 6, 0, 0, DateTimeKind.Utc);

        var reading = _calculator.Calculate(Equator, utc);

        return Verify(Project(reading));
    }

    [Fact]
    public Task Calculate_TromsoPolarNight_MatchesSnapshot()
    {
        var utc = new DateTime(2025, 12, 21, 12, 0, 0, DateTimeKind.Utc);

        var reading = _calculator.Calculate(Tromso, utc);

        return Verify(Project(reading));
    }

    [Fact]
    public void Calculate_PopulatesCalculationSeconds_GreaterThanZero()
    {
        var reading = _calculator.Calculate(Warsaw, new DateTime(2025, 6, 21, 12, 0, 0, DateTimeKind.Utc));

        Assert.True(reading.CalculationSeconds > 0,
            $"Expected positive elapsed seconds, got {reading.CalculationSeconds}");
    }

    [Fact]
    public void Calculate_TromsoPolarNight_ReturnsNaNForSunriseAndSunset()
    {
        var reading = _calculator.Calculate(Tromso, new DateTime(2025, 12, 21, 12, 0, 0, DateTimeKind.Utc));

        Assert.True(double.IsNaN(reading.SunriseUnix));
        Assert.True(double.IsNaN(reading.SunsetUnix));
    }

    // Strips CalculationSeconds (wall-clock, non-deterministic) and rounds doubles for
    // cross-CPU snapshot stability. AASharp itself is deterministic for fixed UT input.
    private static object Project(AstroReading r) => new
    {
        SunAltitudeDegrees = Round(r.SunAltitudeDegrees),
        SunAzimuthDegrees = Round(r.SunAzimuthDegrees),
        MoonAltitudeDegrees = Round(r.MoonAltitudeDegrees),
        MoonAzimuthDegrees = Round(r.MoonAzimuthDegrees),
        MoonIlluminationFraction = Round(r.MoonIlluminationFraction),
        MoonPhaseAngleDegrees = Round(r.MoonPhaseAngleDegrees),
        MoonDistanceKilometres = Round(r.MoonDistanceKilometres, 3),
        SunriseUnix = RoundEvent(r.SunriseUnix),
        SunsetUnix = RoundEvent(r.SunsetUnix),
        SolarNoonUnix = RoundEvent(r.SolarNoonUnix),
        CivilDawnUnix = RoundEvent(r.CivilDawnUnix),
        CivilDuskUnix = RoundEvent(r.CivilDuskUnix),
        NauticalDawnUnix = RoundEvent(r.NauticalDawnUnix),
        NauticalDuskUnix = RoundEvent(r.NauticalDuskUnix),
        AstronomicalDawnUnix = RoundEvent(r.AstronomicalDawnUnix),
        AstronomicalDuskUnix = RoundEvent(r.AstronomicalDuskUnix),
        MoonriseUnix = RoundEvent(r.MoonriseUnix),
        MoonsetUnix = RoundEvent(r.MoonsetUnix),
        MoonTransitUnix = RoundEvent(r.MoonTransitUnix),
    };

    private static double Round(double value, int decimals = 6) =>
        double.IsFinite(value) ? Math.Round(value, decimals) : value;

    private static double RoundEvent(double value) =>
        double.IsFinite(value) ? Math.Round(value, 0) : value;
}
