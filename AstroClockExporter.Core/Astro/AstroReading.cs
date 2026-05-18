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
    public double MoonDistanceKilometres { get; init; }

    // Sun events for today (Unix seconds, NaN if event doesn't occur)
    public double SunriseUnix { get; init; }
    public double SunsetUnix { get; init; }
    public double SolarNoonUnix { get; init; }
    public double CivilDawnUnix { get; init; }
    public double CivilDuskUnix { get; init; }
    public double NauticalDawnUnix { get; init; }
    public double NauticalDuskUnix { get; init; }
    public double AstronomicalDawnUnix { get; init; }
    public double AstronomicalDuskUnix { get; init; }

    // Moon events for today (Unix seconds, NaN if event doesn't occur)
    public double MoonriseUnix { get; init; }
    public double MoonsetUnix { get; init; }
    public double MoonTransitUnix { get; init; }

    // Self-monitoring
    public double CalculationSeconds { get; init; }
}
