using AstroClockExporter.Core;
using AstroClockExporter.Core.Configuration;
using AstroClockExporter.Core.Configuration.DTO;
using AstroClockExporter.Core.Configuration.Serialization;
using AstroClockExporter.Tests.Helpers;
using Microsoft.Extensions.Logging;
using TempDirectory = AstroClockExporter.Tests.Helpers.TempDirectory;

namespace AstroClockExporter.Tests.Configuration;

[Collection("EnvVars")]
public class ConfigProviderTests
{
    private readonly IConfigReader _reader = Substitute.For<IConfigReader>();
    private readonly ILogger<ConfigProvider> _logger = TestLoggers.ForType<ConfigProvider>();

    private ConfigProvider CreateProvider() => new(_reader, _logger);

    [Fact]
    public async Task LoadAsync_FileDoesNotExist_LogsWarningAndUsesEmptyConfig()
    {
        using var temp = new TempDirectory();
        var missingPath = temp.FileIn("does-not-exist.yml");
        var provider = CreateProvider();

        await provider.LoadAsync(missingPath, CancellationToken.None);

        Assert.Empty(provider.GetConfigRoot().Locations);
        _logger.ShouldLogWarning($"File {missingPath} does not exist - using default configuration", eventId: 2);
        _reader.DidNotReceiveWithAnyArgs().ReadConfigFromString(null!);
    }

    [Fact]
    public async Task LoadAsync_FileExists_DelegatesToReaderAndStoresResult()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("config.yml", "anything");
        var expected = new ConfigRoot
        {
            Locations = new Dictionary<string, Location>
            {
                ["warsaw"] = new() { Latitude = 52.2, Longitude = 21.0, Elevation = 110 },
            },
        };
        _reader.ReadConfigFromString(Arg.Any<string>()).Returns(expected);
        var provider = CreateProvider();

        await provider.LoadAsync(path, CancellationToken.None);

        var stored = provider.GetConfigRoot();
        Assert.Equal(expected.Locations.Keys, stored.Locations.Keys);
        Assert.Equal(expected.Locations["warsaw"], stored.Locations["warsaw"]);
    }

    [Fact]
    public async Task LoadAsync_ReaderReturnsNull_LogsEmptyAndUsesEmptyConfig()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("config.yml", "anything");
        _reader.ReadConfigFromString(Arg.Any<string>()).Returns((ConfigRoot?)null);
        var provider = CreateProvider();

        await provider.LoadAsync(path, CancellationToken.None);

        Assert.Empty(provider.GetConfigRoot().Locations);
        _logger.ShouldLogWarning($"File {path} is empty - using default configuration", eventId: 6);
    }

    [Fact]
    public async Task LoadAsync_SubstitutesEnvironmentVariables()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("config.yml", "latitude: ${ACE_TEST_LAT}");
        string? captured = null;
        _reader.ReadConfigFromString(Arg.Do<string>(c => captured = c)).Returns(new ConfigRoot());
        using var scope = new EnvVarScope().Set("ACE_TEST_LAT", "52.2297");
        var provider = CreateProvider();

        await provider.LoadAsync(path, CancellationToken.None);

        Assert.Equal("latitude: 52.2297", captured);
    }

    [Fact]
    public async Task LoadAsync_FallsBackToDefaultWhenEnvMissing()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("config.yml", "longitude: ${ACE_TEST_LON:-21}");
        string? captured = null;
        _reader.ReadConfigFromString(Arg.Do<string>(c => captured = c)).Returns(new ConfigRoot());
        using var scope = new EnvVarScope().Set("ACE_TEST_LON", null);
        var provider = CreateProvider();

        await provider.LoadAsync(path, CancellationToken.None);

        Assert.Equal("longitude: 21", captured);
    }

    [Fact]
    public async Task LoadAsync_RetainsLiteralWhenEnvMissingAndNoDefault()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("config.yml", "elevation: ${ACE_TEST_UNSET}");
        string? captured = null;
        _reader.ReadConfigFromString(Arg.Do<string>(c => captured = c)).Returns(new ConfigRoot());
        using var scope = new EnvVarScope().Set("ACE_TEST_UNSET", null);
        var provider = CreateProvider();

        await provider.LoadAsync(path, CancellationToken.None);

        Assert.Equal("elevation: ${ACE_TEST_UNSET}", captured);
    }

    [Fact]
    public void GetConfigRoot_BeforeLoad_ThrowsInvalidOperationException()
    {
        var provider = CreateProvider();

        Assert.Throws<InvalidOperationException>(provider.GetConfigRoot);
    }

    [Fact]
    public void GetLocation_BeforeLoad_ThrowsInvalidOperationException()
    {
        var provider = CreateProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetLocation("warsaw"));
    }

    [Fact]
    public async Task GetLocation_KnownName_ReturnsSuccess()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("config.yml", "any");
        var warsaw = new Location { Latitude = 52.2, Longitude = 21.0, Elevation = 110 };
        _reader.ReadConfigFromString(Arg.Any<string>()).Returns(new ConfigRoot
        {
            Locations = new Dictionary<string, Location> { ["warsaw"] = warsaw },
        });
        var provider = CreateProvider();
        await provider.LoadAsync(path, CancellationToken.None);

        var result = provider.GetLocation("warsaw");

        Assert.True(result.IsSuccess);
        Assert.Same(warsaw, result.Value);
    }

    [Fact]
    public async Task GetLocation_NameMatchIsCaseInsensitive()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("config.yml", "any");
        var warsaw = new Location { Latitude = 52.2, Longitude = 21.0, Elevation = 110 };
        _reader.ReadConfigFromString(Arg.Any<string>()).Returns(new ConfigRoot
        {
            Locations = new Dictionary<string, Location> { ["warsaw"] = warsaw },
        });
        var provider = CreateProvider();
        await provider.LoadAsync(path, CancellationToken.None);

        foreach (var query in new[] { "warsaw", "Warsaw", "WARSAW", "wArSaW" })
        {
            var result = provider.GetLocation(query);
            Assert.True(result.IsSuccess, $"lookup for '{query}' failed");
            Assert.Same(warsaw, result.Value);
        }
    }

    [Fact]
    public async Task GetLocation_UnknownName_ReturnsFailWithMessage()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("config.yml", "any");
        _reader.ReadConfigFromString(Arg.Any<string>()).Returns(new ConfigRoot());
        var provider = CreateProvider();
        await provider.LoadAsync(path, CancellationToken.None);

        var result = provider.GetLocation("mystery");

        Assert.True(result.IsFail);
        Assert.Equal("Location mystery not found", result.Err!.Message);
    }

    [Fact]
    public async Task LoadAsync_FullStack_WithRealYamlReader_LoadsLocations()
    {
        using var temp = new TempDirectory();
        var path = temp.WriteFile("config.yml", """
                                                 locations:
                                                   warsaw:
                                                     latitude: 52.2297
                                                     longitude: 21.0122
                                                     elevation: 110
                                                 """);
        var provider = new ConfigProvider(new YamlConfigReader(), _logger);

        await provider.LoadAsync(path, CancellationToken.None);

        var location = provider.GetLocation("warsaw");
        Assert.True(location.IsSuccess);
        Assert.Equal(52.2297, location.Value!.Latitude);
        Assert.Equal(21.0122, location.Value.Longitude);
        Assert.Equal(110, location.Value.Elevation);
    }
}
