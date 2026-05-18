using System.Net;
using AstroClockExporter.Core;
using AstroClockExporter.Core.Astro;
using AstroClockExporter.Core.Common;
using AstroClockExporter.Core.Configuration.DTO;
using AstroClockExporter.Core.Prometheus;
using AstroClockExporter.Tests.Helpers;
using Microsoft.Extensions.Logging;

namespace AstroClockExporter.Tests.Prometheus;

public class PrometheusMetricsExporterTests
{
    private static readonly DateTime FixedUtc = new(2025, 6, 21, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Location Warsaw = new() { Latitude = 52.2297, Longitude = 21.0122, Elevation = 110 };

    private readonly IAstroCalculator _calculator = Substitute.For<IAstroCalculator>();
    private readonly IConfigProvider _configProvider = Substitute.For<IConfigProvider>();
    private readonly TimeProvider _timeProvider = new FakeTimeProvider(new DateTimeOffset(FixedUtc, TimeSpan.Zero));
    private readonly ILogger<PrometheusMetricsExporter> _logger = TestLoggers.ForType<PrometheusMetricsExporter>();

    private PrometheusMetricsExporter CreateExporter() => new(_calculator, _configProvider, _timeProvider, _logger);

    [Fact]
    public void ExportMetrics_UnknownLocation_ReturnsNotFoundAndLogsWarning()
    {
        _configProvider.GetLocation("mystery").Returns(Result<Location>.Fail("Location mystery not found"));

        var result = CreateExporter().ExportMetrics("mystery");

        Assert.True(result.IsFail);
        Assert.Equal((int)HttpStatusCode.NotFound, result.Err!.Code);
        Assert.Equal("Location mystery not found", result.Err.Message);
        _logger.ShouldLogWarning("Unknown location mystery", eventId: 3);
        _calculator.DidNotReceiveWithAnyArgs().Calculate(null!, default);
    }

    [Fact]
    public void ExportMetrics_KnownLocation_LogsLifecycle()
    {
        _configProvider.GetLocation("warsaw").Returns(Result<Location>.Success(Warsaw));
        _calculator.Calculate(Warsaw, FixedUtc).Returns(TestAstroReading.Deterministic());

        var result = CreateExporter().ExportMetrics("warsaw");

        Assert.True(result.IsSuccess);
        _logger.ShouldLogInfo("Calculation started for warsaw", eventId: 1);
        _logger.Received(1).Log(LogLevel.Information, Arg.Is<EventId>(e => e.Id == 2),
            Arg.Is<object>(s => s.ToString()!.StartsWith("Calculation finished for warsaw in ")),
            null, Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public Task ExportMetrics_KnownLocation_MatchesSnapshot()
    {
        _configProvider.GetLocation("warsaw").Returns(Result<Location>.Success(Warsaw));
        _calculator.Calculate(Warsaw, FixedUtc).Returns(TestAstroReading.Deterministic());

        var result = CreateExporter().ExportMetrics("warsaw");

        Assert.True(result.IsSuccess);
        return Verify(result.Value);
    }

    [Fact]
    public Task ExportMetrics_HandlesNaNAndInfinity_MatchesSnapshot()
    {
        _configProvider.GetLocation("warsaw").Returns(Result<Location>.Success(Warsaw));
        _calculator.Calculate(Warsaw, FixedUtc).Returns(TestAstroReading.WithSpecialFloats());

        var result = CreateExporter().ExportMetrics("warsaw");

        Assert.True(result.IsSuccess);
        return Verify(result.Value);
    }

    [Fact]
    public void ExportMetrics_KnownLocation_ContainsAllExpectedGauges()
    {
        _configProvider.GetLocation("warsaw").Returns(Result<Location>.Success(Warsaw));
        _calculator.Calculate(Warsaw, FixedUtc).Returns(TestAstroReading.Deterministic());

        var output = CreateExporter().ExportMetrics("warsaw").Value!;

        string[] expectedGauges =
        [
            "astro_sun_altitude_degrees",
            "astro_sun_azimuth_degrees",
            "astro_moon_altitude_degrees",
            "astro_moon_azimuth_degrees",
            "astro_moon_illumination_fraction",
            "astro_moon_phase_angle_degrees",
            "astro_moon_distance_km",
            "astro_sun_event_time_seconds",
            "astro_moon_event_time_seconds",
            "astro_calculation_seconds",
        ];
        foreach (var gauge in expectedGauges)
        {
            Assert.Contains($"# TYPE {gauge} gauge", output);
        }
    }
}
