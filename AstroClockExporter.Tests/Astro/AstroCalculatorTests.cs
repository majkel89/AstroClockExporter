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

        Assert.Equal(long.MinValue, reading.SunriseUnix);
        Assert.Equal(long.MinValue, reading.SunsetUnix);
    }

    // Strips CalculationSeconds (wall-clock, non-deterministic) and rounds doubles to match
    // the precision of the Prometheus formatter for cross-CPU snapshot stability.
    private static object Project(AstroReading r) => new
    {
        SunAltitudeDegrees = Round(r.SunAltitudeDegrees, 2),
        SunAzimuthDegrees = Round(r.SunAzimuthDegrees, 2),
        MoonAltitudeDegrees = Round(r.MoonAltitudeDegrees, 2),
        MoonAzimuthDegrees = Round(r.MoonAzimuthDegrees, 2),
        MoonIlluminationFraction = Round(r.MoonIlluminationFraction, 4),
        MoonPhaseAngleDegrees = Round(r.MoonPhaseAngleDegrees, 2),
        MoonDistanceKilometres = Event(r.MoonDistanceKilometres),
        SunriseUnix = Event(r.SunriseUnix),
        SunsetUnix = Event(r.SunsetUnix),
        SolarNoonUnix = Event(r.SolarNoonUnix),
        CivilDawnUnix = Event(r.CivilDawnUnix),
        CivilDuskUnix = Event(r.CivilDuskUnix),
        NauticalDawnUnix = Event(r.NauticalDawnUnix),
        NauticalDuskUnix = Event(r.NauticalDuskUnix),
        AstronomicalDawnUnix = Event(r.AstronomicalDawnUnix),
        AstronomicalDuskUnix = Event(r.AstronomicalDuskUnix),
        MoonriseUnix = Event(r.MoonriseUnix),
        MoonsetUnix = Event(r.MoonsetUnix),
        MoonTransitUnix = Event(r.MoonTransitUnix),
    };

    private static double Round(double value, int decimals) =>
        double.IsFinite(value) ? Math.Round(value, decimals) : value;

    private static object Event(long value) =>
        value == long.MinValue ? (object)"NaN" : value;
}
