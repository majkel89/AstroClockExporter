using System.Text.RegularExpressions;
using AstroClockExporter.Core.Common;
using AstroClockExporter.Core.Configuration.DTO;
using Microsoft.Extensions.Logging;

namespace AstroClockExporter.Core.Configuration;

internal sealed partial class ConfigProvider(
    IConfigReader configReader,
    ILogger<ConfigProvider> logger) : IConfigProvider
{
    private ConfigRoot? _config;

    public async Task LoadAsync(string filePath, CancellationToken ct)
    {
        logger.LogStarted();

        if (!File.Exists(filePath))
        {
            logger.LogFileNotFound(filePath);
            _config = new ConfigRoot();
            return;
        }

        var contents = await ReadContentsAsync(filePath, ct);

        logger.LogConfigFileLoaded(filePath);

        contents = ReplaceEnvVariables(contents);

        logger.LogEnvVarsSubstituted();

        var config = configReader.ReadConfigFromString(contents);

        logger.LogConfigDeserialized();

        if (config is null)
        {
            logger.LogConfigIsEmpty(filePath);
            _config = new ConfigRoot();
            return;
        }

        _config = config with
        {
            Locations = new Dictionary<string, Location>(config.Locations, StringComparer.OrdinalIgnoreCase),
        };

        logger.LogConfigLoaded(filePath, config.Locations.Count, string.Join(", ", config.Locations.Keys));
    }

    public ConfigRoot GetConfigRoot() =>
        _config ?? throw new InvalidOperationException("Config is not loaded. Call LoadAsync first.");

    public Result<Location> GetLocation(string locationName)
    {
        if (_config is null)
            throw new InvalidOperationException("Config is not loaded. Call LoadAsync first.");
        return _config.Locations.TryGetValue(locationName, out var location)
            ? Result<Location>.Success(location)
            : Result<Location>.Fail($"Location {locationName} not found");
    }

    private static async Task<string> ReadContentsAsync(string filePath, CancellationToken ct)
    {
        await using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await new StreamReader(fs).ReadToEndAsync(ct);
    }

    private static string ReplaceEnvVariables(string contents) =>
        EnvVarPattern().Replace(contents, match =>
        {
            var value = Environment.GetEnvironmentVariable(match.Groups[1].Value);
            if (value is not null) return value;
            var @default = match.Groups[2].Value;
            return string.IsNullOrWhiteSpace(@default) ? match.Groups[0].Value : @default;
        });

    [GeneratedRegex(@"\${(\w+)(?:\:\-(\w+))?}", RegexOptions.Multiline)]
    private static partial Regex EnvVarPattern();
}
