namespace AstroClockExporter.Tests.Helpers;

internal sealed class TempDirectory : IDisposable
{
    private string Path { get; }

    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AstroClockExporterTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string WriteFile(string name, string contents)
    {
        var fullPath = System.IO.Path.Combine(Path, name);
        File.WriteAllText(fullPath, contents);
        return fullPath;
    }

    public string FileIn(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); }
        catch { /* best-effort cleanup */ }
    }
}
