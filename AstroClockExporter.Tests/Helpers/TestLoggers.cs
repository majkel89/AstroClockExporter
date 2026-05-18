using Microsoft.Extensions.Logging;

namespace AstroClockExporter.Tests.Helpers;

// The [LoggerMessage] source generator gates every Log call behind ILogger.IsEnabled(level).
// NSubstitute mocks return false for bool methods by default, so without this opt-in the
// generated wrappers short-circuit and Received(1).Log(...) assertions never see the call.
internal static class TestLoggers
{
    public static ILogger<T> ForType<T>()
    {
        var logger = Substitute.For<ILogger<T>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        return logger;
    }
}
