namespace AstroClockExporter.Tests.Helpers;

internal sealed class EnvVarScope : IDisposable
{
    private readonly Dictionary<string, string?> _previous = new();

    public EnvVarScope Set(string name, string? value)
    {
        _previous[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
        return this;
    }

    public void Dispose()
    {
        foreach (var (name, previousValue) in _previous)
        {
            Environment.SetEnvironmentVariable(name, previousValue);
        }
    }
}
