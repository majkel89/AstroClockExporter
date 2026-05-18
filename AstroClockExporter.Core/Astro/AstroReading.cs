namespace AstroClockExporter.Core.Astro;

public sealed record AstroReading
{
    // Current position
    public double SunAltitudeDegrees { get; init; }
    public double SunAzimuthDegrees { get; init; }
    public double MoonAltitudeDegrees { get; init; }
    public double MoonAzimuthDegrees { get; init; }

    // Moon metadata
    public double MoonIlluminationFraction { get; init; }
    public double MoonPhaseAngleDegrees { get; init; }
    public long MoonDistanceKilometres { get; init; }

    // Sun events for today (Unix seconds, long.MinValue if event doesn't occur)
    public long SunriseUnix { get; init; }
    public long SunsetUnix { get; init; }
    public long SolarNoonUnix { get; init; }
    public long CivilDawnUnix { get; init; }
    public long CivilDuskUnix { get; init; }
    public long NauticalDawnUnix { get; init; }
    public long NauticalDuskUnix { get; init; }
    public long AstronomicalDawnUnix { get; init; }
    public long AstronomicalDuskUnix { get; init; }

    // Moon events for today (Unix seconds, long.MinValue if event doesn't occur)
    public long MoonriseUnix { get; init; }
    public long MoonsetUnix { get; init; }
    public long MoonTransitUnix { get; init; }

    // Self-monitoring
    public double CalculationSeconds { get; init; }
}
