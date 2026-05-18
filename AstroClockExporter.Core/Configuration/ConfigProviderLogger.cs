using Microsoft.Extensions.Logging;

namespace AstroClockExporter.Core.Configuration;

using Logger = ILogger<ConfigProvider>;

internal static partial class ConfigProviderLogger
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Loading configuration...")]
    internal static partial void LogStarted(this Logger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "File {FilePath} does not exist - using default configuration")]
    internal static partial void LogFileNotFound(this Logger logger, string filePath);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Configuration loaded from {FilePath} file")]
    internal static partial void LogConfigFileLoaded(this Logger logger, string filePath);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Substituted env variables")]
    internal static partial void LogEnvVarsSubstituted(this Logger logger);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Deserialized config")]
    internal static partial void LogConfigDeserialized(this Logger logger);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "File {FilePath} is empty - using default configuration")]
    internal static partial void LogConfigIsEmpty(this Logger logger, string filePath);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Config loaded from {FilePath} ({LocationCount} location(s): {Locations})")]
    internal static partial void LogConfigLoaded(this Logger logger, string filePath, int locationCount, string locations);
}
