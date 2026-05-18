using Microsoft.Extensions.Logging;

namespace AstroClockExporter.Core.Prometheus;

using Logger = ILogger<PrometheusMetricsExporter>;

internal static partial class PrometheusMetricsExporterLogger
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Calculation started for {Location}")]
    internal static partial void LogCalculationStarted(this Logger logger, string location);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Calculation finished for {Location} in {Elapsed:F4}s")]
    internal static partial void LogCalculationFinished(this Logger logger, string location, double elapsed);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Unknown location {Location}")]
    internal static partial void LogLocationNotFound(this Logger logger, string location);
}
