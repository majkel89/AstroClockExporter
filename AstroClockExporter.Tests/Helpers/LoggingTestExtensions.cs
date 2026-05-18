using Microsoft.Extensions.Logging;

namespace AstroClockExporter.Tests.Helpers;

internal static class LoggingTestExtensions
{
    extension(ILogger logger)
    {
        private void ShouldLog(LogLevel level, string message, int eventId = 0) =>
            logger.Received(1).Log(level, Arg.Is<EventId>(e => e.Id == eventId),
                Arg.Is<object>(arg => message.Equals(arg.ToString())),
                null, Arg.Any<Func<object, Exception?, string>>());

        public void ShouldLogWarning(string message, int eventId = 0) =>
            logger.ShouldLog(LogLevel.Warning, message, eventId);

        public void ShouldLogError(string message, int eventId = 0) =>
            logger.ShouldLog(LogLevel.Error, message, eventId);

        public void ShouldLogInfo(string message, int eventId = 0) =>
            logger.ShouldLog(LogLevel.Information, message, eventId);

        public void ShouldLogDebug(string message, int eventId = 0) =>
            logger.ShouldLog(LogLevel.Debug, message, eventId);
    }
}
