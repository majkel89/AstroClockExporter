using AstroClockExporter.Core.Configuration.Serialization;

namespace AstroClockExporter.Tests.Configuration;

public class YamlConfigReaderTests
{
    private readonly YamlConfigReader _reader = new();

    [Fact]
    public void ReadConfigFromString_EmptyString_ReturnsNull()
    {
        var result = _reader.ReadConfigFromString(string.Empty);

        Assert.Null(result);
    }

    [Fact]
    public void ReadConfigFromString_SingleLocation_MapsCamelCaseFields()
    {
        const string yaml = """
                            locations:
                              warsaw:
                                latitude: 52.2297
                                longitude: 21.0122
                                elevation: 110
                            """;

        var result = _reader.ReadConfigFromString(yaml);

        Assert.NotNull(result);
        Assert.Single(result.Locations);
        var warsaw = result.Locations["warsaw"];
        Assert.Equal(52.2297, warsaw.Latitude);
        Assert.Equal(21.0122, warsaw.Longitude);
        Assert.Equal(110, warsaw.Elevation);
    }

    [Fact]
    public void ReadConfigFromString_MultipleLocations_PopulatesDictionary()
    {
        const string yaml = """
                            locations:
                              warsaw:
                                latitude: 52.2297
                                longitude: 21.0122
                                elevation: 110
                              equator:
                                latitude: 0
                                longitude: 0
                                elevation: 0
                            """;

        var result = _reader.ReadConfigFromString(yaml);

        Assert.NotNull(result);
        Assert.Equal(2, result.Locations.Count);
        Assert.Contains("warsaw", result.Locations.Keys);
        Assert.Contains("equator", result.Locations.Keys);
    }

    [Fact]
    public void ReadConfigFromString_MissingOptionalFields_UsesDefaults()
    {
        const string yaml = """
                            locations:
                              minimal:
                                latitude: 10
                            """;

        var result = _reader.ReadConfigFromString(yaml);

        Assert.NotNull(result);
        var minimal = result.Locations["minimal"];
        Assert.Equal(10, minimal.Latitude);
        Assert.Equal(0, minimal.Longitude);
        Assert.Equal(0, minimal.Elevation);
    }
}
