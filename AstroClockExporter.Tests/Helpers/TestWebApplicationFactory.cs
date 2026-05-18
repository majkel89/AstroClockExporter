using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace AstroClockExporter.Tests.Helpers;

internal sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly DateTimeOffset FixedUtc = new(2025, 6, 21, 12, 0, 0, TimeSpan.Zero);

    private readonly string? _previousConfigFile;

    public TestWebApplicationFactory(string? configFileName = "test-config.yml")
    {
        _previousConfigFile = Environment.GetEnvironmentVariable("CONFIG_FILE");
        var configPath = configFileName is null
            ? string.Empty
            : Path.Combine(AppContext.BaseDirectory, "Resources", configFileName);
        Environment.SetEnvironmentVariable("CONFIG_FILE", configPath);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FakeTimeProvider(FixedUtc));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Environment.SetEnvironmentVariable("CONFIG_FILE", _previousConfigFile);
        }
        base.Dispose(disposing);
    }
}
