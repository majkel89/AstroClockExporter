namespace AstroClockExporter.Tests.Helpers;

internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
