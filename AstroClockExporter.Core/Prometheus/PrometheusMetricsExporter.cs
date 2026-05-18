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
            reading.SunAltitudeDegrees);
        AppendGauge(sb, "astro_sun_azimuth_degrees",
            "Current sun azimuth (degrees, measured clockwise from north)",
            reading.SunAzimuthDegrees);

        AppendGauge(sb, "astro_moon_altitude_degrees",
            "Current moon altitude above the horizon (degrees)",
            reading.MoonAltitudeDegrees);
        AppendGauge(sb, "astro_moon_azimuth_degrees",
            "Current moon azimuth (degrees, measured clockwise from north)",
            reading.MoonAzimuthDegrees);
        AppendGauge(sb, "astro_moon_illumination_fraction",
            "Fraction of the moon's disk that is illuminated (0..1)",
            reading.MoonIlluminationFraction);
        AppendGauge(sb, "astro_moon_phase_angle_degrees",
            "Moon phase angle (degrees, 0 = full moon, 180 = new moon)",
            reading.MoonPhaseAngleDegrees);
        AppendGauge(sb, "astro_moon_distance_km",
            "Earth-Moon centre-to-centre distance (kilometres)",
            reading.MoonDistanceKilometres);

        sb.Append("# HELP astro_sun_event_time_seconds Sun event time today (Unix seconds, UTC). NaN if event does not occur.\n");
        sb.Append("# TYPE astro_sun_event_time_seconds gauge\n");
        AppendEvent(sb, "astro_sun_event_time_seconds", "sunrise", reading.SunriseUnix);
        AppendEvent(sb, "astro_sun_event_time_seconds", "sunset", reading.SunsetUnix);
        AppendEvent(sb, "astro_sun_event_time_seconds", "solar_noon", reading.SolarNoonUnix);
        AppendEvent(sb, "astro_sun_event_time_seconds", "civil_dawn", reading.CivilDawnUnix);
        AppendEvent(sb, "astro_sun_event_time_seconds", "civil_dusk", reading.CivilDuskUnix);
        AppendEvent(sb, "astro_sun_event_time_seconds", "nautical_dawn", reading.NauticalDawnUnix);
        AppendEvent(sb, "astro_sun_event_time_seconds", "nautical_dusk", reading.NauticalDuskUnix);
        AppendEvent(sb, "astro_sun_event_time_seconds", "astronomical_dawn", reading.AstronomicalDawnUnix);
        AppendEvent(sb, "astro_sun_event_time_seconds", "astronomical_dusk", reading.AstronomicalDuskUnix);

        sb.Append("# HELP astro_moon_event_time_seconds Moon event time today (Unix seconds, UTC). NaN if event does not occur.\n");
        sb.Append("# TYPE astro_moon_event_time_seconds gauge\n");
        AppendEvent(sb, "astro_moon_event_time_seconds", "moonrise", reading.MoonriseUnix);
        AppendEvent(sb, "astro_moon_event_time_seconds", "moonset", reading.MoonsetUnix);
        AppendEvent(sb, "astro_moon_event_time_seconds", "moon_transit", reading.MoonTransitUnix);

        AppendGauge(sb, "astro_calculation_seconds",
            "Wall-clock time spent computing this scrape (seconds)",
            reading.CalculationSeconds);

        logger.LogCalculationFinished(locationName, reading.CalculationSeconds);

        return Result<string>.Success(sb.ToString());
    }

    private static void AppendGauge(StringBuilder sb, string name, string help, double value)
    {
        sb.Append("# HELP ").Append(name).Append(' ').Append(help).Append('\n');
        sb.Append("# TYPE ").Append(name).Append(" gauge\n");
        sb.Append(name).Append(' ').Append(FormatValue(value)).Append('\n');
    }

    private static void AppendEvent(StringBuilder sb, string name, string eventLabel, double value)
    {
        sb.Append(name).Append("{event=\"").Append(eventLabel).Append("\"} ").Append(FormatValue(value)).Append('\n');
    }

    private static string FormatValue(double value)
    {
        if (double.IsNaN(value)) return "NaN";
        if (double.IsPositiveInfinity(value)) return "+Inf";
        if (double.IsNegativeInfinity(value)) return "-Inf";
        return value.ToString("R", CultureInfo.InvariantCulture);
    }
}
