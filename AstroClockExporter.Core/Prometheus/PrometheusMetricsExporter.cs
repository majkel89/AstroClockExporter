using System.Globalization;
using System.Net;
using System.Text;
using AstroClockExporter.Core.Astro;
using AstroClockExporter.Core.Common;
using Microsoft.Extensions.Logging;

namespace AstroClockExporter.Core.Prometheus;

internal sealed class PrometheusMetricsExporter(
    IAstroCalculator calculator,
    IConfigProvider configProvider,
    TimeProvider timeProvider,
    ILogger<PrometheusMetricsExporter> logger) : IMetricsExporter
{
    public Result<string> ExportMetrics(string locationName)
    {
        using var _ = logger.BeginScope("Location:{location}", locationName);

        var locationResult = configProvider.GetLocation(locationName);
        if (locationResult.IsFail)
        {
            logger.LogLocationNotFound(locationName);
            return Result<string>.Fail((int)HttpStatusCode.NotFound, $"Location {locationName} not found");
        }

        logger.LogCalculationStarted(locationName);

        var reading = calculator.Calculate(locationResult.Value, timeProvider.GetUtcNow().UtcDateTime);

        var sb = new StringBuilder(2048);

        AppendGauge(sb, "astro_sun_altitude_degrees",
            "Current sun altitude above the horizon (degrees)",
            FormatDegrees(reading.SunAltitudeDegrees));
        AppendGauge(sb, "astro_sun_azimuth_degrees",
            "Current sun azimuth (degrees, measured clockwise from north)",
            FormatDegrees(reading.SunAzimuthDegrees));

        AppendGauge(sb, "astro_moon_altitude_degrees",
            "Current moon altitude above the horizon (degrees)",
            FormatDegrees(reading.MoonAltitudeDegrees));
        AppendGauge(sb, "astro_moon_azimuth_degrees",
            "Current moon azimuth (degrees, measured clockwise from north)",
            FormatDegrees(reading.MoonAzimuthDegrees));
        AppendGauge(sb, "astro_moon_illumination_fraction",
            "Fraction of the moon's disk that is illuminated (0..1)",
            FormatFraction(reading.MoonIlluminationFraction));
        AppendGauge(sb, "astro_moon_phase_angle_degrees",
            "Moon phase angle (degrees, 0 = full moon, 180 = new moon)",
            FormatDegrees(reading.MoonPhaseAngleDegrees));
        AppendGauge(sb, "astro_moon_distance_km",
            "Earth-Moon centre-to-centre distance (kilometres)",
            FormatInteger(reading.MoonDistanceKilometres));

        sb.Append("# HELP astro_sun_event_time_seconds Sun event time today (Unix seconds, UTC). NaN if event does not occur.\n");
        sb.Append("# TYPE astro_sun_event_time_seconds gauge\n");
        AppendEvent(sb, "astro_sun_event_time_seconds", "sunrise", FormatInteger(reading.SunriseUnix));
        AppendEvent(sb, "astro_sun_event_time_seconds", "sunset", FormatInteger(reading.SunsetUnix));
        AppendEvent(sb, "astro_sun_event_time_seconds", "solar_noon", FormatInteger(reading.SolarNoonUnix));
        AppendEvent(sb, "astro_sun_event_time_seconds", "civil_dawn", FormatInteger(reading.CivilDawnUnix));
        AppendEvent(sb, "astro_sun_event_time_seconds", "civil_dusk", FormatInteger(reading.CivilDuskUnix));
        AppendEvent(sb, "astro_sun_event_time_seconds", "nautical_dawn", FormatInteger(reading.NauticalDawnUnix));
        AppendEvent(sb, "astro_sun_event_time_seconds", "nautical_dusk", FormatInteger(reading.NauticalDuskUnix));
        AppendEvent(sb, "astro_sun_event_time_seconds", "astronomical_dawn", FormatInteger(reading.AstronomicalDawnUnix));
        AppendEvent(sb, "astro_sun_event_time_seconds", "astronomical_dusk", FormatInteger(reading.AstronomicalDuskUnix));

        sb.Append("# HELP astro_moon_event_time_seconds Moon event time today (Unix seconds, UTC). NaN if event does not occur.\n");
        sb.Append("# TYPE astro_moon_event_time_seconds gauge\n");
        AppendEvent(sb, "astro_moon_event_time_seconds", "moonrise", FormatInteger(reading.MoonriseUnix));
        AppendEvent(sb, "astro_moon_event_time_seconds", "moonset", FormatInteger(reading.MoonsetUnix));
        AppendEvent(sb, "astro_moon_event_time_seconds", "moon_transit", FormatInteger(reading.MoonTransitUnix));

        AppendGauge(sb, "astro_calculation_seconds",
            "Wall-clock time spent computing this scrape (seconds)",
            FormatFraction(reading.CalculationSeconds));

        logger.LogCalculationFinished(locationName, reading.CalculationSeconds);

        return Result<string>.Success(sb.ToString());
    }

    private static void AppendGauge(StringBuilder sb, string name, string help, string value)
    {
        sb.Append("# HELP ").Append(name).Append(' ').Append(help).Append('\n');
        sb.Append("# TYPE ").Append(name).Append(" gauge\n");
        sb.Append(name).Append(' ').Append(value).Append('\n');
    }

    private static void AppendEvent(StringBuilder sb, string name, string eventLabel, string value)
    {
        sb.Append(name).Append("{event=\"").Append(eventLabel).Append("\"} ").Append(value).Append('\n');
    }

    // 2 dp — matches Meeus truncated VSOP87 accuracy (~36 arcseconds ≈ 0.01°).
    private static string FormatDegrees(double value)
    {
        if (double.IsNaN(value)) return "NaN";
        if (double.IsPositiveInfinity(value)) return "+Inf";
        if (double.IsNegativeInfinity(value)) return "-Inf";
        return Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("G", CultureInfo.InvariantCulture);
    }

    // 4 dp — for illumination fraction (propagated position error ~0.00009) and wall-clock duration.
    private static string FormatFraction(double value)
    {
        if (double.IsNaN(value)) return "NaN";
        if (double.IsPositiveInfinity(value)) return "+Inf";
        if (double.IsNegativeInfinity(value)) return "-Inf";
        return Math.Round(value, 4, MidpointRounding.AwayFromZero).ToString("G", CultureInfo.InvariantCulture);
    }

    // Integer — for event timestamps (step resolution ~10 min) and moon distance (accuracy ±10 km).
    // long.MinValue is the sentinel for "event did not occur" (Prometheus NaN).
    private static string FormatInteger(long value) =>
        value == long.MinValue ? "NaN" : value.ToString(CultureInfo.InvariantCulture);
}
