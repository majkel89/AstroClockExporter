using System.Net;
using System.Text.RegularExpressions;
using AstroClockExporter.Tests.Helpers;

namespace AstroClockExporter.Tests.Api;

[Collection("EnvVars")]
public class ProgramEndpointTests
{
    [Fact]
    public async Task Root_Returns200WithBanner()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        response.EnsureStatusCode(HttpStatusCode.OK, "text/plain");
        await response.EnsureResponseIs("AstroClockExporter");
    }

    [Fact]
    public async Task Healthy_Returns200WithHealthy()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/healthy");

        response.EnsureStatusCode(HttpStatusCode.OK, "text/plain");
        await response.EnsureResponseIs("healthy");
    }

    [Fact]
    public async Task Metrics_KnownLocation_Returns200WithPrometheusPayload()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/metrics?location=warsaw");

        response.EnsureStatusCode(HttpStatusCode.OK, "text/plain");
        await response.EnsureResponseMatches(new Regex(@"astro_sun_altitude_degrees \S+"));
        await response.EnsureResponseMatches(new Regex(@"astro_moon_event_time_seconds\{event=""moonrise""\} \S+"));
    }

    [Fact]
    public async Task Metrics_WarsawAtFixedTime_MatchesSnapshot()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/metrics?location=warsaw");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        // Strip the wall-clock self-monitoring gauge so the snapshot is stable across runs.
        var scrubbed = Regex.Replace(body, @"^astro_calculation_seconds \S+$", "astro_calculation_seconds <scrubbed>", RegexOptions.Multiline);
        await Verify(scrubbed).UseDirectory("../Api");
    }

    [Fact]
    public async Task Metrics_MissingLocationParameter_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/metrics");

        response.EnsureStatusCode(HttpStatusCode.BadRequest, "text/plain");
        await response.EnsureResponseIs("Parameter location is required");
    }

    [Fact]
    public async Task Metrics_BlankLocationParameter_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/metrics?location=%20%20");

        response.EnsureStatusCode(HttpStatusCode.BadRequest, "text/plain");
        await response.EnsureResponseIs("Parameter location is required");
    }

    [Fact]
    public async Task Metrics_UnknownLocation_Returns404()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/metrics?location=mystery");

        response.EnsureStatusCode(HttpStatusCode.NotFound, "text/plain");
        await response.EnsureResponseIs("Location mystery not found");
    }
}
